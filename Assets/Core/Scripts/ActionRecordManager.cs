using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;

public class ActionRecordManager : MonoBehaviour
{
    private const int CurrentPapalElectionHistoryVersion = 1;
    private const string SaveFolderName = "Json";
    private const string PersistentStatsFileName = "action_stats_persistent.json";
    private const string GameSceneName = "GameScene";
    private const float PersistentSaveInterval = 1f;

    public static ActionRecordManager Instance { get; private set; }

    private ActionStatsSaveData currentRunStats = new ActionStatsSaveData();
    private ActionStatsSaveData persistentStats = new ActionStatsSaveData();
    private float persistentSaveTimer;
    private float currentRunSaveTimer;
    private bool currentRunDirty;

    public ActionStatsSaveData CurrentRunStats => currentRunStats;
    public ActionStatsSaveData PersistentStats => persistentStats;

    private string SaveDirectoryPath => Path.Combine(Application.persistentDataPath, SaveFolderName);
    private string PersistentStatsFilePath => Path.Combine(SaveDirectoryPath, PersistentStatsFileName);

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Bootstrap()
    {
        if (Instance != null)
        {
            return;
        }

        GameObject managerObject = new GameObject(nameof(ActionRecordManager));
        DontDestroyOnLoad(managerObject);
        Instance = managerObject.AddComponent<ActionRecordManager>();
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
        LoadPersistentStats();
    }

    private void Update()
    {
        TrackPlayerStatThresholdTimes();
        SavePersistentStatsPeriodically();
        SaveCurrentRunStatsPeriodically();
    }

    public void RestoreCurrentRunStats(ActionStatsSaveData saveData)
    {
        currentRunStats = saveData != null ? saveData.Clone() : new ActionStatsSaveData();
        currentRunDirty = false;
    }

    public ActionStatsSaveData CaptureCurrentRunStats()
    {
        return currentRunStats.Clone();
    }

    public void ClearCurrentRunStats()
    {
        currentRunStats = new ActionStatsSaveData();
        currentRunDirty = false;
    }

    public void ResetPersistentData()
    {
        currentRunStats = new ActionStatsSaveData();
        persistentStats = new ActionStatsSaveData();
        currentRunDirty = false;
        currentRunSaveTimer = 0f;
        persistentSaveTimer = 0f;

        try
        {
            if (File.Exists(PersistentStatsFilePath))
            {
                File.Delete(PersistentStatsFilePath);
            }
        }
        catch (Exception exception)
        {
            Debug.LogError($"[ActionRecord] Persistent stats delete failed: {exception}");
        }
    }

    public void RecordPray(Cardinal cardinal)
    {
        if (!IsPlayer(cardinal))
        {
            return;
        }

        currentRunStats.prayCount++;
        persistentStats.prayCount++;
        currentRunDirty = true;
        SavePersistentStats();
    }

    public void RecordSpeech(Cardinal cardinal)
    {
        if (!IsPlayer(cardinal))
        {
            return;
        }

        currentRunStats.speechCount++;
        persistentStats.speechCount++;
        currentRunDirty = true;
        SavePersistentStats();
    }

    public void RecordPlot(Cardinal performer)
    {
        if (!IsPlayer(performer))
        {
            return;
        }

        currentRunStats.plotCount++;
        persistentStats.plotCount++;
        currentRunDirty = true;
        SavePersistentStats();
    }

    public void RecordItemAcquired(Item item)
    {
        if (item == null)
        {
            return;
        }

        currentRunStats.RecordItemAcquired(item.itemID, item.itemName);
        persistentStats.RecordItemAcquired(item.itemID, item.itemName);
        currentRunDirty = true;
        SavePersistentStats();
    }

    public void RecordKnockOut(Cardinal cardinal)
    {
        if (!IsPlayer(cardinal))
        {
            return;
        }

        currentRunStats.stunCount++;
        persistentStats.stunCount++;
        currentRunDirty = true;
        SavePersistentStats();
    }

    public void RecordHealthGameOver()
    {
        currentRunStats.healthGameOverCount++;
        persistentStats.healthGameOverCount++;
        currentRunDirty = true;
        SavePersistentStats();
    }

    public void RecordPapalElection(EndingType endingType)
    {
        CandidateSlot candidateSlot = endingType == EndingType.PlayerPope
            ? CandidateSlot.Player
            : CandidateSlot.Unknown;
        RecordPapalElection(endingType, string.Empty, candidateSlot);
    }

    public void RecordPapalElection(EndingType endingType, string popeName)
    {
        CandidateSlot candidateSlot = endingType == EndingType.PlayerPope
            ? CandidateSlot.Player
            : CandidateSlot.Unknown;
        RecordPapalElection(endingType, popeName, candidateSlot);
    }

    public void RecordPapalElection(
        EndingType endingType,
        string popeName,
        CandidateSlot candidateSlot)
    {
        bool isPapalElectionEnding = endingType == EndingType.PlayerPope || endingType == EndingType.NpcPope;

        if (isPapalElectionEnding)
        {
            currentRunStats.papalElectionCount++;
            persistentStats.papalElectionCount++;

            persistentStats.currentPopeGeneration++;
            currentRunStats.currentPopeGeneration = persistentStats.currentPopeGeneration;

            EnsurePersistentCollections();
            persistentStats.papalElectionHistory.Add(new PapalElectionRecordSaveData
            {
                generation = persistentStats.currentPopeGeneration,
                popeName = string.IsNullOrWhiteSpace(popeName) ? "Unknown" : popeName.Trim(),
                isPlayer = endingType == EndingType.PlayerPope,
                candidateSlot = candidateSlot,
                electedAtUtc = DateTime.UtcNow.ToString("O")
            });
        }

        switch (endingType)
        {
            case EndingType.PlayerPope:
                currentRunStats.badEndingCount++;
                persistentStats.badEndingCount++;
                break;
            case EndingType.NpcPope:
                currentRunStats.happyEndingCount++;
                persistentStats.happyEndingCount++;
                break;
        }

        currentRunDirty = true;
        SavePersistentStats();
    }

    public void RecordPapalElectionFailed()
    {
        currentRunStats.papalElectionFailedCount++;
        persistentStats.papalElectionFailedCount++;
        currentRunDirty = true;
        SavePersistentStats();
    }

    public void RecordConclaveStarted()
    {
        currentRunStats.conclaveCount++;
        persistentStats.conclaveCount++;
        currentRunDirty = true;
        SavePersistentStats();
    }

    public int GetCurrentPrayCount() => currentRunStats.prayCount;
    public int GetPersistentPrayCount() => persistentStats.prayCount;
    public int GetCurrentSpeechCount() => currentRunStats.speechCount;
    public int GetPersistentSpeechCount() => persistentStats.speechCount;
    public int GetCurrentPlotCount() => currentRunStats.plotCount;
    public int GetPersistentPlotCount() => persistentStats.plotCount;
    public int GetCurrentItemAcquireTotalCount() => currentRunStats.itemAcquireTotalCount;
    public int GetPersistentItemAcquireTotalCount() => persistentStats.itemAcquireTotalCount;
    public string GetCurrentMostAcquiredItemName() => currentRunStats.GetMostAcquiredItemName();
    public string GetPersistentMostAcquiredItemName() => persistentStats.GetMostAcquiredItemName();
    public float GetCurrentHighPietyTime() => currentRunStats.highPietyTime;
    public float GetPersistentHighPietyTime() => persistentStats.highPietyTime;
    public float GetCurrentHighInfluenceTime() => currentRunStats.highInfluenceTime;
    public float GetPersistentHighInfluenceTime() => persistentStats.highInfluenceTime;
    public float GetCurrentLowPietyTime() => currentRunStats.lowPietyTime;
    public float GetPersistentLowPietyTime() => persistentStats.lowPietyTime;
    public float GetCurrentLowInfluenceTime() => currentRunStats.lowInfluenceTime;
    public float GetPersistentLowInfluenceTime() => persistentStats.lowInfluenceTime;
    public int GetCurrentStunCount() => currentRunStats.stunCount;
    public int GetPersistentStunCount() => persistentStats.stunCount;
    public int GetCurrentHealthGameOverCount() => currentRunStats.healthGameOverCount;
    public int GetPersistentHealthGameOverCount() => persistentStats.healthGameOverCount;
    public int GetCurrentBadEndingCount() => currentRunStats.badEndingCount;
    public int GetPersistentBadEndingCount() => persistentStats.badEndingCount;
    public int GetCurrentHappyEndingCount() => currentRunStats.happyEndingCount;
    public int GetPersistentHappyEndingCount() => persistentStats.happyEndingCount;
    public int GetCurrentPapalElectionCount() => currentRunStats.papalElectionCount;
    public int GetPersistentPapalElectionCount() => persistentStats.papalElectionCount;
    public int GetCurrentPapalElectionFailedCount() => currentRunStats.papalElectionFailedCount;
    public int GetPersistentPapalElectionFailedCount() => persistentStats.papalElectionFailedCount;
    public int GetCurrentPopeGeneration() => currentRunStats.currentPopeGeneration;
    public int GetPersistentPopeGeneration() => persistentStats.currentPopeGeneration;

    public IReadOnlyList<PapalElectionRecordSaveData> GetPersistentPapalElectionHistory()
    {
        EnsurePersistentCollections();
        return persistentStats.papalElectionHistory.AsReadOnly();
    }

    public PapalElectionRecordSaveData GetLatestPope()
    {
        EnsurePersistentCollections();
        int count = persistentStats.papalElectionHistory.Count;
        return count > 0 ? persistentStats.papalElectionHistory[count - 1] : null;
    }

    public int GetCurrentConclaveCount() => currentRunStats.conclaveCount;
    public int GetPersistentConclaveCount() => persistentStats.conclaveCount;
    public float GetCurrentConclaveDays() => currentRunStats.conclaveCount / 4f;
    public float GetPersistentConclaveDays() => persistentStats.conclaveCount / 4f;

    private void TrackPlayerStatThresholdTimes()
    {
        if (SceneManager.GetActiveScene().name != GameSceneName ||
            InGameManager.Instance == null ||
            !InGameManager.Instance.IsTimeRunning)
        {
            return;
        }

        Cardinal player = FindPlayerCardinal();
        if (player == null)
        {
            return;
        }

        float deltaTime = Time.deltaTime;

        if (player.Piety >= 80f)
        {
            currentRunStats.highPietyTime += deltaTime;
            persistentStats.highPietyTime += deltaTime;
            currentRunDirty = true;
        }

        if (player.Influence >= 80f)
        {
            currentRunStats.highInfluenceTime += deltaTime;
            persistentStats.highInfluenceTime += deltaTime;
            currentRunDirty = true;
        }

        if (player.Piety <= 20f)
        {
            currentRunStats.lowPietyTime += deltaTime;
            persistentStats.lowPietyTime += deltaTime;
            currentRunDirty = true;
        }

        if (player.Influence <= 20f)
        {
            currentRunStats.lowInfluenceTime += deltaTime;
            persistentStats.lowInfluenceTime += deltaTime;
            currentRunDirty = true;
        }
    }

    private Cardinal FindPlayerCardinal()
    {
        if (InventoryManager.Instance != null && InventoryManager.Instance.Player != null)
        {
            return InventoryManager.Instance.Player;
        }

        Cardinal[] cardinals = FindObjectsByType<Cardinal>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None);

        foreach (Cardinal cardinal in cardinals)
        {
            if (IsPlayer(cardinal))
            {
                return cardinal;
            }
        }

        return null;
    }

    private static bool IsPlayer(Cardinal cardinal)
    {
        return cardinal != null && cardinal.CompareTag("Player");
    }

    private void SavePersistentStatsPeriodically()
    {
        persistentSaveTimer += Time.unscaledDeltaTime;

        if (persistentSaveTimer < PersistentSaveInterval)
        {
            return;
        }

        persistentSaveTimer = 0f;
        SavePersistentStats();
    }

    private void SaveCurrentRunStatsPeriodically()
    {
        if (!currentRunDirty)
        {
            return;
        }

        currentRunSaveTimer += Time.unscaledDeltaTime;

        if (currentRunSaveTimer < PersistentSaveInterval)
        {
            return;
        }

        currentRunSaveTimer = 0f;
        currentRunDirty = false;

        if (SaveManager.Instance != null)
        {
            SaveManager.Instance.AutoSave();
        }
    }

    private void LoadPersistentStats()
    {
        if (!File.Exists(PersistentStatsFilePath))
        {
            persistentStats = new ActionStatsSaveData();
            EnsurePersistentCollections();
            return;
        }

        try
        {
            string json = File.ReadAllText(PersistentStatsFilePath);
            persistentStats = JsonUtility.FromJson<ActionStatsSaveData>(json) ?? new ActionStatsSaveData();
            bool historyWasMigrated = EnsurePersistentCollections();
            if (historyWasMigrated)
            {
                SavePersistentStats();
            }
        }
        catch (Exception exception)
        {
            Debug.LogError($"[ActionRecord] Persistent stats load failed: {exception}");
            persistentStats = new ActionStatsSaveData();
            EnsurePersistentCollections();
        }
    }

    private void SavePersistentStats()
    {
        try
        {
            Directory.CreateDirectory(SaveDirectoryPath);
            File.WriteAllText(PersistentStatsFilePath, JsonUtility.ToJson(persistentStats, true));
        }
        catch (Exception exception)
        {
            Debug.LogError($"[ActionRecord] Persistent stats save failed: {exception}");
        }
    }

    private bool EnsurePersistentCollections()
    {
        bool changed = false;

        if (persistentStats == null)
        {
            persistentStats = new ActionStatsSaveData();
            changed = true;
        }

        if (persistentStats.itemAcquireCounts == null)
        {
            persistentStats.itemAcquireCounts = new List<ItemAcquireCountSaveData>();
            changed = true;
        }

        if (persistentStats.papalElectionHistory == null)
        {
            persistentStats.papalElectionHistory = new List<PapalElectionRecordSaveData>();
            changed = true;
        }

        if (persistentStats.papalElectionHistoryVersion < CurrentPapalElectionHistoryVersion)
        {
            persistentStats.papalElectionHistory.Clear();
            persistentStats.papalElectionHistoryVersion = CurrentPapalElectionHistoryVersion;
            changed = true;
        }

        return changed;
    }
}
