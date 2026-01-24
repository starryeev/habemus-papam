using System.Collections.Generic;
using System.Collections;
using System.Linq;
using UnityEngine;

// 목적지를 담은 구조체
[System.Serializable]
public class ConclavePathData
{
    public string groupName;           
    public Transform spawnPoint;       // 시작 위치
    public Transform[] waypoints;      // 경유지 목록 (순서대로 이동)
}

public class CardinalManager : MonoBehaviour
{
    [Header("추기경 프리팹 설정")]
    [Tooltip("플레이어가 조종하는 추기경 프리팹")]
    [SerializeField] private GameObject cardinalPrefabPlayer;

    [Tooltip("AI가 조종하는 추기경 프리팹")]
    [SerializeField] private GameObject cardinalPrefabAI;

    [Header("추기경 설정")]
    [Tooltip("추기경 스폰 포인트 설정")]
    [SerializeField] private Transform[] spawnPoints;

    [Header("콘클라베 설정")]
    [Tooltip("AI 스폰 및 이동 경로 데이터")]
    [SerializeField] private List<ConclavePathData> conclavePaths;

    [Header("퇴장 줄세우기 좌표 설정 (Line Up)")]
    [SerializeField] private Transform leftLineStart;  // 왼쪽 줄 시작점 (a)
    [SerializeField] private Transform leftLineEnd;    // 왼쪽 줄 끝점 (b)
    [SerializeField] private Transform rightLineStart; // 오른쪽 줄 시작점 (a)
    [SerializeField] private Transform rightLineEnd;   // 오른쪽 줄 끝점 (b)

    [Header("퇴장 위치 설정 (Exit Points)")]
    [SerializeField] private Transform leftExitPoint;   // 왼쪽 첫번째가 이동할 곳
    [SerializeField] private Transform rightExitPoint;  // 오른쪽 첫번째가 이동할 곳

    // 내부 관리용 리스트
    private List<Cardinal> leftGroupList = new List<Cardinal>();    // 왼쪽 카디널
    private List<Cardinal> rightGroupList = new List<Cardinal>();   // 오른쪽 카디널
    private List<Vector3> leftLinePositions = new List<Vector3>();  // 왼쪽 줄 좌표들 (고정)
    private List<Vector3> rightLinePositions = new List<Vector3>(); // 오른쪽 줄 좌표들 (고정)

    // 싱글톤
    public static CardinalManager Instance { get; private set; }

    // Cardinal 관리 리스트
    private List<Cardinal> cardinals;

   

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        cardinals = new List<Cardinal>();
    }

    void Start()
    {

    }

    // 하이어라키 정렬
    Transform GetOrCreateCardinalsContainer()
    {
        GameObject runtimeObj = GameObject.Find("Runtime");
        if (runtimeObj == null)
            runtimeObj = new GameObject("Runtime");

        Transform cardinalsTr = runtimeObj.transform.Find("Cardinals");
        if (cardinalsTr == null)
        {
            GameObject cardinalsObj = new GameObject("Cardinals");
            cardinalsTr = cardinalsObj.transform;
            cardinalsTr.SetParent(runtimeObj.transform, false);
        }

        return cardinalsTr;
    }

    //콘클라베 퇴장
    public void StopConClave()
    {
        Time.timeScale = 5f;
        if (cardinals == null || cardinals.Count == 0) return;

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
            if (c == null) continue;

            
            StateController sc = c.GetComponent<StateController>();
            if (sc == null) continue;

            Vector3 targetPos;

            if (i < halfCount)
            {
                // 왼쪽 그룹
                float t = (halfCount > 1) ? (float)i / (halfCount - 1) : 0.5f;
                targetPos = Vector3.Lerp(leftLineStart.position, leftLineEnd.position, t);

                // 데이터 저장
                leftGroupList.Add(c);
                leftLinePositions.Add(targetPos);
            }
            else
            {
                // 오른쪽 그룹
                int rightIndex = i - halfCount;
                int rightTotal = totalCount - halfCount;
                float t = (rightTotal > 1) ? (float)rightIndex / (rightTotal - 1) : 0.5f;
                targetPos = Vector3.Lerp(rightLineStart.position, rightLineEnd.position, t);

                // 데이터 저장
                rightGroupList.Add(c);
                rightLinePositions.Add(targetPos);
            }


            GameObject tempPoint = new GameObject($"InitPos_{i}");
            tempPoint.transform.SetParent(targetParent.transform);
            tempPoint.transform.position = targetPos;

            
            sc.ConClaving = true;           
            c.SetAgentSize(0.1f, 0.1f);     
            sc.MoveToWaypoints(new Transform[] { tempPoint.transform }); 

        }

        Destroy(targetParent, 1f);

        StartCoroutine(ProcessExitSequence());
    }

    //정렬 후 퇴장하는 로직
    private IEnumerator ProcessExitSequence()
    {
        yield return new WaitForSeconds(5.0f);

        while (leftGroupList.Count > 0 || rightGroupList.Count > 0)
        {
            // --- 맨 앞사람 퇴장 시키기 ---

            // 왼쪽 1번 타자 퇴장
            if (leftGroupList.Count > 0)
            {
                Cardinal leaver = leftGroupList[0];
                MoveCardinalToPoint(leaver, leftExitPoint.position);
                leftGroupList.RemoveAt(0);
            }

            // 오른쪽 1번 타자 퇴장
            if (rightGroupList.Count > 0)
            {
                Cardinal leaver = rightGroupList[0];
                MoveCardinalToPoint(leaver, rightExitPoint.position);
                rightGroupList.RemoveAt(0);
            }

            // 잠시 대기
            yield return new WaitForSeconds(0.5f);


            GameObject shiftTargetParent = new GameObject("Temp_ShiftTargets");

            // 왼쪽 줄 당기기
            for (int i = 0; i < leftGroupList.Count; i++)
            {
                MoveCardinalToPoint(leftGroupList[i], leftLinePositions[i], shiftTargetParent.transform);
            }

            // 오른쪽 줄 당기기
            for (int i = 0; i < rightGroupList.Count; i++)
            {
                MoveCardinalToPoint(rightGroupList[i], rightLinePositions[i], shiftTargetParent.transform);
            }

            // 임시 객체 삭제 예약
            Destroy(shiftTargetParent, 1f);


            // 한 쌍이 나가고 다음 쌍이 나갈 때까지의 간격
            yield return new WaitForSeconds(2.0f);
        }

        Time.timeScale = 1f;
    }

    // 헬퍼 함수
    private void MoveCardinalToPoint(Cardinal c, Vector3 pos, Transform parent = null)
    {
        // StateController 확인
        StateController sc = c.GetComponent<StateController>();
        if (sc == null) return;

        GameObject tempObj = new GameObject($"Target_{c.name}");
        tempObj.transform.position = pos;

        if (parent != null) tempObj.transform.SetParent(parent);
        else Destroy(tempObj, 2f);

        
        sc.ConClaving = true;
        sc.MoveToWaypoints(new Transform[] { tempObj.transform });
    }

    //콘클라베 시작 함수 입장 로직 시작
    public void StartConClave()
    {
        StartCoroutine(SpawnAndMoveAISequence(20));
    }

    //콘클라베 시작 코루틴
    private IEnumerator SpawnAndMoveAISequence(int totalCount)
    {
        // 1. 경로 데이터 확보 (Left/Right/Player)
        ConclavePathData leftPath = conclavePaths.Find(p => p.groupName.Contains("Left"));
        ConclavePathData rightPath = conclavePaths.Find(p => p.groupName.Contains("Right"));
        ConclavePathData playerPath = conclavePaths.Find(p => p.groupName.Contains("Player"));

        // 경로 예외처리
        if (leftPath == null && conclavePaths.Count > 0) leftPath = conclavePaths[0];
        if (rightPath == null && conclavePaths.Count > 1) rightPath = conclavePaths[1];
        if (leftPath == null || rightPath == null) { yield break; }

        // --- 1단계: NPC 입장 (5배속) ---
        Time.timeScale = 5f;
        int currentSpawned = 0;

        while (currentSpawned < totalCount)
        {
            if (currentSpawned < totalCount)
            {
                SpawnUnitOnPath(leftPath, $"Cardinal_Left_{currentSpawned + 1}");
                currentSpawned++;
            }
            if (currentSpawned < totalCount)
            {
                SpawnUnitOnPath(rightPath, $"Cardinal_Right_{currentSpawned + 1}");
                currentSpawned++;
            }
            yield return new WaitForSeconds(1.5f);
        }

        yield return new WaitForSeconds(5f); // NPC 정렬 대기

        // --- 2단계: Player 입장 (1배속) ---
        Time.timeScale = 1f;
        Cardinal playerCardinal = null;
        StateController playerSC = null; 

        if (playerPath != null && playerPath.spawnPoint != null)
        {
            GameObject playerObj = SpawnCardinalReturn(cardinalPrefabPlayer, playerPath.spawnPoint, "Cardinal_Player");
            playerCardinal = playerObj.GetComponent<Cardinal>();

            
            if (playerCardinal != null)
            {
                playerSC = playerCardinal.GetComponent<StateController>();
                if (playerSC != null && playerPath.waypoints != null)
                {
                    playerSC.MoveToWaypoints(playerPath.waypoints);
                }
            }
        }

        // Player 도착 대기
        if (playerSC != null)
        {
            yield return null; // 계산시간 때문에 1프레임 잠시 대기
            yield return new WaitUntil(() => playerSC.IsMoving == false);
        }

        //게임 시작

        foreach (var c in cardinals)
        {
            if (c != null)
            {
                StateController sc = c.GetComponent<StateController>();
                if (sc != null)
                {
                    sc.ConClaving = true;
                    sc.ChangeState(CardinalState.Idle);
                }
            }
        }

        AssignRandomSchemers();

    }

    // ========================================================================
    // Scheme 지정 함수
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

    // ---------------------------------------------------------
    // 추기경 생성 헬퍼 함수
    // ---------------------------------------------------------

    //기존 스폰 함수 
    void SpawnCardinal(GameObject prefab, Transform spawnPoint, string objName)
    {
        GameObject cardinalObj = Instantiate(prefab, spawnPoint.position, Quaternion.identity, GetOrCreateCardinalsContainer());
        cardinalObj.name = objName;

        Cardinal cardinal = cardinalObj.GetComponent<Cardinal>();
        cardinals.Add(cardinal);
    }


    // 시작할때 사용하는 카디널 스폰 함수
    private void SpawnUnitOnPath(ConclavePathData pathData, string aiName)
    {
        if (pathData.spawnPoint == null) return;

        GameObject cardinalObj = SpawnCardinalReturn(cardinalPrefabAI, pathData.spawnPoint, aiName);

        // StateController를 가져와서 이동 명령
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

    // 현재 챗마스터 현황 파악
    public int GetCurrentChatMasterCount()
    {
        int count = 0;
        if (cardinals == null) return 0;

        foreach (var card in cardinals)
        {
            if (card == null) continue;

            StateController sc = card.GetComponent<StateController>();
            if (sc != null && sc.CurrentState == CardinalState.ChatMaster)
            {
                count++;
            }
        }
        return count;
    }

    // 기타 추기경 함수 (데이터 관련이므로 Cardinal 접근 유지)

    public float GetCardinalHpSum()
    {
        float result = 0;

        foreach (var cardinal in cardinals)
        {
            result += cardinal.Hp;
        }

        return result;
    }

    public float GetCardinalPietySum()
    {
        float result = 0;

        foreach (var cardinal in cardinals)
        {
            result += cardinal.Piety;
        }

        return result;
    }

    public void DrainAllCardinalHp(float delta)
    {
        foreach(var cardinal in cardinals)
        {
            cardinal.ChangeHp(delta);
        }
    }

}
