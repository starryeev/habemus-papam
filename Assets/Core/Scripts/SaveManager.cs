using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SaveManager : MonoBehaviour
{
    private const int CurrentSaveVersion = 5;
    private const string MainSceneName = "MainScene";
    private const string GameSceneName = "GameScene";
    private const string SaveFolderName = "Json";
    private const string SaveFileName = "autosave.json";
    private const string CompletedPlayerNamesFileName = "completed_player_names.json";
    private const string DefaultPlayerName = "Player";

    public static SaveManager Instance { get; private set; }

    private bool pendingLoad = false;
    private bool pendingNewGame = false;
    private bool isApplyingLoad = false;
    private GameNameSaveData currentGameNames = new GameNameSaveData();

    public string SaveDirectoryPath => Path.Combine(Application.persistentDataPath, SaveFolderName);
    public string SaveFilePath => Path.Combine(SaveDirectoryPath, SaveFileName);
    public string CompletedPlayerNamesFilePath => Path.Combine(SaveDirectoryPath, CompletedPlayerNamesFileName);
    public GameNameSaveData CurrentGameNames => currentGameNames;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Bootstrap()
    {
        if (Instance != null)
        {
            return;
        }

        GameObject saveManagerObject = new GameObject(nameof(SaveManager));
        DontDestroyOnLoad(saveManagerObject);
        Instance = saveManagerObject.AddComponent<SaveManager>();
    }

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    public bool HasSave()
    {
        if (!File.Exists(SaveFilePath))
        {
            return false;
        }

        try
        {
            SaveModel saveModel = JsonUtility.FromJson<SaveModel>(File.ReadAllText(SaveFilePath));
            if (saveModel != null && saveModel.version == CurrentSaveVersion)
            {
                return true;
            }

            Debug.Log("[Save] 기존 자동저장 파일을 폐기합니다.");
            DeleteSave();
        }
        catch (Exception exception)
        {
            Debug.LogError($"[Save] 저장 파일 검사 실패: {exception}");
            DeleteSave();
        }

        return false;
    }

    public bool TryGetSavePreview(out SavePreviewData preview)
    {
        preview = null;

        if (!TryReadSave(out SaveModel saveModel) || saveModel == null || saveModel.gameContext == null)
        {
            return false;
        }

        CardinalSaveData playerSave = null;
        if (saveModel.cardinals != null)
        {
            foreach (var cardinalSave in saveModel.cardinals)
            {
                if (cardinalSave != null && cardinalSave.isPlayer)
                {
                    playerSave = cardinalSave;
                    break;
                }
            }
        }

        if (playerSave == null)
        {
            Debug.LogWarning("[Save] 플레이어 저장 데이터를 찾지 못해 기본값으로 미리보기를 구성합니다.");
        }

        int conclaveIndex = Mathf.Clamp(
            saveModel.gameContext.conclave,
            0,
            Enum.GetValues(typeof(GameContext.Conclave)).Length - 1);
        GameContext.Conclave conclave = (GameContext.Conclave)conclaveIndex;

        preview = new SavePreviewData
        {
            playerName = saveModel.names != null ? saveModel.names.playerName : string.Empty,
            playerHp = playerSave != null ? playerSave.hp : 0f,
            playerInfluence = playerSave != null ? playerSave.influence : 0f,
            playerPiety = playerSave != null ? playerSave.piety : 0f,
            day = Mathf.Max(1, saveModel.gameContext.day),
            conclave = conclaveIndex,
            conclaveName = conclave.ToString()
        };

        return true;
    }

    public void StartNewGame()
    {
        pendingLoad = false;
        pendingNewGame = true;
        Time.timeScale = 1f;

        EnsureCurrentPlayerName();
        currentGameNames.npcNames.Clear();
        if (ActionRecordManager.Instance != null)
        {
            ActionRecordManager.Instance.ClearCurrentRunStats();
        }
        DeleteSave();
        SceneManager.LoadScene(GameSceneName);
    }

    public void StartNewGame(string playerName)
    {
        SetNewGamePlayerName(playerName);
        StartNewGame();
    }

    public void LoadGame()
    {
        if (!HasSave())
        {
            Debug.LogWarning("[Save] 저장 파일이 없어 Load를 진행할 수 없습니다.");
            return;
        }

        pendingNewGame = false;
        pendingLoad = true;
        Time.timeScale = 1f;

        SceneManager.LoadScene(GameSceneName);
    }

    public void GoToMainMenu()
    {
        pendingLoad = false;
        pendingNewGame = false;
        Time.timeScale = 1f;

        SceneManager.LoadScene(MainSceneName);
    }

    public void DeleteCompletedGameSave()
    {
        pendingLoad = false;
        pendingNewGame = false;
        if (ActionRecordManager.Instance != null)
        {
            ActionRecordManager.Instance.ClearCurrentRunStats();
        }
        DeleteSave();
    }

    public void ClearContinueSaveForEnding()
    {
        pendingLoad = false;
        pendingNewGame = false;
        isApplyingLoad = false;
        DeleteSave();
    }

    public void DiscardCurrentGameSave()
    {
        pendingLoad = false;
        pendingNewGame = false;
        if (ActionRecordManager.Instance != null)
        {
            ActionRecordManager.Instance.ClearCurrentRunStats();
        }
        DeleteSave();
    }

    public void ResetAllUserData()
    {
        pendingLoad = false;
        pendingNewGame = false;
        isApplyingLoad = false;
        currentGameNames = new GameNameSaveData();

        DeleteSave();
        DeleteCompletedPlayerNames();
        NameDB.SetPlayerInputNames(null);

        if (ActionRecordManager.Instance != null)
        {
            ActionRecordManager.Instance.ResetPersistentData();
        }
    }

    public void CompleteCurrentGame()
    {
        pendingLoad = false;
        pendingNewGame = false;
        RegisterCompletedPlayerName(currentGameNames.playerName);
        DeleteSave();
        currentGameNames = new GameNameSaveData();
    }

    public void SetNewGamePlayerName(string playerName)
    {
        currentGameNames = new GameNameSaveData
        {
            playerName = SanitizeName(playerName),
            npcNames = new List<string>()
        };
    }

    public void SaveCheckpoint(SaveCheckpointType checkpointType, SaveResumeStep resumeStep)
    {
        if (isApplyingLoad || SceneManager.GetActiveScene().name != GameSceneName)
        {
            return;
        }

        SaveModel saveModel = CaptureCurrentGame(checkpointType, resumeStep);
        if (saveModel == null)
        {
            return;
        }

        WriteSave(saveModel);
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name != GameSceneName)
        {
            return;
        }

        StartCoroutine(HandleGameSceneLoaded());
    }

    private IEnumerator HandleGameSceneLoaded()
    {
        yield return null;

        while (!AreManagersReady())
        {
            yield return null;
        }

        Dictionary<string, Item> itemCatalog = BuildItemCatalog();
        ResetAllItemRuntimeStates(itemCatalog);
        SyncCompletedPlayerNamesToNameDB();

        if (pendingLoad)
        {
            if (TryReadSave(out SaveModel saveModel))
            {
                isApplyingLoad = true;
                ApplySaveToScene(saveModel, itemCatalog);
                isApplyingLoad = false;
                ResumeFromCheckpoint(saveModel);
            }

            pendingLoad = false;
            yield break;
        }

        if (pendingNewGame)
        {
            EnsureCurrentPlayerName();
            EnsureNpcNames();
            pendingNewGame = false;
        }
    }

    private bool AreManagersReady()
    {
        return InGameManager.Instance != null &&
               CardinalManager.Instance != null &&
               InventoryManager.Instance != null &&
               PlotManager.Instance != null;
    }

    private SaveModel CaptureCurrentGame(SaveCheckpointType checkpointType, SaveResumeStep resumeStep)
    {
        if (InGameManager.Instance == null)
        {
            return null;
        }

        SaveModel saveModel = new SaveModel
        {
            version = CurrentSaveVersion,
            savedAtUtc = DateTime.UtcNow.ToString("o"),
            sceneName = SceneManager.GetActiveScene().name,
            checkpointType = checkpointType,
            resumeStep = resumeStep,
            gameContext = InGameManager.Instance.CaptureSaveData(),
            cardinals = CardinalManager.Instance != null ? CardinalManager.Instance.CaptureSaveData() : new List<CardinalSaveData>(),
            inventory = InventoryManager.Instance != null ? InventoryManager.Instance.CaptureSaveData() : new InventorySaveData(),
            events = InGameManager.Instance.EventManager != null ? InGameManager.Instance.EventManager.CaptureSaveData() : new EventManagerSaveData(),
            plots = PlotManager.Instance != null ? PlotManager.Instance.CaptureSaveData() : new PlotManagerSaveData(),
            fieldItems = InGameManager.Instance.CaptureFieldItemSaveData(),
            names = CloneNames(currentGameNames),
            actionStats = ActionRecordManager.Instance != null ? ActionRecordManager.Instance.CaptureCurrentRunStats() : new ActionStatsSaveData(),
            sushi = SushiUI.Instance != null ? SushiUI.Instance.CaptureSaveData() : new SushiSaveData()
        };

        return saveModel;
    }

    private void ApplySaveToScene(SaveModel saveModel, Dictionary<string, Item> itemCatalog)
    {
        if (saveModel == null || InGameManager.Instance == null)
        {
            return;
        }

        Time.timeScale = 1f;
        currentGameNames = CloneNames(saveModel.names);
        EnsureCurrentPlayerName();
        EnsureNpcNames();

        InGameManager.Instance.StopTimer();
        InGameManager.Instance.ClearFieldItems();
        InGameManager.Instance.RestoreGameContext(saveModel.gameContext);

        if (CardinalManager.Instance != null)
        {
            CardinalManager.Instance.RestoreFromSave(saveModel.cardinals);
        }

        if (InventoryManager.Instance != null)
        {
            InventoryManager.Instance.RestoreFromSave(saveModel.inventory, itemCatalog);
        }

        if (InGameManager.Instance.EventManager != null)
        {
            InGameManager.Instance.EventManager.RestoreFromSave(saveModel.events);
        }

        if (PlotManager.Instance != null)
        {
            PlotManager.Instance.RestoreFromSave(saveModel.plots);
        }

        InGameManager.Instance.RestoreFieldItems(saveModel.fieldItems);

        if (ActionRecordManager.Instance != null)
        {
            ActionRecordManager.Instance.RestoreCurrentRunStats(saveModel.actionStats);
        }

        RefreshSceneUi(saveModel);
    }

    private void ResumeFromCheckpoint(SaveModel saveModel)
    {
        switch (saveModel.resumeStep)
        {
            case SaveResumeStep.ReopenPendingEvent:
                InGameManager.Instance.RestorePendingTurnEventUI();
                break;

            case SaveResumeStep.OpenSushiSelection:
                if (SushiUI.Instance != null)
                {
                    SushiUI.Instance.RestoreFromSave(saveModel.sushi);
                }
                break;

            case SaveResumeStep.StartNextConclave:
                InGameManager.Instance.StartConclaveCycle();
                break;

            case SaveResumeStep.ContinueAfterResolvedEvent:
                InGameManager.Instance.ResumeAfterResolvedEvent();
                break;
        }
    }

    private void RefreshSceneUi(SaveModel saveModel)
    {
        TimeUI timeUI = FindAnyObjectByType<TimeUI>();
        if (timeUI != null)
        {
            timeUI.ResetUI();
        }

        bool hasActiveCardinals = false;
        if (saveModel != null && saveModel.cardinals != null)
        {
            foreach (var cardinal in saveModel.cardinals)
            {
                if (cardinal != null && cardinal.isActive)
                {
                    hasActiveCardinals = true;
                    break;
                }
            }
        }

        if (hasActiveCardinals && CardinalManager.Instance != null && CardinalManager.Instance.StatsUI != null)
        {
            CardinalManager.Instance.StatsUI.Initialize(CardinalManager.Instance.Cardinals);
            CardinalManager.Instance.StatsUI.FadeInAfterConclaveEntrance(1f);
        }
    }

    private void EnsureCurrentPlayerName()
    {
        currentGameNames.playerName = SanitizeName(currentGameNames.playerName);
    }

    private void EnsureNpcNames()
    {
        if (currentGameNames.npcNames == null)
        {
            currentGameNames.npcNames = new List<string>();
        }

        while (currentGameNames.npcNames.Count < 3)
        {
            string npcName = NameDB.Instance != null ? NameDB.Instance.GetRandomName() : null;
            if (string.IsNullOrWhiteSpace(npcName))
            {
                npcName = $"NPC{currentGameNames.npcNames.Count + 1}";
            }

            currentGameNames.npcNames.Add(npcName);
        }

        if (currentGameNames.npcNames.Count > 3)
        {
            currentGameNames.npcNames.RemoveRange(3, currentGameNames.npcNames.Count - 3);
        }
    }

    private static string SanitizeName(string playerName)
    {
        string sanitized = string.IsNullOrWhiteSpace(playerName) ? DefaultPlayerName : playerName.Trim();
        return sanitized.Length > 10 ? sanitized.Substring(0, 10) : sanitized;
    }

    private static GameNameSaveData CloneNames(GameNameSaveData source)
    {
        GameNameSaveData clone = new GameNameSaveData();
        if (source == null)
        {
            return clone;
        }

        clone.playerName = source.playerName;
        clone.npcNames = source.npcNames != null ? new List<string>(source.npcNames) : new List<string>();
        return clone;
    }

    private void RegisterCompletedPlayerName(string playerName)
    {
        string sanitized = SanitizeName(playerName);
        CompletedPlayerNameSaveData data = ReadCompletedPlayerNames();
        if (!data.playerInputNames.Contains(sanitized))
        {
            data.playerInputNames.Add(sanitized);
            WriteCompletedPlayerNames(data);
        }

        NameDB.AddCompletedPlayerInputName(sanitized);
    }

    private void SyncCompletedPlayerNamesToNameDB()
    {
        if (NameDB.Instance != null)
        {
            NameDB.SetPlayerInputNames(ReadCompletedPlayerNames().playerInputNames);
        }
    }

    private CompletedPlayerNameSaveData ReadCompletedPlayerNames()
    {
        if (!File.Exists(CompletedPlayerNamesFilePath))
        {
            return new CompletedPlayerNameSaveData();
        }

        try
        {
            string json = File.ReadAllText(CompletedPlayerNamesFilePath);
            CompletedPlayerNameSaveData data = JsonUtility.FromJson<CompletedPlayerNameSaveData>(json);
            return data ?? new CompletedPlayerNameSaveData();
        }
        catch (Exception exception)
        {
            Debug.LogError($"[Save] Completed player name load failed: {exception}");
            return new CompletedPlayerNameSaveData();
        }
    }

    private void WriteCompletedPlayerNames(CompletedPlayerNameSaveData data)
    {
        try
        {
            Directory.CreateDirectory(SaveDirectoryPath);
            File.WriteAllText(CompletedPlayerNamesFilePath, JsonUtility.ToJson(data, true));
        }
        catch (Exception exception)
        {
            Debug.LogError($"[Save] Completed player name save failed: {exception}");
        }
    }

    private Dictionary<string, Item> BuildItemCatalog()
    {
        Dictionary<string, Item> itemCatalog = new Dictionary<string, Item>();
        Item[] loadedItems = Resources.FindObjectsOfTypeAll<Item>();

        foreach (var item in loadedItems)
        {
            if (item == null || string.IsNullOrWhiteSpace(item.itemID))
            {
                continue;
            }

            if (!itemCatalog.ContainsKey(item.itemID))
            {
                itemCatalog[item.itemID] = item;
            }
        }

        return itemCatalog;
    }

    private void ResetAllItemRuntimeStates(Dictionary<string, Item> itemCatalog)
    {
        if (itemCatalog == null)
        {
            return;
        }

        foreach (var item in itemCatalog.Values)
        {
            if (item == null)
            {
                continue;
            }

            item.ResetRuntimeState();
        }
    }

    private void WriteSave(SaveModel saveModel)
    {
        string temporaryPath = SaveFilePath + ".tmp";

        try
        {
            Directory.CreateDirectory(SaveDirectoryPath);
            string json = JsonUtility.ToJson(saveModel, true);
            File.WriteAllText(temporaryPath, json);

            if (File.Exists(SaveFilePath))
            {
                File.Replace(temporaryPath, SaveFilePath, null);
            }
            else
            {
                File.Move(temporaryPath, SaveFilePath);
            }

            int phase = saveModel.gameContext.isEventPhase
                ? 3
                : Mathf.Clamp(saveModel.gameContext.completedActions + 1, 1, 2);
            Debug.Log($"[Save] {saveModel.checkpointType} 완료: Turn " +
                $"{saveModel.gameContext.currentTurn}-{phase} / {SaveFilePath}");
        }
        catch (Exception exception)
        {
            Debug.LogError($"[Save] 저장 실패: {exception}");
        }
        finally
        {
            if (File.Exists(temporaryPath))
            {
                File.Delete(temporaryPath);
            }
        }
    }

    private bool TryReadSave(out SaveModel saveModel)
    {
        saveModel = null;

        if (!HasSave())
        {
            return false;
        }

        try
        {
            string json = File.ReadAllText(SaveFilePath);
            saveModel = JsonUtility.FromJson<SaveModel>(json);

            if (saveModel == null)
            {
                Debug.LogWarning("[Save] 저장 파일을 읽었지만 복원 가능한 데이터가 없습니다.");
                return false;
            }

            return true;
        }
        catch (Exception exception)
        {
            Debug.LogError($"[Save] 로드 실패: {exception}");
            return false;
        }
    }

    private void DeleteSave()
    {
        try
        {
            if (File.Exists(SaveFilePath))
            {
                File.Delete(SaveFilePath);
            }

            Directory.CreateDirectory(SaveDirectoryPath);
        }
        catch (Exception exception)
        {
            Debug.LogError($"[Save] 저장 파일 삭제 실패: {exception}");
        }
    }

    private void DeleteCompletedPlayerNames()
    {
        try
        {
            if (File.Exists(CompletedPlayerNamesFilePath))
            {
                File.Delete(CompletedPlayerNamesFilePath);
            }
        }
        catch (Exception exception)
        {
            Debug.LogError($"[Save] Completed player name delete failed: {exception}");
        }
    }
}
