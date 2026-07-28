using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

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
    private bool endConclaveAfterEvent;

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
        endConclaveAfterEvent = false;

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
                break;

            case GameContext.GameContextEvent.ConclaveEnd:
                isConclaveExitInProgress = true;
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
        return new GameContextSaveData
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
            endConclaveAfterEvent = endConclaveAfterEvent,
            currentEventId = gameContext.CurrentEvent != null ? gameContext.CurrentEvent.eventID : string.Empty,
            isTimeRunning = isTimeRunning,
            isFirstStart = isFirstStart,
            isSushiOn = isSushiOn,
            showStartButton = startButton != null && startButton.gameObject.activeSelf,
            startButtonInteractable = startButton == null || startButton.interactable,
            showInventoryPanel = inventoryUIPanel != null && inventoryUIPanel.activeSelf
        };
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
        endConclaveAfterEvent = saveData.endConclaveAfterEvent;
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

    public void CompletePlayerAction(Cardinal performer)
    {
        if (performer == null || !performer.CompareTag("Player") || !CanPerformPlayerAction(performer)) return;
        if (!gameContext.CompleteAction()) return;
        if (blockRemainingCurrentTurn)
        {
            blockRemainingCurrentTurn = false;
            gameContext.CompleteRemainingActions();
        }
        if (gameContext.AreActionsComplete()) ResolveCompletedTurn();
    }

    public void QueueNextTurnActionDelta(int delta)
    {
        nextTurnActionModifier = Mathf.Clamp(nextTurnActionModifier + delta, -2, 1);
    }

    public void AddCurrentTurnActions(int count)
    {
        gameContext.AddCurrentTurnActions(count);
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
        gameContext.EndConclave();
    }

    public void RestorePendingTurnEventUI()
    {
        if (!awaitingTurnEvent || gameContext.CurrentEvent == null || UIManager.Instance == null ||
            UIManager.Instance.Ingame == null) return;
        UIManager.Instance.Ingame.Event.UISetEvent();
    }

    private void ResolveCompletedTurn()
    {
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
        }
        else
        {
            Debug.LogWarning("[Turn] 표시할 이벤트가 없어 다음 턴으로 진행합니다.");
            OnTurnEventClosed();
        }
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
