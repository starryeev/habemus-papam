using System.Collections.Generic;
using System.Collections;
using System.Linq;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;

[System.Serializable]
public class ConclavePathData
{
    public string groupName;
    public Transform spawnPoint;       // 시작 위치
    public Transform[] waypoints;      // 경유지 목록
}

public class CardinalManager : MonoBehaviour
{
    private const string CardinalEnterExitSfxName = "13 인게임- NPC등장 및 퇴장";

    [Header("프리팹 및 기본 설정")]
    [Tooltip("플레이어가 조종하는 추기경 프리팹")]
    [SerializeField] private GameObject cardinalPrefabPlayer;
    [Tooltip("AI가 조종하는 추기경 프리팹")]
    [SerializeField] private GameObject cardinalPrefabAI;
    [Tooltip("콘클라베 시작시 입장하는 NPC 수")]
    [SerializeField] private int spawnNPCCount = 20;

    [Header("경로 설정")]
    [SerializeField] private List<ConclavePathData> conclavePaths;

    [Header("퇴장 줄세우기 설정")]
    [SerializeField] private Transform leftLineStart;
    [SerializeField] private Transform leftLineEnd;
    [SerializeField] private Transform rightLineStart;
    [SerializeField] private Transform rightLineEnd;
    [SerializeField] private Transform leftExitPoint;
    [SerializeField] private Transform rightExitPoint;

    // 카디널들 관리하는 리스트
    private List<Cardinal> cardinals = new List<Cardinal>();
    public List<Cardinal> Cardinals => cardinals;
    public Transform PlayerTransform { get; private set; }
    public bool IsConclaveTransitionInProgress { get; private set; }

    private List<Cardinal> leftGroupList = new List<Cardinal>();
    private List<Cardinal> rightGroupList = new List<Cardinal>();
    private List<Vector3> leftLinePositions = new List<Vector3>();
    private List<Vector3> rightLinePositions = new List<Vector3>();
    private int exitingCardinalCount;
    private EventSystem blockedEventSystem;
    private InputSystemUIInputModule blockedInputModule;
    private Coroutine enableInputCoroutine;
    private bool schemersAssignedThisConclave;

    [SerializeField] private StatsUI statsUI;
    public StatsUI StatsUI => statsUI;

    public static CardinalManager Instance { get; private set; }

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        InitializeCardinals();
    }

    private void Start()
    {
        SetConclaveTransition(true);
    }

    public List<Cardinal> GetAICardinlas()
    {
        var aiCardinals = cardinals.Where(c => !c.CompareTag("Player")).ToList();

        return aiCardinals;
    }

    //게임 시작시 NPC, Player 생성 및 대기
    private void InitializeCardinals()
    {
        Transform container = GetOrCreateCardinalsContainer();

        for (int i = 0; i < spawnNPCCount; i++)
        {
            string groupName = (i % 2 == 0) ? "Left" : "Right";
            GameObject obj = Instantiate(cardinalPrefabAI, container);
            obj.name = $"Cardinal_{groupName}_{i}";

            Cardinal c = obj.GetComponent<Cardinal>();
            if (c != null) cardinals.Add(c);

            obj.SetActive(false);
        }

        // 플레이어 생성
        GameObject pObj = Instantiate(cardinalPrefabPlayer, container);
        pObj.name = "Cardinal_Player";
        PlayerTransform = pObj.transform;
        Cardinal pCard = pObj.GetComponent<Cardinal>();
        if (pCard != null) cardinals.Add(pCard);

        pObj.SetActive(false);
    }

    Transform GetOrCreateCardinalsContainer()
    {
        GameObject runtimeObj = GameObject.Find("Runtime");
        if (runtimeObj == null) runtimeObj = new GameObject("Runtime");

        Transform cardinalsTr = runtimeObj.transform.Find("Cardinals");
        if (cardinalsTr == null)
        {
            GameObject cardinalsObj = new GameObject("Cardinals");
            cardinalsTr = cardinalsObj.transform;
            cardinalsTr.SetParent(runtimeObj.transform, false);
        }
        return cardinalsTr;
    }

    // =========================================================
    // 콘클라베 시작
    // =========================================================
    public void StartConClave()
    {
        StopAllCoroutines();
        SetConclaveTransition(true);
        if (statsUI != null)
        {
            statsUI.HideForConclaveEntrance();
        }

        TimeUI timeUI = UIManager.Instance != null && UIManager.Instance.Ingame != null
            ? UIManager.Instance.Ingame.Time
            : null;
        if (timeUI != null)
        {
            timeUI.HideForConclaveEntrance();
        }

        StartCoroutine(ResetAndEnterSequence());
        SoundManager.Instance.PlayBGM("DummyBGM", 1);
    }

    private IEnumerator ResetAndEnterSequence()
    {
        schemersAssignedThisConclave = false;
        ConclavePathData leftPath = conclavePaths.Find(p => p.groupName.Contains("Left"));
        ConclavePathData rightPath = conclavePaths.Find(p => p.groupName.Contains("Right"));
        ConclavePathData playerPath = conclavePaths.Find(p => p.groupName.Contains("Player"));

        if (leftPath == null) leftPath = conclavePaths[0];
        if (rightPath == null) rightPath = conclavePaths[1];

        Time.timeScale = 5f;

        var aiCardinals = cardinals.Where(c => !c.CompareTag("Player")).ToList();

        for (int i = 0; i < aiCardinals.Count; i += 2)
        {
            if (i < aiCardinals.Count)
            {
                Cardinal aiLeft = aiCardinals[i];
                ResetCardinalState(aiLeft, leftPath.spawnPoint.position);

                StateController scLeft = aiLeft.GetComponent<StateController>();
                if (scLeft != null && leftPath.waypoints != null)
                {
                    scLeft.MoveToWaypoints(leftPath.waypoints);
                }
            }

            if (i + 1 < aiCardinals.Count)
            {
                Cardinal aiRight = aiCardinals[i + 1];
                ResetCardinalState(aiRight, rightPath.spawnPoint.position);
                SoundManager.Instance.PlaySFX(CardinalEnterExitSfxName);

                StateController scRight = aiRight.GetComponent<StateController>();
                if (scRight != null && rightPath.waypoints != null)
                {
                    scRight.MoveToWaypoints(rightPath.waypoints);
                }
            }
            yield return new WaitForSeconds(1.5f);
        }

        yield return new WaitForSeconds(5f);

        Time.timeScale = 1f;
        SoundManager.Instance.PlayBGM("DummyBGM", 0);

        Cardinal player = cardinals.Find(c => c.CompareTag("Player"));
        StateController playerSC = null;

        if (player != null && playerPath != null)
        {
            ResetCardinalState(player, playerPath.spawnPoint.position);
            SoundManager.Instance.PlaySFX(CardinalEnterExitSfxName);
            playerSC = player.GetComponent<StateController>();

            if (playerSC != null && playerPath.waypoints != null)
            {
                playerSC.MoveToWaypoints(playerPath.waypoints);
            }
        }

        if (playerSC != null)
        {
            yield return null;
            yield return new WaitUntil(() => playerSC.IsMoving == false);

            var animCtrl = playerSC.GetComponentInChildren<Animation_Controller>();
            if (animCtrl != null)
            {
                animCtrl.SetIndicatorActive(true); 
            }
        }

        foreach (var c in cardinals)
        {
            if (c == null) continue;
            StateController sc = c.GetComponent<StateController>();
            if (sc != null)
            {
                sc.ConClaving = false; 
                sc.ChangeState(CardinalState.Idle); 
            }
        }

        //공작
        if (InGameManager.Instance == null || !InGameManager.Instance.IsInitialTutorialLocked)
        {
            AssignRandomSchemers();
        }

        //스탯 연동과 타이머 연동
        if (statsUI != null)
        {
            statsUI.Initialize(cardinals);
            statsUI.FadeInAfterConclaveEntrance(1f);

            TimeUI timeUI = UIManager.Instance != null && UIManager.Instance.Ingame != null
                ? UIManager.Instance.Ingame.Time
                : null;
            if (timeUI != null)
            {
                timeUI.FadeInAfterConclaveEntrance(1f);
            }

            InGameManager.Instance.StartTimer();
        }

        if (SaveManager.Instance != null)
        {
            SaveManager.Instance.SaveCheckpoint(
                SaveCheckpointType.ConclaveEntryCompleted,
                InGameManager.Instance.IsAwaitingTurnEvent
                    ? SaveResumeStep.ReopenPendingEvent
                    : SaveResumeStep.Gameplay);
        }

        SetConclaveTransition(false);
    }
    private void ResetCardinalState(Cardinal c, Vector3 startPos)
    {
        c.gameObject.SetActive(true);

        StateController sc = c.GetComponent<StateController>();
        if (sc != null)
        {
            sc.ConClaving = false; 
            sc.SetSchemerMode(false);
            sc.StopAllCoroutines();
        }

        NavMeshAgent agent = c.GetComponent<NavMeshAgent>();
        if (agent != null)
        {
            agent.enabled = false;
            c.transform.position = startPos;
            agent.enabled = true;
            agent.ResetPath();
            agent.velocity = Vector3.zero;
        }
        else
        {
            c.transform.position = startPos;
        }
    }

    // =========================================================
    // 콘클라베 종료 
    // =========================================================
    public void StopConClave()
    {
        SetConclaveTransition(true);
        SoundManager.Instance.PlayBGM("DummyBGM", 1);

        Time.timeScale = 5f;
        if (cardinals == null || cardinals.Count == 0)
        {
            SetConclaveTransition(false);
            return;
        }

        exitingCardinalCount = 0;

        // 리스트 초기화
        leftGroupList.Clear();
        rightGroupList.Clear();
        leftLinePositions.Clear();
        rightLinePositions.Clear();

        int totalCount = cardinals.Count;
        int halfCount = totalCount / 2;

        GameObject targetParent = new GameObject("Temp_InitialLineUp");

        for (int i = 0; i < totalCount; i++)
        {
            Cardinal c = cardinals[i];
            if (c == null || !c.gameObject.activeSelf) continue; 

            StateController sc = c.GetComponent<StateController>();
            if (sc == null) continue;

            Vector3 targetPos;

            if (i < halfCount)
            {
                float t = (halfCount > 1) ? (float)i / (halfCount - 1) : 0.5f;
                targetPos = Vector3.Lerp(leftLineStart.position, leftLineEnd.position, t);
                leftGroupList.Add(c);
                leftLinePositions.Add(targetPos);
            }
            else
            {
                int rightIndex = i - halfCount;
                int rightTotal = totalCount - halfCount;
                float t = (rightTotal > 1) ? (float)rightIndex / (rightTotal - 1) : 0.5f;
                targetPos = Vector3.Lerp(rightLineStart.position, rightLineEnd.position, t);
                rightGroupList.Add(c);
                rightLinePositions.Add(targetPos);
            }

            GameObject tempPoint = new GameObject($"InitPos_{i}");
            tempPoint.transform.SetParent(targetParent.transform);
            tempPoint.transform.position = targetPos;

            sc.ConClaving = true;
            c.SetAgentSize(0.1f, 0.1f);

            sc.ReleaseStun(false);   //행동불가 상태라면 풀어주기

            // 모든 코루틴 멈추고 이동 명령 
            sc.MoveToWaypoints(new Transform[] { tempPoint.transform });

        }

        Destroy(targetParent, 1f);
        StartCoroutine(ProcessExitSequence());
    }

    private IEnumerator ProcessExitSequence()
    {
        yield return new WaitForSeconds(5.0f); 

        while (leftGroupList.Count > 0 || rightGroupList.Count > 0)
        {
            if (leftGroupList.Count > 0)
            {
                Cardinal leaver = leftGroupList[0];
                MoveCardinalToExit(leaver, leftExitPoint.position);
                leftGroupList.RemoveAt(0);
            }
            if (rightGroupList.Count > 0)
            {
                Cardinal leaver = rightGroupList[0];
                MoveCardinalToExit(leaver, rightExitPoint.position);
                rightGroupList.RemoveAt(0);
            }

            yield return new WaitForSeconds(0.5f);

            GameObject shiftTargetParent = new GameObject("Temp_ShiftTargets");
            for (int i = 0; i < leftGroupList.Count; i++)
                MoveCardinalToPoint(leftGroupList[i], leftLinePositions[i], shiftTargetParent.transform);
            for (int i = 0; i < rightGroupList.Count; i++)
                MoveCardinalToPoint(rightGroupList[i], rightLinePositions[i], shiftTargetParent.transform);

            Destroy(shiftTargetParent, 1f);
            yield return new WaitForSeconds(2.0f);
        }

        yield return new WaitUntil(() => exitingCardinalCount == 0);
        if (InGameManager.Instance != null)
        {
            InGameManager.Instance.OnConclaveExitSequenceCompleted();
        }
        Time.timeScale = 1f;

        SoundManager.Instance.PlayBGM("DummyBGM");
        SetConclaveTransition(false);
    }
    private void MoveCardinalToExit(Cardinal c, Vector3 exitPos)
    {
        StateController sc = c.GetComponent<StateController>();
        if (sc == null) return;

        GameObject tempObj = new GameObject($"ExitTarget_{c.name}");
        tempObj.transform.position = exitPos;
        Destroy(tempObj, 3f);

        sc.ConClaving = true;
        sc.MoveToWaypoints(new Transform[] { tempObj.transform });

        exitingCardinalCount++;
        StartCoroutine(DeactivateAfterExit(c, sc));
    }

    private IEnumerator DeactivateAfterExit(Cardinal c, StateController sc)
    {
        Camera mainCamera = Camera.main;
        SpriteRenderer spriteRenderer = c.GetComponentInChildren<SpriteRenderer>();
        bool wasVisible = IsVisibleInCamera(mainCamera, spriteRenderer);
        bool exitSoundPlayed = false;

        while (sc.IsMoving)
        {
            bool isVisible = IsVisibleInCamera(mainCamera, spriteRenderer);
            if (!exitSoundPlayed && wasVisible && !isVisible)
            {
                SoundManager.Instance.PlaySFX(CardinalEnterExitSfxName);
                exitSoundPlayed = true;
            }

            wasVisible |= isVisible;
            yield return null;
        }

        if (!exitSoundPlayed)
        {
            SoundManager.Instance.PlaySFX(CardinalEnterExitSfxName);
        }

        c.gameObject.SetActive(false);
        exitingCardinalCount--;

        if (c.CompareTag("Player"))
        {
            if (InGameManager.Instance != null)
            {
                InGameManager.Instance.OnExitSequenceFinished();
            }
        }
    }

    private void SetConclaveTransition(bool active)
    {
        if (active)
        {
            IsConclaveTransitionInProgress = true;

            if (enableInputCoroutine != null)
            {
                StopCoroutine(enableInputCoroutine);
                enableInputCoroutine = null;
            }

            if (blockedEventSystem == null)
            {
                blockedEventSystem = EventSystem.current;
            }

            if (blockedEventSystem != null)
            {
                blockedInputModule = blockedEventSystem.GetComponent<InputSystemUIInputModule>();
                if (blockedInputModule != null)
                {
                    blockedInputModule.enabled = false;
                }

                blockedEventSystem.enabled = false;
            }
        }
        else if (enableInputCoroutine == null)
        {
            enableInputCoroutine = StartCoroutine(EnableInputAfterMouseRelease());
        }
    }

    private IEnumerator EnableInputAfterMouseRelease()
    {
        yield return new WaitUntil(() => Mouse.current == null || !Mouse.current.leftButton.isPressed);
        yield return null;

        if (blockedInputModule != null)
        {
            blockedInputModule.enabled = true;
            blockedInputModule = null;
        }

        if (blockedEventSystem != null)
        {
            blockedEventSystem.enabled = true;
            blockedEventSystem = null;
        }

        IsConclaveTransitionInProgress = false;
        enableInputCoroutine = null;
    }

    private static bool IsVisibleInCamera(Camera camera, SpriteRenderer spriteRenderer)
    {
        if (camera == null || spriteRenderer == null)
        {
            return false;
        }

        Bounds bounds = spriteRenderer.bounds;
        Vector3 min = camera.WorldToViewportPoint(bounds.min);
        Vector3 max = camera.WorldToViewportPoint(bounds.max);

        return max.z > 0f &&
            max.x >= 0f && min.x <= 1f &&
            max.y >= 0f && min.y <= 1f;
    }

    private void MoveCardinalToPoint(Cardinal c, Vector3 pos, Transform parent = null)
    {
        StateController sc = c.GetComponent<StateController>();
        if (sc == null) return;

        GameObject tempObj = new GameObject($"Target_{c.name}");
        tempObj.transform.position = pos;
        if (parent != null) tempObj.transform.SetParent(parent);
        else Destroy(tempObj, 2f);

        sc.ConClaving = true;
        sc.MoveToWaypoints(new Transform[] { tempObj.transform });
    }

    // ========================================================================
    // 기존 카디널 생성 함수
    // ========================================================================
    void SpawnCardinal(GameObject prefab, Transform spawnPoint, string objName)
    {
        GameObject cardinalObj = Instantiate(prefab, spawnPoint.position, Quaternion.identity, GetOrCreateCardinalsContainer());
        cardinalObj.name = objName;

        Cardinal cardinal = cardinalObj.GetComponent<Cardinal>();
        cardinals.Add(cardinal);
    }


    private void SpawnUnitOnPath(ConclavePathData pathData, string aiName)
    {
        if (pathData.spawnPoint == null) return;

        GameObject cardinalObj = SpawnCardinalReturn(cardinalPrefabAI, pathData.spawnPoint, aiName);
        StateController sc = cardinalObj.GetComponent<StateController>();

        if (sc != null)
        {
            if (pathData.waypoints != null)
            {
                sc.MoveToWaypoints(pathData.waypoints);
            }
        }
    }


    GameObject SpawnCardinalReturn(GameObject prefab, Transform spawnPoint, string objName)
    {
        GameObject cardinalObj = Instantiate(prefab, spawnPoint.position, Quaternion.identity, GetOrCreateCardinalsContainer());
        cardinalObj.name = objName;

        Cardinal cardinal = cardinalObj.GetComponent<Cardinal>();
        if (cardinal != null)
        {
            cardinals.Add(cardinal);
        }

        return cardinalObj;
    }

    // ========================================================================
    // Scheme & Utils
    // ========================================================================
    private void AssignRandomSchemers()
    {
        var candidates = cardinals.Where(c => c != null &&
            c.gameObject.activeSelf &&
            !c.CompareTag("Player")).ToList();
        var selectedSchemers = candidates.OrderBy(x => Random.value).Take(2).ToList();
        int assignedCount = 0;

        foreach (var c in selectedSchemers)
        {
            StateController sc = c.GetComponent<StateController>();
            if (sc != null)
            {
                sc.SetSchemerMode(true);
                assignedCount++;
            }
        }

        schemersAssignedThisConclave = assignedCount > 0;
    }

    public void EnsureSchemerAssignmentAfterLoad()
    {
        if (InGameManager.Instance != null && InGameManager.Instance.IsInitialTutorialLocked)
        {
            schemersAssignedThisConclave = false;
            return;
        }

        schemersAssignedThisConclave = cardinals.Any(cardinal => cardinal != null &&
            cardinal.gameObject.activeSelf &&
            cardinal.GetComponent<StateController>()?.IsSchemer == true);

        if (!schemersAssignedThisConclave)
        {
            AssignRandomSchemers();
        }
    }

    public void SetInitialTutorialSchemeLock(bool locked)
    {
        if (locked)
        {
            foreach (Cardinal cardinal in cardinals)
            {
                StateController state = cardinal != null ? cardinal.GetComponent<StateController>() : null;
                if (state != null && state.IsSchemer) state.SetSchemerMode(false);
            }

            schemersAssignedThisConclave = false;
            return;
        }

        if (IsConclaveTransitionInProgress || schemersAssignedThisConclave) return;
        if (cardinals.Any(cardinal => cardinal != null &&
            cardinal.gameObject.activeSelf &&
            cardinal.GetComponent<StateController>()?.IsSchemer == true))
        {
            schemersAssignedThisConclave = true;
            return;
        }

        AssignRandomSchemers();
    }

    public int GetCurrentChatMasterCount()
    {
        int count = 0;
        foreach (var c in cardinals)
        {
            if (c == null || !c.gameObject.activeSelf) continue;
            StateController sc = c.GetComponent<StateController>();
            if (sc != null && sc.CurrentState == CardinalState.ChatMaster) count++;
        }
        return count;
    }

    public float GetCardinalHpSum()
    {
        float result = 0;
        foreach (var c in cardinals) if (c.gameObject.activeSelf) result += c.Hp;
        return result;
    }

    public float GetCardinalPolSum()
    {
        float result = 0;
        foreach (var c in cardinals) if (c.gameObject.activeSelf) result += c.Influence;

        return result;
    }
    public float GetCardinalPietySum()
    {
        float result = 0;

        foreach(var cardinal in cardinals)
        {
            result += cardinal.Piety;
        }

        return result;
    }

    public void DrainAllCardinalHp(float delta)
    {
        foreach (var c in cardinals) if (c.gameObject.activeSelf) c.ChangeHp(delta * c.HpDrainMultiplier);
    }

    public List<CardinalSaveData> CaptureSaveData()
    {
        List<CardinalSaveData> saveData = new List<CardinalSaveData>();

        for (int i = 0; i < cardinals.Count; i++)
        {
            Cardinal cardinal = cardinals[i];
            if (cardinal == null)
            {
                continue;
            }

            saveData.Add(cardinal.CaptureSaveData(i));
        }

        return saveData;
    }

    public void RestoreFromSave(List<CardinalSaveData> saveDataList)
    {
        if (saveDataList == null)
        {
            return;
        }

        Dictionary<int, CardinalSaveData> saveLookup = new Dictionary<int, CardinalSaveData>();
        foreach (var saveData in saveDataList)
        {
            if (saveData == null)
            {
                continue;
            }

            saveLookup[saveData.index] = saveData;
        }

        for (int i = 0; i < cardinals.Count; i++)
        {
            Cardinal cardinal = cardinals[i];
            if (cardinal == null || !saveLookup.TryGetValue(i, out CardinalSaveData saveData))
            {
                continue;
            }

            RestoreCardinal(cardinal, saveData);
        }

        SetConclaveTransition(false);
    }

    private void RestoreCardinal(Cardinal cardinal, CardinalSaveData saveData)
    {
        StateController stateController = cardinal.GetComponent<StateController>();
        if (stateController != null)
        {
            stateController.PrepareForLoadedState();
        }

        cardinal.ApplySaveData(saveData);
        RestoreCardinalTransform(cardinal, saveData.position.ToVector3(), saveData.rotationZ);

        cardinal.gameObject.SetActive(saveData.isActive);

        if (!saveData.isActive)
        {
            return;
        }

        if (stateController != null)
        {
            CardinalState restoredState = (CardinalState)Mathf.Clamp(
                saveData.state,
                0,
                System.Enum.GetValues(typeof(CardinalState)).Length - 1);

            stateController.ApplyLoadedState(restoredState, saveData.isSchemer, saveData.isConClaving);
        }

        cardinal.RestorePlayerIndicatorAfterLoad();
    }

    private void RestoreCardinalTransform(Cardinal cardinal, Vector3 position, float rotationZ)
    {
        NavMeshAgent agent = cardinal.GetComponent<NavMeshAgent>();
        bool restoreAgent = agent != null && agent.enabled;

        if (restoreAgent)
        {
            agent.enabled = false;
        }

        cardinal.transform.position = position;
        cardinal.transform.rotation = Quaternion.Euler(0f, 0f, rotationZ);

        if (restoreAgent)
        {
            agent.enabled = true;

            if (agent.isOnNavMesh)
            {
                agent.ResetPath();
                agent.velocity = Vector3.zero;
            }
        }
    }
}
