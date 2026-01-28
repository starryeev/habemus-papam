using System.Collections.Generic;
using System.Collections;
using UnityEngine;

// 목적지를 담은 구조체
[System.Serializable]
public class ConclavePathData
{
    public string groupName;           // 구분용 이름 (예: "Left Group", "Right Group")
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

    // 기타 멤버변수
    // 데이터 관리는 여전히 Cardinal 클래스를 통해 합니다.
    private List<Cardinal> cardinals;

   

    void Awake()
    {
        // 싱글톤
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        // 멤버변수 초기화
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

        // 1. 리스트 및 데이터 초기화
        leftGroupList.Clear();
        rightGroupList.Clear();
        leftLinePositions.Clear();
        rightLinePositions.Clear();

        int totalCount = cardinals.Count;
        int halfCount = totalCount / 2;

        // 임시 타겟 부모
        GameObject targetParent = new GameObject("Temp_InitialLineUp");

        for (int i = 0; i < totalCount; i++)
        {
            Cardinal c = cardinals[i];
            if (c == null) continue;

            
            StateController sc = c.GetComponent<StateController>();
            if (sc == null) continue;

            Vector3 targetPos;

            // 2. 그룹 분류 및 고정 좌표 계산
            if (i < halfCount)
            {
                // [왼쪽 그룹]
                float t = (halfCount > 1) ? (float)i / (halfCount - 1) : 0.5f;
                targetPos = Vector3.Lerp(leftLineStart.position, leftLineEnd.position, t);

                // 데이터 저장
                leftGroupList.Add(c);
                leftLinePositions.Add(targetPos);
            }
            else
            {
                // [오른쪽 그룹]
                int rightIndex = i - halfCount;
                int rightTotal = totalCount - halfCount;
                float t = (rightTotal > 1) ? (float)rightIndex / (rightTotal - 1) : 0.5f;
                targetPos = Vector3.Lerp(rightLineStart.position, rightLineEnd.position, t);

                // 데이터 저장
                rightGroupList.Add(c);
                rightLinePositions.Add(targetPos);
            }

            // 3. 초기 정렬 이동 명령

            GameObject tempPoint = new GameObject($"InitPos_{i}");
            tempPoint.transform.SetParent(targetParent.transform);
            tempPoint.transform.position = targetPos;

            
            sc.ConClaving = true;           // StateController의 변수
            c.SetAgentSize(0.1f, 0.1f);     // Cardinal의 변수 (Agent 사이즈 조절)
            sc.MoveToWaypoints(new Transform[] { tempPoint.transform }); // StateController의 함수

        }

        Destroy(targetParent, 1f);

        // 4. 정렬 후 순차 퇴장 코루틴 시작
        StartCoroutine(ProcessExitSequence());
    }

    //정렬 후 퇴장하는 로직
    private IEnumerator ProcessExitSequence()
    {
        // 1. 추기경들이 처음 줄을 설 때까지 충분히 대기.. 
        yield return new WaitForSeconds(5.0f);

        // 왼쪽이나 오른쪽 그룹에 사람이 남아있다면 계속 반복
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


            // --- [B] 나머지 인원 한 칸씩 앞으로 당기기 ---
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

        Debug.Log("All cardinals have exited the line.");
        Time.timeScale = 1f;
    }

    // (헬퍼 함수) Vector3 좌표로 이동시키기 위해 임시 오브젝트를 만드는 과정을 단순화
    private void MoveCardinalToPoint(Cardinal c, Vector3 pos, Transform parent = null)
    {
        // [변경점] StateController 확인
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

        // --- 3단계: Player 도착 대기 ---
        if (playerSC != null)
        {
            yield return null; // 계산시간 때문에 1프레임 잠시 대기
            yield return new WaitUntil(() => playerSC.IsMoving == false);
        }

        // --- 4단계: 게임 시작 (모두 Idle 전환) ---

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

        // [변경점] StateController를 가져와서 이동 명령
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
            //리스트 등록
            //리스트 등록 (데이터 접근을 위해 Cardinal 저장)
            cardinals.Add(cardinal);
        }

        return cardinalObj;
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

    public float GetCardinalPolSum()
    {
        float result = 0;

        foreach (var cardinal in cardinals)
        {
            result += cardinal.Influence;
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
