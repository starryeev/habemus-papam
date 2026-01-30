using System.Collections.Generic;
using System.Collections;
using System.Linq;
using UnityEngine;
using UnityEngine.AI;

[System.Serializable]
public class ConclavePathData
{
    public string groupName;
    public Transform spawnPoint;       // 시작 위치
    public Transform[] waypoints;      // 경유지 목록
}

public class CardinalManager : MonoBehaviour
{
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

    private List<Cardinal> leftGroupList = new List<Cardinal>();
    private List<Cardinal> rightGroupList = new List<Cardinal>();
    private List<Vector3> leftLinePositions = new List<Vector3>();
    private List<Vector3> rightLinePositions = new List<Vector3>();

    [SerializeField] private StatsUI statsUI;

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
        StartCoroutine(ResetAndEnterSequence());
    }

    private IEnumerator ResetAndEnterSequence()
    {
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

        Cardinal player = cardinals.Find(c => c.CompareTag("Player"));
        StateController playerSC = null;

        if (player != null && playerPath != null)
        {
            ResetCardinalState(player, playerPath.spawnPoint.position);
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
        }

        Debug.Log("모든 입장 완료. 콘클라베 시작.");

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
        AssignRandomSchemers();

        if (statsUI != null)
        {
            statsUI.Initialize(cardinals);
        }
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
        Time.timeScale = 5f;
        if (cardinals == null || cardinals.Count == 0) return;

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

        Debug.Log("All cardinals have exited.");
        Time.timeScale = 1f;
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

        StartCoroutine(DeactivateAfterExit(c, sc));
    }

    private IEnumerator DeactivateAfterExit(Cardinal c, StateController sc)
    {
        yield return new WaitUntil(() => !sc.IsMoving);

        c.gameObject.SetActive(false);
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
        var candidates = cardinals.Where(c => c != null && !c.CompareTag("Player")).ToList();
        var selectedSchemers = candidates.OrderBy(x => Random.value).Take(2).ToList();

        foreach (var c in selectedSchemers)
        {
            StateController sc = c.GetComponent<StateController>();
            if (sc != null)
            {
                sc.SetSchemerMode(true);
                Debug.Log($"NPC {c.name} Scheme 상태 적용");
            }
        }
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

    public void DrainAllCardinalHp(float delta)
    {
        foreach (var c in cardinals) if (c.gameObject.activeSelf) c.ChangeHp(delta);
    }
}