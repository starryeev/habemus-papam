using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public enum NPCBehaviour
{
    None = 0,
    Pray = 1,
    Speech = 2,
    ActionBlocked = 3,
    PlayerExtraAction = 4
}

public class GameContext
{
    public enum Conclave
    {
        Dawn,
        Morning,
        Afternoon,
        Evening
    }

    public enum GameContextEvent
    {
        ConclaveStart,
        ConclaveEnd,
        TurnStart
    }

    int currentDay;
    Conclave currentConclave;
    int currentTurn;
    int completedActions;
    int actionsThisTurn;
    bool isEventPhase;

    public event Action<GameContextEvent> OnGameContextEvent;

    public int CurrentDay => currentDay;
    public Conclave CurrentConclave => currentConclave;
    public int CurrentTurn => currentTurn;
    public int CompletedActions => completedActions;
    public int ActionsThisTurn => actionsThisTurn;
    public bool IsEventPhase => isEventPhase;
    public int DisplayPhase => isEventPhase ? 3 : Mathf.Clamp(completedActions + 1, 1, 2);

    private Event currentEvent;
    public Event CurrentEvent => currentEvent;

    public void InitGameContext(int day=1, Conclave conclave=Conclave.Dawn)
    {
        currentDay = day;
        currentConclave = conclave;
        currentEvent = ScriptableObject.CreateInstance<E11100>();
        ResetTurns();
    }

    public void RestoreState(int day, Conclave conclave, int restoredTurn, int restoredCompletedActions,
        int restoredActionsThisTurn, bool restoredEventPhase)
    {
        currentDay = day;
        currentConclave = conclave;
        currentTurn = Mathf.Clamp(restoredTurn, 1, 4);
        actionsThisTurn = Mathf.Clamp(restoredActionsThisTurn, 0, 4);
        completedActions = Mathf.Clamp(restoredCompletedActions, 0, actionsThisTurn);
        isEventPhase = restoredEventPhase;
    }

    public void AdvanceConclave()
    {
        if (currentConclave == Conclave.Evening)
        {
            currentConclave = Conclave.Dawn;
            currentDay++;
        }
        else
        {
            currentConclave++;
        }

        ResetTurns();

        OnGameContextEvent?.Invoke(GameContextEvent.ConclaveStart);
    }

    public void EndConclave()
    {
        OnGameContextEvent?.Invoke(GameContextEvent.ConclaveEnd);
    }

    public void BeginTurn(int actionModifier, bool blockActions)
    {
        completedActions = 0;
        actionsThisTurn = blockActions ? 0 : Mathf.Clamp(2 + actionModifier, 1, 3);
        isEventPhase = false;
        OnGameContextEvent?.Invoke(GameContextEvent.TurnStart);
    }

    public bool CompleteAction()
    {
        if (isEventPhase || completedActions >= actionsThisTurn) return false;
        completedActions++;
        return true;
    }

    public bool AreActionsComplete() => completedActions >= actionsThisTurn;

    public void CompleteRemainingActions() => completedActions = actionsThisTurn;

    public void AddCurrentTurnActions(int count)
    {
        if (isEventPhase || count <= 0) return;
        actionsThisTurn = Mathf.Clamp(actionsThisTurn + count, completedActions, 4);
    }

    public void EnterEventPhase() => isEventPhase = true;

    public bool AdvanceTurn()
    {
        if (currentTurn >= 4) return false;
        currentTurn++;
        return true;
    }

    public void StartGame()
    {
        OnGameContextEvent?.Invoke(GameContextEvent.ConclaveStart);
    }
    public void SetNewEvent()
    {
        currentEvent = InGameManager.Instance.EventManager.GetNewEvent();
    }
    public void SetEvent(Event evt)
    {
        currentEvent = evt;
    }

    private void ResetTurns()
    {
        currentTurn = 1;
        completedActions = 0;
        actionsThisTurn = 2;
        isEventPhase = false;
    }
}

public class InGameManager : MonoBehaviour
{
    public static InGameManager Instance { get; private set; }

    [SerializeField] private GameBalance balance;
    private GameContext gameContext;
    [SerializeField] private EventManager eventManager;
    [SerializeField] private Event CurrentEvent;

    [Header("UI 연결")]
    [SerializeField] private Button startButton;
    [SerializeField] private GameObject inventoryUIPanel;

    [Header("아이템 스폰 설정")]
    [Tooltip("아이템이 스폰될 수 있는 위치들")]
    [SerializeField] private List<Transform> spawnPoints;

    [Tooltip("필드에 드랍될 '일반' 등급 아이템 프리팹들")]
    [SerializeField] private List<GameObject> commonItemPrefabs;

    [Tooltip("필드에 드랍될 '고급' 등급 아이템 프리팹들")]
    [SerializeField] private List<GameObject> rareItemPrefabs;

    [Range(0, 100)][SerializeField] private float spawnChance = 100f;
    [Range(0, 100)][SerializeField] private float spawnTwoItemsChance = 30f;
    [Range(0, 100)][SerializeField] private float rareItemChance = 20f;

    private List<GameObject> spawnedFieldItems = new List<GameObject>();
    private bool isTimeRunning = false;
    private bool isFirstStart = true;
    private bool isSushiOn = false;
    private bool hasHandledFirstPlayerHpZero = false;
    private bool shouldRevivePlayerOnNextConclave = false;
    private bool isHandlingFinalPlayerHpZero = false;
    private bool isEndingConclaveAfterPlayerHpZero = false;
    private bool isConclaveExitInProgress = false;
    private int nextTurnActionModifier;
    private bool blockNextTurn;
    private bool blockRemainingCurrentTurn;
    private bool awaitingTurnEvent;
    private bool eventBeforeActions;
    private Event queuedImmediateEvent;
    private bool endConclaveAfterEvent;
    private readonly NPCBehaviour[,] npcTurnBehaviours = new NPCBehaviour[3, 4];
    private readonly bool[,] npcTurnActionsExecuted = new bool[3, 4];
    private readonly bool[] npcNextTurnActionBlocked = new bool[3];
    private readonly HashSet<int> prayerBlockedCandidateNumbers = new HashSet<int>();
    private readonly List<PendingEffectSaveData> pendingEffects = new List<PendingEffectSaveData>();

    public GameBalance Balance => balance;
    public GameContext Context => gameContext;
    public bool IsTimeRunning => isTimeRunning;
    public EventManager EventManager => eventManager;
    public bool IsFirstStart => isFirstStart;
    public bool IsSushiOn => isSushiOn;
    public bool IsConclaveExitInProgress => isConclaveExitInProgress;
    public bool IsAwaitingTurnEvent => awaitingTurnEvent;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        gameContext = new GameContext();
        gameContext.OnGameContextEvent += HandleGameContextEvent;

        eventManager = GetComponent<EventManager>();
    }

    void Start()
    {
        InitGame();
        StartCoroutine(AutoStartConclaveAfterDelay());
    }

    public void StartConclaveCycle()
    {
        if (isTimeRunning)
        {
            return;
        }

        ConfigureStartButton(false, false);

        if (isFirstStart)
        {
            Debug.Log(">>> 게임 최초 시작");
            isFirstStart = false;
            gameContext.StartGame();
        }
        else
        {
            isSushiOn = true;
            Debug.Log(">>> 다음 콘클라베 진행");
            gameContext.AdvanceConclave();
        }
    }

    void Update()
    {
    }

    public void StartTimer()
    {
        isEndingConclaveAfterPlayerHpZero = false;
        isTimeRunning = true;
        awaitingTurnEvent = false;
        endConclaveAfterEvent = false;
        blockRemainingCurrentTurn = false;
        gameContext.BeginTurn(ConsumeNextTurnActionModifier(), ConsumeNextTurnBlock());
        Debug.Log("턴 진행 시작");

        if (inventoryUIPanel != null)
        {
            inventoryUIPanel.SetActive(true);
        }

        SpawnFieldItems();
        Event startEvent = eventManager != null ? eventManager.GetStartOfDayEvent() : null;
        if (startEvent != null)
        {
            OpenEventBeforeActions(startEvent);
            return;
        }
        TryResolveTurnWithoutActions();
    }

    public void StopTimer()
    {
        isTimeRunning = false;
    }

    public void OnExitSequenceFinished()
    {
        Debug.Log("퇴장 완료.");

        ConfigureStartButton(true, true);

        if (ElectionManager.Instance != null)
        {
            ElectionManager.Instance.OnConclaveEnded();
        }
    }

    public void OnConclaveExitSequenceCompleted()
    {
        isConclaveExitInProgress = false;
    }

    void InitGame()
    {
        isTimeRunning = false;
        isFirstStart = true;
        isSushiOn = false;
        isConclaveExitInProgress = false;
        nextTurnActionModifier = 0;
        blockNextTurn = false;
        blockRemainingCurrentTurn = false;
        awaitingTurnEvent = false;
        eventBeforeActions = false;
        queuedImmediateEvent = null;
        endConclaveAfterEvent = false;
        prayerBlockedCandidateNumbers.Clear();
        pendingEffects.Clear();

        gameContext.InitGameContext();
        ConfigureStartButton(true, true);

        if (inventoryUIPanel != null)
        {
            inventoryUIPanel.SetActive(false);
        }
    }

    void HandleGameContextEvent(GameContext.GameContextEvent eventType)
    {
        switch (eventType)
        {
            case GameContext.GameContextEvent.ConclaveStart:
                isConclaveExitInProgress = false;
                Debug.Log($"[InGameManager] 콘클라베 시작: {gameContext.CurrentConclave}");

                if (ActionRecordManager.Instance != null)
                {
                    ActionRecordManager.Instance.RecordConclaveStarted();
                }

                TryRevivePlayerOnNextConclave();

                if (CardinalManager.Instance != null)
                {
                    CardinalManager.Instance.StartConClave();
                }
                ProcessPendingEffects();
                break;

            case GameContext.GameContextEvent.ConclaveEnd:
                isConclaveExitInProgress = true;
                prayerBlockedCandidateNumbers.Clear();
                GameSceneCameraZoom.ReleaseAllGameCameraZoomAndFollow(1f);
                Debug.Log("[InGameManager] 콘클라베 종료 (Turn Complete)");

                if (inventoryUIPanel != null)
                {
                    inventoryUIPanel.SetActive(false);
                }

                ClearFieldItems();

                if (CardinalManager.Instance != null)
                {
                    CardinalManager.Instance.StopConClave();
                }
                break;

            case GameContext.GameContextEvent.TurnStart:
                SelectNpcBehavioursForTurn();
                break;
        }
    }

    private IEnumerator AutoStartConclaveAfterDelay()
    {
        yield return new WaitForSecondsRealtime(3f);

        if (isFirstStart && !isTimeRunning)
        {
            StartConclaveCycle();
        }
    }

    private void SpawnFieldItems()
    {
        if (spawnPoints == null || spawnPoints.Count == 0)
        {
            return;
        }

        if (commonItemPrefabs.Count == 0 && rareItemPrefabs.Count == 0)
        {
            return;
        }

        if (UnityEngine.Random.Range(0f, 100f) > spawnChance)
        {
            return;
        }

        int spawnCount = UnityEngine.Random.Range(0f, 100f) <= spawnTwoItemsChance ? 2 : 1;
        List<Transform> availablePoints = new List<Transform>(spawnPoints);

        for (int i = 0; i < spawnCount; i++)
        {
            if (availablePoints.Count == 0)
            {
                break;
            }

            int pointIndex = UnityEngine.Random.Range(0, availablePoints.Count);
            Transform spawnPoint = availablePoints[pointIndex];
            availablePoints.RemoveAt(pointIndex);

            GameObject prefabToSpawn = GetRandomItemPrefab();
            if (prefabToSpawn == null)
            {
                continue;
            }

            GameObject spawnedObj = Instantiate(prefabToSpawn, spawnPoint.position, spawnPoint.rotation);
            spawnedFieldItems.Add(spawnedObj);
        }
    }

    public GameObject GetRandomItemPrefab()
    {
        bool isRare = UnityEngine.Random.Range(0f, 100f) <= rareItemChance;

        if (isRare && rareItemPrefabs.Count > 0)
        {
            return rareItemPrefabs[UnityEngine.Random.Range(0, rareItemPrefabs.Count)];
        }

        if (commonItemPrefabs.Count > 0)
        {
            return commonItemPrefabs[UnityEngine.Random.Range(0, commonItemPrefabs.Count)];
        }

        return null;
    }

    public GameObject GetFieldItemPrefabByItemId(string itemId)
    {
        if (string.IsNullOrWhiteSpace(itemId))
        {
            return null;
        }

        foreach (var prefab in commonItemPrefabs)
        {
            if (PrefabMatchesItem(prefab, itemId))
            {
                return prefab;
            }
        }

        foreach (var prefab in rareItemPrefabs)
        {
            if (PrefabMatchesItem(prefab, itemId))
            {
                return prefab;
            }
        }

        return null;
    }

    private bool PrefabMatchesItem(GameObject prefab, string itemId)
    {
        if (prefab == null)
        {
            return false;
        }

        FieldItem fieldItem = prefab.GetComponent<FieldItem>();
        return fieldItem != null && fieldItem.ItemData != null && fieldItem.ItemData.itemID == itemId;
    }

    public void ClearFieldItems()
    {
        foreach (var item in spawnedFieldItems)
        {
            if (item != null)
            {
                Destroy(item);
            }
        }

        spawnedFieldItems.Clear();
    }

    public List<FieldItemSaveData> CaptureFieldItemSaveData()
    {
        List<FieldItemSaveData> saveData = new List<FieldItemSaveData>();

        foreach (var spawnedItem in spawnedFieldItems)
        {
            if (spawnedItem == null)
            {
                continue;
            }

            FieldItem fieldItem = spawnedItem.GetComponent<FieldItem>();
            if (fieldItem == null || fieldItem.ItemData == null)
            {
                continue;
            }

            saveData.Add(new FieldItemSaveData
            {
                itemId = fieldItem.ItemData.itemID,
                position = SerializableVector3.FromVector3(spawnedItem.transform.position),
                rotationZ = spawnedItem.transform.eulerAngles.z
            });
        }

        return saveData;
    }

    public void RestoreFieldItems(List<FieldItemSaveData> saveData)
    {
        ClearFieldItems();

        if (saveData == null)
        {
            return;
        }

        foreach (var fieldItemSave in saveData)
        {
            if (fieldItemSave == null || string.IsNullOrWhiteSpace(fieldItemSave.itemId))
            {
                continue;
            }

            GameObject prefab = GetFieldItemPrefabByItemId(fieldItemSave.itemId);
            if (prefab == null)
            {
                Debug.LogWarning($"[Save] 필드 아이템 프리팹 '{fieldItemSave.itemId}'를 찾지 못했습니다.");
                continue;
            }

            GameObject restored = Instantiate(
                prefab,
                fieldItemSave.position.ToVector3(),
                Quaternion.Euler(0f, 0f, fieldItemSave.rotationZ));

            spawnedFieldItems.Add(restored);
        }
    }

    public GameContextSaveData CaptureSaveData()
    {
        GameContextSaveData saveData = new GameContextSaveData
        {
            day = gameContext.CurrentDay,
            conclave = (int)gameContext.CurrentConclave,
            currentTurn = gameContext.CurrentTurn,
            completedActions = gameContext.CompletedActions,
            actionsThisTurn = gameContext.ActionsThisTurn,
            isEventPhase = gameContext.IsEventPhase,
            nextTurnActionModifier = nextTurnActionModifier,
            blockNextTurn = blockNextTurn,
            blockRemainingCurrentTurn = blockRemainingCurrentTurn,
            awaitingTurnEvent = awaitingTurnEvent,
            eventBeforeActions = eventBeforeActions,
            endConclaveAfterEvent = endConclaveAfterEvent,
            currentEventId = gameContext.CurrentEvent != null ? gameContext.CurrentEvent.eventID : string.Empty,
            isTimeRunning = isTimeRunning,
            isFirstStart = isFirstStart,
            isSushiOn = isSushiOn,
            showStartButton = startButton != null && startButton.gameObject.activeSelf,
            startButtonInteractable = startButton == null || startButton.interactable,
            showInventoryPanel = inventoryUIPanel != null && inventoryUIPanel.activeSelf,
            hasHandledFirstPlayerHpZero = hasHandledFirstPlayerHpZero,
            shouldRevivePlayerOnNextConclave = shouldRevivePlayerOnNextConclave
        };

        for (int candidate = 0; candidate < 3; candidate++)
        {
            for (int action = 0; action < 4; action++)
            {
                saveData.npcTurnBehaviours.Add((int)npcTurnBehaviours[candidate, action]);
                saveData.npcTurnActionsExecuted.Add(npcTurnActionsExecuted[candidate, action]);
            }
            saveData.npcNextTurnActionBlocked.Add(npcNextTurnActionBlocked[candidate]);
        }

        foreach (PendingEffectSaveData effect in pendingEffects)
        {
            if (effect == null) continue;
            saveData.pendingEffects.Add(ClonePendingEffect(effect));
        }

        foreach (int candidateNumber in prayerBlockedCandidateNumbers)
        {
            saveData.prayerBlockedCandidateNumbers.Add(candidateNumber);
        }

        return saveData;
    }

    public void RestoreGameContext(GameContextSaveData saveData)
    {
        if (saveData == null)
        {
            return;
        }

        GameContext.Conclave conclave = (GameContext.Conclave)Mathf.Clamp(saveData.conclave, 0, Enum.GetValues(typeof(GameContext.Conclave)).Length - 1);

        gameContext.RestoreState(saveData.day, conclave, saveData.currentTurn, saveData.completedActions,
            saveData.actionsThisTurn, saveData.isEventPhase);
        isTimeRunning = saveData.isTimeRunning;
        isFirstStart = saveData.isFirstStart;
        isSushiOn = saveData.isSushiOn;
        nextTurnActionModifier = Mathf.Clamp(saveData.nextTurnActionModifier, -2, 1);
        blockNextTurn = saveData.blockNextTurn;
        blockRemainingCurrentTurn = saveData.blockRemainingCurrentTurn;
        awaitingTurnEvent = saveData.awaitingTurnEvent;
        eventBeforeActions = saveData.eventBeforeActions;
        endConclaveAfterEvent = saveData.endConclaveAfterEvent;
        hasHandledFirstPlayerHpZero = saveData.hasHandledFirstPlayerHpZero;
        shouldRevivePlayerOnNextConclave = saveData.shouldRevivePlayerOnNextConclave;
        isHandlingFinalPlayerHpZero = false;
        isEndingConclaveAfterPlayerHpZero = false;
        isConclaveExitInProgress = false;
        RestoreNpcTurnPlan(saveData);
        RestorePrayerBlocks(saveData.prayerBlockedCandidateNumbers);
        RestorePendingEffects(saveData.pendingEffects);
        if (!string.IsNullOrWhiteSpace(saveData.currentEventId) && eventManager != null)
        {
            Event restoredEvent = eventManager.GetEventById(saveData.currentEventId);
            if (restoredEvent != null) gameContext.SetEvent(restoredEvent);
        }

        ConfigureStartButton(saveData.showStartButton, saveData.startButtonInteractable);

        if (inventoryUIPanel != null)
        {
            inventoryUIPanel.SetActive(saveData.showInventoryPanel);
        }
    }

    public void ConfigureStartButton(bool visible, bool interactable)
    {
        if (startButton == null)
        {
            return;
        }

        startButton.interactable = interactable;
        startButton.gameObject.SetActive(visible);
    }

    public void HandlePlayerHpReachedZero(Cardinal player)
    {
        if (player == null || !player.CompareTag("Player") || isHandlingFinalPlayerHpZero)
        {
            return;
        }

        if (!hasHandledFirstPlayerHpZero)
        {
            hasHandledFirstPlayerHpZero = true;
            shouldRevivePlayerOnNextConclave = true;

            if (PlayerHealthVignetteController.Instance != null)
            {
                PlayerHealthVignetteController.Instance.PlayFirstPlayerDownEffect(EndCurrentConclaveAfterPlayerHpZero);
            }
            else
            {
                EndCurrentConclaveAfterPlayerHpZero();
            }
            return;
        }

        isHandlingFinalPlayerHpZero = true;
        StateController playerState = player.GetComponent<StateController>();
        playerState?.StopForGameOver();
        GameSceneCameraZoom.LockAllGameCamerasZoomedOut(1f);
        StopTimer();

        if (SoundManager.Instance != null)
        {
            SoundManager.Instance.StopBGM();
        }

        if (ActionRecordManager.Instance != null)
        {
            ActionRecordManager.Instance.RecordHealthGameOver();
        }

        if (PlayerHealthVignetteController.Instance != null)
        {
            PlayerHealthVignetteController.Instance.PlayFinalGameOverEffect(DeleteCurrentGameAndReturnToMain);
        }
        else
        {
            DeleteCurrentGameAndReturnToMain();
        }
    }

    private void EndCurrentConclaveAfterPlayerHpZero()
    {
        if (isEndingConclaveAfterPlayerHpZero)
        {
            return;
        }

        isEndingConclaveAfterPlayerHpZero = true;
        StopTimer();
        GameSceneCameraZoom.ReleaseAllGameCameraZoomAndFollow(1f);

        if (inventoryUIPanel != null)
        {
            inventoryUIPanel.SetActive(false);
        }

        ClearFieldItems();

        if (CardinalManager.Instance != null)
        {
            CardinalManager.Instance.StopConClave();
        }
    }

    private void TryRevivePlayerOnNextConclave()
    {
        if (!shouldRevivePlayerOnNextConclave)
        {
            return;
        }

        Cardinal player = FindPlayerCardinal();
        if (player == null)
        {
            return;
        }

        player.ChangeHp(5f - player.Hp);
        shouldRevivePlayerOnNextConclave = false;

        Debug.Log("[Player HP] Player revived to 5 HP for the next conclave.");
    }

    private Cardinal FindPlayerCardinal()
    {
        if (CardinalManager.Instance == null)
        {
            return null;
        }

        foreach (Cardinal cardinal in CardinalManager.Instance.Cardinals)
        {
            if (cardinal != null && cardinal.CompareTag("Player"))
            {
                return cardinal;
            }
        }

        return null;
    }

    private void DeleteCurrentGameAndReturnToMain()
    {
        Time.timeScale = 1f;

        if (SaveManager.Instance != null)
        {
            SaveManager.Instance.DiscardCurrentGameSave();
            SaveManager.Instance.GoToMainMenu();
            return;
        }

        SceneManager.LoadScene("MainScene");
    }

    public void DebugSetPlayerHpTo21()
    {
        if (CardinalManager.Instance == null)
        {
            Debug.LogWarning("[Debug] CardinalManager was not found. Cannot set player HP.");
            return;
        }

        foreach (Cardinal cardinal in CardinalManager.Instance.Cardinals)
        {
            if (cardinal != null && cardinal.CompareTag("Player"))
            {
                cardinal.ChangeHp(3f - cardinal.Hp);
                Debug.Log($"[Debug] Player HP set to {cardinal.Hp}.");
                return;
            }
        }

        Debug.LogWarning("[Debug] Player cardinal was not found. Cannot set player HP.");
    }

    public int GetCurrentDay()
    {
        return gameContext.CurrentDay;
    }

    public GameContext.Conclave GetCurrentConclave()
    {
        return gameContext.CurrentConclave;
    }

    public int GetCurrentTurn()
    {
        return gameContext.CurrentTurn;
    }
    public int GetCurrentTurnPhase() => gameContext.DisplayPhase;

    public bool CanPerformPlayerAction(Cardinal performer)
    {
        return performer == null || !performer.CompareTag("Player") ||
            (isTimeRunning && !awaitingTurnEvent && !gameContext.IsEventPhase && !gameContext.AreActionsComplete());
    }

    public bool CanPerformPrayer(Cardinal performer, out string alertTitle, out string alertMessage)
    {
        alertTitle = "기도";
        alertMessage = string.Empty;
        return performer == null || !prayerBlockedCandidateNumbers.Contains(GetCandidateNumber(performer));
    }

    public void BlockPrayerForCurrentConclave(Cardinal performer)
    {
        int candidateNumber = GetCandidateNumber(performer);
        if (candidateNumber >= 0) prayerBlockedCandidateNumbers.Add(candidateNumber);
    }

    private void RestorePrayerBlocks(List<int> savedCandidateNumbers)
    {
        prayerBlockedCandidateNumbers.Clear();
        if (savedCandidateNumbers == null) return;
        foreach (int candidateNumber in savedCandidateNumbers)
        {
            if (candidateNumber >= 0 && candidateNumber <= 3)
                prayerBlockedCandidateNumbers.Add(candidateNumber);
        }
    }

    public NPCBehaviour GetNPCBehaviourThisTurn(int candidateNumber)
    {
        int candidateIndex = Mathf.Clamp(candidateNumber - 1, 0, 2);
        int actionIndex = Mathf.Clamp(gameContext.CompletedActions, 0, 3);
        return npcTurnBehaviours[candidateIndex, actionIndex];
    }

    public NPCBehaviour GetNPCBehaviourThisTurn(int candidateNumber, int actionIndex)
    {
        return npcTurnBehaviours[Mathf.Clamp(candidateNumber - 1, 0, 2), Mathf.Clamp(actionIndex, 0, 3)];
    }

    public void ExecuteNpcActionsBeforePlayerAction(Cardinal performer)
    {
        if (performer == null || !performer.CompareTag("Player") || gameContext.IsEventPhase) return;

        int actionIndex = Mathf.Clamp(gameContext.CompletedActions, 0, 3);
        for (int candidateNumber = 1; candidateNumber <= 3; candidateNumber++)
        {
            int candidateIndex = candidateNumber - 1;
            if (npcTurnActionsExecuted[candidateIndex, actionIndex]) continue;
            npcTurnActionsExecuted[candidateIndex, actionIndex] = true;

            Cardinal candidate = GetRepresentativeCandidate(candidateNumber);
            if (candidate == null || candidate.Hp <= 0f || candidate.IsKnockedOut) continue;
            ExecuteNpcBehaviour(candidate, npcTurnBehaviours[candidateIndex, actionIndex]);
        }

        // 플레이어 행동이 1회뿐인 턴도 NPC의 기본 2행동은 플레이어 효과보다 먼저 끝낸다.
        if (gameContext.ActionsThisTurn == 1 && actionIndex == 0)
        {
            for (int candidateNumber = 1; candidateNumber <= 3; candidateNumber++)
            {
                int candidateIndex = candidateNumber - 1;
                if (npcTurnActionsExecuted[candidateIndex, 1]) continue;
                npcTurnActionsExecuted[candidateIndex, 1] = true;

                Cardinal candidate = GetRepresentativeCandidate(candidateNumber);
                if (candidate == null || candidate.Hp <= 0f || candidate.IsKnockedOut) continue;
                ExecuteNpcBehaviour(candidate, npcTurnBehaviours[candidateIndex, 1]);
            }
        }
    }

    public float GetSpeechSuccessChance(Cardinal actor)
    {
        float chance = GetNpcCandidateNumber(actor) == 1 ? 0.9f : balance.SpeechSuccessChance;
        Cardinal leader = GetLeadingCandidate();
        if (GetNpcCandidateNumber(leader) == 1 && actor != leader) chance -= 0.1f;
        return Mathf.Clamp01(chance);
    }

    public int GetNpcCandidateNumber(Cardinal candidate)
    {
        if (candidate == null) return 0;
        for (int candidateNumber = 1; candidateNumber <= 3; candidateNumber++)
        {
            if (GetRepresentativeCandidate(candidateNumber) == candidate) return candidateNumber;
        }
        return 0;
    }

    public bool IsNpcCandidateLeading(int candidateNumber)
    {
        return GetRepresentativeCandidate(candidateNumber) == GetLeadingCandidate();
    }

    private void SelectNpcBehavioursForTurn()
    {
        PrepareNpcPassives();
        for (int candidateNumber = 1; candidateNumber <= 3; candidateNumber++)
        {
            Cardinal candidate = GetRepresentativeCandidate(candidateNumber);
            bool actionBlocked = npcNextTurnActionBlocked[candidateNumber - 1];
            npcNextTurnActionBlocked[candidateNumber - 1] = false;

            for (int actionIndex = 0; actionIndex < 4; actionIndex++)
            {
                bool isBaseNpcAction = actionIndex < 2;
                bool isPlayerExtraAction = actionIndex >= 2 && actionIndex < gameContext.ActionsThisTurn;
                npcTurnActionsExecuted[candidateNumber - 1, actionIndex] = !isBaseNpcAction && !isPlayerExtraAction;

                if (isBaseNpcAction)
                {
                    npcTurnBehaviours[candidateNumber - 1, actionIndex] = actionBlocked && actionIndex == 1
                        ? NPCBehaviour.ActionBlocked
                        : candidate != null ? RollNpcBehaviour(candidateNumber, candidate.Hp) : NPCBehaviour.None;
                }
                else
                {
                    npcTurnBehaviours[candidateNumber - 1, actionIndex] = isPlayerExtraAction
                        ? NPCBehaviour.PlayerExtraAction : NPCBehaviour.None;
                }
            }
        }
    }

    private static NPCBehaviour RollNpcBehaviour(int candidateNumber, float hp)
    {
        float roll = UnityEngine.Random.value;
        bool healthy = hp >= 4f;
        switch (candidateNumber)
        {
            case 1:
                if (healthy) return roll < 0.3f ? NPCBehaviour.Pray : roll < 0.7f ? NPCBehaviour.Speech : NPCBehaviour.None;
                return roll < 0.7f ? NPCBehaviour.Pray : roll < 0.9f ? NPCBehaviour.Speech : NPCBehaviour.None;
            case 2:
                if (healthy) return roll < 0.5f ? NPCBehaviour.Pray : roll < 0.8f ? NPCBehaviour.Speech : NPCBehaviour.None;
                return roll < 0.6f ? NPCBehaviour.Pray : NPCBehaviour.None;
            default:
                if (healthy) return roll < 0.3f ? NPCBehaviour.Pray : roll < 0.6f ? NPCBehaviour.Speech : NPCBehaviour.None;
                return roll < 0.6f ? NPCBehaviour.Pray : NPCBehaviour.None;
        }
    }

    private static void ApplyNpcIdlePenalty(Cardinal candidate)
    {
        if (UnityEngine.Random.value < 0.1f) candidate.ChangeInfluence(-1f);
        if (UnityEngine.Random.value < 0.1f) candidate.ChangePiety(-1f);
        if (UnityEngine.Random.value < 0.1f) candidate.ChangePiety(-2f);
        if (UnityEngine.Random.value < 0.1f) candidate.ChangePiety(-3f);
    }

    private void ExecuteRemainingNpcBaseActions()
    {
        for (int candidateIndex = 0; candidateIndex < 3; candidateIndex++)
        {
            for (int actionIndex = 0; actionIndex < 2; actionIndex++)
            {
                if (npcTurnActionsExecuted[candidateIndex, actionIndex]) continue;

                npcTurnActionsExecuted[candidateIndex, actionIndex] = true;
                Cardinal candidate = GetRepresentativeCandidate(candidateIndex + 1);
                if (candidate == null || candidate.Hp <= 0f || candidate.IsKnockedOut) continue;
                ExecuteNpcBehaviour(candidate, npcTurnBehaviours[candidateIndex, actionIndex]);
            }
        }
    }

    private void ExecuteNpcBehaviour(Cardinal candidate, NPCBehaviour behaviour)
    {
        switch (behaviour)
        {
            case NPCBehaviour.Pray:
                candidate.PerformNpcPrayer(balance.PraySuccessChance);
                break;
            case NPCBehaviour.Speech:
                candidate.PerformNpcSpeech(GetSpeechSuccessChance(candidate));
                break;
            case NPCBehaviour.None:
            case NPCBehaviour.ActionBlocked:
                ApplyNpcIdlePenalty(candidate);
                break;
            case NPCBehaviour.PlayerExtraAction:
                break;
        }

        candidate.ResolveHpState();
    }

    private void PrepareNpcPassives()
    {
        Cardinal candidate3 = GetRepresentativeCandidate(3);
        if (candidate3 != null) candidate3.SetMaxHp(15f);
    }

    private Cardinal GetRepresentativeCandidate(int candidateNumber)
    {
        if (candidateNumber < 1 || candidateNumber > 3 || CardinalManager.Instance == null) return null;
        StatsUI statsUI = CardinalManager.Instance.StatsUI;
        Cardinal[] linked = statsUI != null ? statsUI.LinkedCardinals : null;
        if (linked != null && linked.Length > candidateNumber && linked[candidateNumber] != null)
            return linked[candidateNumber];

        List<Cardinal> aiCandidates = CardinalManager.Instance.GetAICardinlas();
        return aiCandidates.Count >= candidateNumber ? aiCandidates[candidateNumber - 1] : null;
    }

    public List<Cardinal> GetRepresentativeCandidates()
    {
        List<Cardinal> candidates = new List<Cardinal>();
        Cardinal player = FindPlayerCardinal();
        if (player != null) candidates.Add(player);

        for (int candidateNumber = 1; candidateNumber <= 3; candidateNumber++)
        {
            Cardinal candidate = GetRepresentativeCandidate(candidateNumber);
            if (candidate != null && !candidates.Contains(candidate)) candidates.Add(candidate);
        }

        return candidates;
    }

    public void ScheduleNextDayInfluenceRestore(string sourceId, Cardinal owner, float amount)
    {
        RegisterPendingEffect(PendingEffectType.P021RestoreInfluence, sourceId, owner,
            gameContext.CurrentDay + 1, GameContext.Conclave.Dawn, amount);
    }

    public void ScheduleNextConclaveRevenge(string sourceId, Cardinal owner)
    {
        int triggerDay = gameContext.CurrentDay;
        GameContext.Conclave triggerConclave = gameContext.CurrentConclave;
        if (triggerConclave == GameContext.Conclave.Evening)
        {
            triggerDay++;
            triggerConclave = GameContext.Conclave.Dawn;
        }
        else
        {
            triggerConclave++;
        }

        RegisterPendingEffect(PendingEffectType.P033RevengeDamage, sourceId, owner,
            triggerDay, triggerConclave, 0f);
    }

    public void RecordPendingHpLoss(Cardinal owner, float actualLoss)
    {
        if (owner == null || actualLoss <= 0f || gameContext == null || pendingEffects.Count == 0) return;
        int ownerCandidateNumber = GetCandidateNumber(owner);

        foreach (PendingEffectSaveData effect in pendingEffects)
        {
            if (effect == null || effect.effectType != (int)PendingEffectType.P033RevengeDamage ||
                effect.ownerCandidateNumber != ownerCandidateNumber || effect.createdDay != gameContext.CurrentDay ||
                effect.createdConclave != (int)gameContext.CurrentConclave) continue;
            effect.accumulatedValue += actualLoss;
        }
    }

    private void RegisterPendingEffect(PendingEffectType effectType, string sourceId, Cardinal owner,
        int triggerDay, GameContext.Conclave triggerConclave, float initialValue)
    {
        if (owner == null) return;
        int ownerCandidateNumber = GetCandidateNumber(owner);
        if (ownerCandidateNumber < 0) return;
        pendingEffects.Add(new PendingEffectSaveData
        {
            id = Guid.NewGuid().ToString("N"),
            sourceId = sourceId ?? string.Empty,
            effectType = (int)effectType,
            ownerCandidateNumber = ownerCandidateNumber,
            createdDay = gameContext.CurrentDay,
            createdConclave = (int)gameContext.CurrentConclave,
            triggerDay = triggerDay,
            triggerConclave = (int)triggerConclave,
            accumulatedValue = initialValue
        });
    }

    private void ProcessPendingEffects()
    {
        if (pendingEffects.Count == 0 || gameContext == null) return;

        for (int index = pendingEffects.Count - 1; index >= 0; index--)
        {
            PendingEffectSaveData effect = pendingEffects[index];
            if (effect == null)
            {
                pendingEffects.RemoveAt(index);
                continue;
            }

            bool isDue = gameContext.CurrentDay > effect.triggerDay ||
                gameContext.CurrentDay == effect.triggerDay && (int)gameContext.CurrentConclave >= effect.triggerConclave;
            if (!isDue) continue;

            Cardinal owner = GetCandidateByNumber(effect.ownerCandidateNumber);
            foreach (Cardinal target in GetRepresentativeCandidates())
            {
                if (target == null || target == owner) continue;
                switch ((PendingEffectType)effect.effectType)
                {
                    case PendingEffectType.P021RestoreInfluence:
                        target.ChangeInfluence(effect.accumulatedValue);
                        break;
                    case PendingEffectType.P033RevengeDamage:
                        if (effect.accumulatedValue <= 0f) break;
                        float hpDelta = eventManager != null
                            ? eventManager.ModifyPlotHpDelta(owner, target, -effect.accumulatedValue)
                            : -effect.accumulatedValue;
                        target.ChangeHp(hpDelta);
                        target.ResolveHpState();
                        break;
                }
            }

            pendingEffects.RemoveAt(index);
        }
    }

    private int GetCandidateNumber(Cardinal candidate)
    {
        if (candidate == null) return -1;
        if (candidate == FindPlayerCardinal()) return 0;
        int npcCandidateNumber = GetNpcCandidateNumber(candidate);
        return npcCandidateNumber > 0 ? npcCandidateNumber : -1;
    }

    private Cardinal GetCandidateByNumber(int candidateNumber)
    {
        return candidateNumber == 0 ? FindPlayerCardinal() : GetRepresentativeCandidate(candidateNumber);
    }

    private static PendingEffectSaveData ClonePendingEffect(PendingEffectSaveData effect)
    {
        return new PendingEffectSaveData
        {
            id = effect.id,
            sourceId = effect.sourceId,
            effectType = effect.effectType,
            ownerCandidateNumber = effect.ownerCandidateNumber,
            createdDay = effect.createdDay,
            createdConclave = effect.createdConclave,
            triggerDay = effect.triggerDay,
            triggerConclave = effect.triggerConclave,
            accumulatedValue = effect.accumulatedValue
        };
    }

    private void RestorePendingEffects(List<PendingEffectSaveData> savedEffects)
    {
        pendingEffects.Clear();
        if (savedEffects == null) return;
        foreach (PendingEffectSaveData effect in savedEffects)
        {
            if (effect == null || effect.effectType < (int)PendingEffectType.P021RestoreInfluence ||
                effect.effectType > (int)PendingEffectType.P033RevengeDamage) continue;
            PendingEffectSaveData restored = ClonePendingEffect(effect);
            restored.ownerCandidateNumber = Mathf.Clamp(restored.ownerCandidateNumber, 0, 3);
            restored.createdConclave = Mathf.Clamp(restored.createdConclave, 0, 3);
            restored.triggerConclave = Mathf.Clamp(restored.triggerConclave, 0, 3);
            pendingEffects.Add(restored);
        }
    }

    private Cardinal GetLeadingCandidate()
    {
        if (CardinalManager.Instance == null) return null;
        List<Cardinal> candidates = new List<Cardinal>();
        Cardinal player = FindPlayerCardinal();
        if (player != null) candidates.Add(player);
        for (int candidateNumber = 1; candidateNumber <= 3; candidateNumber++)
        {
            Cardinal candidate = GetRepresentativeCandidate(candidateNumber);
            if (candidate != null && !candidates.Contains(candidate)) candidates.Add(candidate);
        }

        Cardinal leader = null;
        foreach (Cardinal candidate in candidates)
        {
            if (candidate == null || candidate.Hp <= 0f) continue;
            if (leader == null || CompareCandidateRank(candidate, leader) > 0) leader = candidate;
        }
        return leader;
    }

    private static int CompareCandidateRank(Cardinal left, Cardinal right)
    {
        float leftHigh = Mathf.Max(left.Influence, left.Piety);
        float rightHigh = Mathf.Max(right.Influence, right.Piety);
        int highComparison = leftHigh.CompareTo(rightHigh);
        if (highComparison != 0) return highComparison;
        return Mathf.Min(left.Influence, left.Piety).CompareTo(Mathf.Min(right.Influence, right.Piety));
    }

    private void RestoreNpcTurnPlan(GameContextSaveData saveData)
    {
        for (int candidate = 0; candidate < 3; candidate++)
        {
            npcNextTurnActionBlocked[candidate] = saveData.npcNextTurnActionBlocked != null &&
                candidate < saveData.npcNextTurnActionBlocked.Count && saveData.npcNextTurnActionBlocked[candidate];
            for (int action = 0; action < 4; action++)
            {
                int index = candidate * 4 + action;
                npcTurnBehaviours[candidate, action] = saveData.npcTurnBehaviours != null && index < saveData.npcTurnBehaviours.Count
                    ? (NPCBehaviour)Mathf.Clamp(saveData.npcTurnBehaviours[index], 0, 4)
                    : NPCBehaviour.None;
                npcTurnActionsExecuted[candidate, action] = saveData.npcTurnActionsExecuted != null &&
                    index < saveData.npcTurnActionsExecuted.Count && saveData.npcTurnActionsExecuted[index];
            }
        }
        PrepareNpcPassives();
    }

    public void BlockNpcNextTurnAction(int candidateNumber)
    {
        if (candidateNumber < 1 || candidateNumber > 3) return;
        npcNextTurnActionBlocked[candidateNumber - 1] = true;
    }

    public void CompletePlayerAction(Cardinal performer)
    {
        if (performer == null || !performer.CompareTag("Player") || !CanPerformPlayerAction(performer)) return;
        if (!gameContext.CompleteAction()) return;
        if (blockRemainingCurrentTurn)
        {
            blockRemainingCurrentTurn = false;
            gameContext.CompleteRemainingActions();
        }
        if (queuedImmediateEvent != null)
        {
            Event immediateEvent = queuedImmediateEvent;
            queuedImmediateEvent = null;
            OpenEventBeforeActions(immediateEvent);
            SaveTurnPhaseCheckpoint(SaveResumeStep.ReopenPendingEvent);
            return;
        }
        if (gameContext.AreActionsComplete())
        {
            ResolveCompletedTurn();
            return;
        }

        SaveTurnPhaseCheckpoint(SaveResumeStep.Gameplay);
    }

    public void QueueImmediateEventAfterPlayerAction(Event evt)
    {
        if (evt != null && queuedImmediateEvent == null) queuedImmediateEvent = evt;
    }

    private void OpenEventBeforeActions(Event evt)
    {
        if (evt == null) return;
        gameContext.SetEvent(evt);
        awaitingTurnEvent = true;
        eventBeforeActions = true;
        if (UIManager.Instance != null && UIManager.Instance.Ingame != null)
            UIManager.Instance.Ingame.Event.UISetEvent();
    }

    public void QueueNextTurnActionDelta(int delta)
    {
        nextTurnActionModifier = Mathf.Clamp(nextTurnActionModifier + delta, -2, 1);
    }

    public void AddCurrentTurnActions(int count)
    {
        int previousActionCount = gameContext.ActionsThisTurn;
        gameContext.AddCurrentTurnActions(count);
        for (int actionIndex = Mathf.Max(2, previousActionCount); actionIndex < gameContext.ActionsThisTurn; actionIndex++)
        {
            for (int candidateIndex = 0; candidateIndex < 3; candidateIndex++)
            {
                npcTurnBehaviours[candidateIndex, actionIndex] = NPCBehaviour.PlayerExtraAction;
                npcTurnActionsExecuted[candidateIndex, actionIndex] = false;
            }
        }
    }

    public void BlockPlayerTurnActions()
    {
        if (awaitingTurnEvent || gameContext.IsEventPhase) blockNextTurn = true;
        else blockRemainingCurrentTurn = true;
    }

    public void OnTurnEventClosed()
    {
        if (!awaitingTurnEvent) return;
        awaitingTurnEvent = false;
        if (eventBeforeActions)
        {
            eventBeforeActions = false;
            if (gameContext.AreActionsComplete())
            {
                ResolveCompletedTurn();
            }
            else
            {
                SaveTurnPhaseCheckpoint(SaveResumeStep.Gameplay);
            }
            return;
        }
        if (endConclaveAfterEvent)
        {
            endConclaveAfterEvent = false;
            FinishCurrentConclave();
            return;
        }
        StartNextTurnOrEndConclave();
    }

    public void EndCurrentConclave()
    {
        if (!isTimeRunning || isConclaveExitInProgress) return;
        if (awaitingTurnEvent)
        {
            endConclaveAfterEvent = true;
            return;
        }
        FinishCurrentConclave();
    }

    private void FinishCurrentConclave()
    {
        StopTimer();
        awaitingTurnEvent = false;
        eventBeforeActions = false;
        queuedImmediateEvent = null;
        gameContext.EndConclave();
    }

    public void RestorePendingTurnEventUI()
    {
        if (!awaitingTurnEvent || gameContext.CurrentEvent == null || UIManager.Instance == null ||
            UIManager.Instance.Ingame == null) return;
        UIManager.Instance.Ingame.Event.UISetEvent();
    }

    public void ResumeAfterResolvedEvent()
    {
        OnTurnEventClosed();
    }

    private void ResolveCompletedTurn()
    {
        ExecuteRemainingNpcBaseActions();
        if (!ApplyTurnEndHealthLoss()) return;

        if (gameContext.CurrentTurn >= 4)
        {
            EndCurrentConclave();
            return;
        }

        gameContext.EnterEventPhase();
        awaitingTurnEvent = true;
        gameContext.SetNewEvent();

        if (gameContext.CurrentEvent != null && UIManager.Instance != null && UIManager.Instance.Ingame != null)
        {
            UIManager.Instance.Ingame.Event.UISetEvent();
            SaveTurnPhaseCheckpoint(SaveResumeStep.ReopenPendingEvent);
        }
        else
        {
            Debug.LogWarning("[Turn] 표시할 이벤트가 없어 다음 턴으로 진행합니다.");
            OnTurnEventClosed();
        }
    }

    private bool ApplyTurnEndHealthLoss()
    {
        PrepareNpcPassives();
        List<Cardinal> candidates = new List<Cardinal>();
        Cardinal player = FindPlayerCardinal();
        if (player != null) candidates.Add(player);
        for (int candidateNumber = 1; candidateNumber <= 3; candidateNumber++)
        {
            Cardinal candidate = GetRepresentativeCandidate(candidateNumber);
            if (candidate != null && !candidates.Contains(candidate)) candidates.Add(candidate);
        }

        Cardinal candidate3 = GetRepresentativeCandidate(3);
        bool candidate3WasLeading = candidate3 != null && IsNpcCandidateLeading(3);

        foreach (Cardinal candidate in candidates)
        {
            if (candidate == null || candidate.Hp <= 0f) continue;
            candidate.ChangeHp(-1f);
        }

        if (candidate3 != null && candidate3.Hp > 0f && candidate3WasLeading && UnityEngine.Random.value < 0.3f)
            candidate3.ChangeHp(-1f);

        foreach (Cardinal candidate in candidates) candidate?.ResolveHpState();
        return player == null || player.Hp > 0f;
    }

    private void StartNextTurnOrEndConclave()
    {
        if (!gameContext.AdvanceTurn())
        {
            EndCurrentConclave();
            return;
        }

        gameContext.BeginTurn(ConsumeNextTurnActionModifier(), ConsumeNextTurnBlock());
        TryResolveTurnWithoutActions();

        if (isTimeRunning && SaveManager.Instance != null)
        {
            SaveManager.Instance.SaveCheckpoint(
                SaveCheckpointType.TurnPhaseAdvanced,
                awaitingTurnEvent ? SaveResumeStep.ReopenPendingEvent : SaveResumeStep.Gameplay);
        }
    }

    private void SaveTurnPhaseCheckpoint(SaveResumeStep resumeStep)
    {
        if (isTimeRunning && SaveManager.Instance != null)
        {
            SaveManager.Instance.SaveCheckpoint(SaveCheckpointType.TurnPhaseAdvanced, resumeStep);
        }
    }

    private void TryResolveTurnWithoutActions()
    {
        if (isTimeRunning && gameContext.AreActionsComplete()) ResolveCompletedTurn();
    }

    private int ConsumeNextTurnActionModifier()
    {
        int modifier = nextTurnActionModifier;
        nextTurnActionModifier = 0;
        return modifier;
    }

    private bool ConsumeNextTurnBlock()
    {
        bool blocked = blockNextTurn;
        blockNextTurn = false;
        return blocked;
    }
    public Event GetCurrentEvent()
    {
        return gameContext.CurrentEvent;
    }
}
