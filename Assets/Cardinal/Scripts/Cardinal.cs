using System.Collections.Generic;
using System.Collections;
using UnityEngine;
using UnityEngine.AI;

// 상태 정의 
public enum CardinalState
{
    Idle,           // 평상시
    Praying,        // 기도 중
    InSpeech,       // 연설 중
    ChatMaster,     // 대화 생성 중 (주최자)
    Chatting,       // 대화 중 (참여자)
    Scheme,         // 공작 중
    SchemeChatting, // 공작-대화 중
    CutScene        // 콘클라베 시작/종료 연출 시 강제 이동
}

public class Cardinal : MonoBehaviour
{
    [Header("상태 정보")]
    [SerializeField] private CardinalState currentState = CardinalState.CutScene; // 인스펙터 확인용 -> 이 창을 통해 현재 Cardinal.cs 오브젝트가 어떤 상태인지 파악 가능

    [Header("추기경 기본 설정")]
    [Tooltip("추기경 기본 체력")]
    [SerializeField] private int hp;

    [Tooltip("추기경 기본 정치력")]
    [SerializeField] private int influence;

    [Tooltip("추기경 기본 경건함")]
    [SerializeField] private int piety;

    [Header("이동 관련 설정")]
    [SerializeField] private float moveSpeed;

    // 추기경 멤버변수
    private List<Item> items;

    // 기타 멤버변수
    private ICardinalController controller;
    private Rigidbody2D rb;

    private NavMeshAgent agent;

    // 이동 경로 큐
    private Queue<Vector3> waypoints = new Queue<Vector3>();

    //이동 코루틴
    private Coroutine pathCoroutine;

    //배회 코루틴
    private Coroutine aiWanderCoroutine;

    //이동중 확인하는 변수
    public bool IsMoving => pathCoroutine != null;

    //콘클라베 입장인지 퇴장인지 구별하는 변수
    public bool ConClaving = false;

    // 추기경 기본 프로퍼티 설정
    public int Hp => hp;
    public int Influence => influence;
    public int Piety => piety;
    
    //(추가)
    public CardinalState CurrentState => currentState; // 외부에서 상태 확인 가능

    void Awake()
    {
        // 멤버변수 초기화
        items = new List<Item>();
        rb = GetComponentInChildren<Rigidbody2D>();    
        controller = GetComponent<ICardinalController>();
        agent = GetComponent<NavMeshAgent>();
        
        if(agent != null)
        {
            // 회전 방지
            agent.updateRotation = false;
            agent.updateUpAxis = false;

            //속도 초기화
            agent.speed = moveSpeed;
        }
    }

    void Start()
    {
        
        InitCardinal();
        ChangeState(CardinalState.CutScene); //오브젝트가 생성 되면 초기 상태를 무조건 컷씬으로 변경
    }

    void Update()
    {
        //상태에 따른 행동 정의
        switch (currentState)
        {
            case CardinalState.Idle:
                HandleIdleState();
                break;

            case CardinalState.CutScene:
                HandleCutSceneState();
                break;

            case CardinalState.Praying:
                // 기도 중 로직 (구현 예정)
                break;

            case CardinalState.InSpeech:
                // 연설 중 로직 (구현 예정)
                break;

            case CardinalState.ChatMaster:
            case CardinalState.Chatting:
                // 대화 관련 로직 (구현 예정)
                break;

            case CardinalState.Scheme:
            case CardinalState.SchemeChatting:
                // 공작 관련 로직 (구현 예정)
                break;
        }

    }

    //NavMesh 의 radius 와 height 수정
    public void SetAgentSize(float newRadius, float newHeight)
    {
        if (agent != null)
        {
            agent.radius = newRadius;
            agent.height = newHeight;
        }
    }

    // ---------------------------------------------------------
    // 상태 변경 및 관리 메서드 이를 통해 현재 NPC, Player 가 무엇을 하는지 CardinalManager.cs 에서 할당할 수 있습니다.
    // ---------------------------------------------------------
    public void ChangeState(CardinalState newState)
    {
        if (currentState == newState) return;

        ExitState(currentState); // 이전 상태 종료 처리
        currentState = newState;
        EnterState(currentState); // 새로운 상태 진입 처리
    }
    private void EnterState(CardinalState state)   
    {
        switch (state)
        {
            case CardinalState.Idle:
                break;
            case CardinalState.CutScene:
                if (agent.hasPath) agent.ResetPath(); // 컷씬 상태로 Enter 시 기존 이동 경로 모두 취소
                break;

                // 나머지 상태 진입 로직...(구현예정)
        }
    }

    private void ExitState(CardinalState state)    
    {
        switch (state)
        {
            case CardinalState.Idle:
                // (추가) Idle 상태를 빠져나갈 때 배회 코루틴 정지
                if (aiWanderCoroutine != null)
                {
                    StopCoroutine(aiWanderCoroutine);
                    aiWanderCoroutine = null;
                }

                // 이동 중이었다면 멈춤
                if (agent.enabled && agent.isOnNavMesh)
                {
                    agent.ResetPath();
                    agent.velocity = Vector3.zero;
                }
                break;

            case CardinalState.CutScene:
                // 컷씬 종료 시 로직
                break;

                // ... 나머지
        }
    }

    // ---------------------------------------------------------
    // 입장 로직(강제 이동)
    // ---------------------------------------------------------

    public void MoveToWaypoints(Transform[] pathNodes)
    {
        // 1. 상태를 CutScene으로 변경 (플레이어/AI 조작 차단)
        ChangeState(CardinalState.CutScene);

        // 2. 큐 초기화 및 경로 등록
        waypoints.Clear();
        foreach (Transform t in pathNodes)
        {
            waypoints.Enqueue(t.position);
        }

        // 3. 기존 이동 코루틴이 있다면 중지하고 새로 시작
        if (pathCoroutine != null) StopCoroutine(pathCoroutine);
        pathCoroutine = StartCoroutine(ProcessMoveQueue());
    }

    private IEnumerator ProcessMoveQueue()
    {
        // 큐에 남은 목적지가 없을 때까지 반복
        while (waypoints.Count > 0)
        {
            // 다음 목적지 꺼내기
            Vector3 nextPos = waypoints.Dequeue();

            // --- 좌표 오차 적용 로직(입장일때만 마지막 목적지 무작위 배치) ---
            if (waypoints.Count == 0 && ConClaving == false )
            {
                if (CompareTag("Player"))
                {
                    nextPos.y -= 1f;
                }
                else
                {
                    float randomX = Random.Range(-1.5f, 1.5f);
                    float randomY = Random.Range(-4f, 7f);
                    nextPos.x += randomX;
                    nextPos.y += randomY;
                }
            }
            

            agent.SetDestination(nextPos);
            agent.avoidancePriority = 1; // 이동 중 우선순위 높임

            yield return new WaitUntil(() =>
                !agent.pathPending &&
                agent.remainingDistance <= agent.stoppingDistance &&
                (!agent.hasPath || agent.velocity.sqrMagnitude == 0f)
            );
        }

        // 이동 완료 후 우선순위 복구
        if (CompareTag("Player")) agent.avoidancePriority = 10;
        else agent.avoidancePriority = 50;

        pathCoroutine = null;
    }

    // [컷씬/강제이동] - 입력 불가, 정해진 웨이포인트만 
    void HandleCutSceneState()
    {
        //현재 공란 나중에 필요한 기능이 생긴다면 채워넣을 예정
    }

    // ---------------------------------------------------------
    // 평상시 조작 함수
    // ---------------------------------------------------------
    void HandleIdleState()
    {
        // Player 태그일 때만 직접 조작 입력을 받음
        if (CompareTag("Player"))
        {
            HandlePlayerInput();
        }
        else
        {
            // AI의 평상시 로직 (랜덤 배회)
            // 배회 코루틴이 돌고 있지 않다면 시작
            if (aiWanderCoroutine == null)
            {
                aiWanderCoroutine = StartCoroutine(AIWanderRoutine());
            }
        }
    }

    // ---------------------------------------------------------
    // Idle 상태 로직(배회)
    // ---------------------------------------------------------


    private IEnumerator AIWanderRoutine()
    {
        // Idle 상태인 동안 무한 반복
        while (currentState == CardinalState.Idle)
        {
            // 1. 1~3초 대기
            float waitTime = Random.Range(1f, 3f);
            yield return new WaitForSeconds(waitTime);

            // 상태가 바뀌었는지 중간 체크
            if (currentState != CardinalState.Idle) yield break;

            // 2. 현재 위치 기준 랜덤 오프셋 설정 (-2 ~ 2)
            Vector3 randomOffset = new Vector3(
                Random.Range(-2f, 2f),
                Random.Range(-2f, 2f),
                0 
            );

            Vector3 randomDest = transform.position + randomOffset;

            // 3. NavMesh 유효 좌표 찾기
            // randomDest 근처 1.0f 반경 내에서 가장 가까운 NavMesh 위의 점을 찾음
            NavMeshHit hit;
            if (NavMesh.SamplePosition(randomDest, out hit, 1.0f, NavMesh.AllAreas))
            {
                agent.SetDestination(hit.position);
            }
            else
            {
                // 유효하지 않은 위치라면 이번 턴은 넘기고 다시 대기
                continue;
            }

            // 4. 도착할 때까지 대기
            yield return new WaitUntil(() =>
                currentState != CardinalState.Idle || // 상태가 바뀌면 즉시 탈출
                (!agent.pathPending &&
                 agent.remainingDistance <= agent.stoppingDistance &&
                 agent.velocity.sqrMagnitude <= 0.1f)
            );
        }
    }

    //Idle 상태 시 입력받아 이동하는 함수
    void HandlePlayerInput()
    {
        // 입력을 받아오는 부분은 기존과 동일하지만, Idle 상태에서만 호출됨
        if (controller == null) return;

        CardinalInputData input = controller.GetInput();

        if (input.moveDirection != Vector2.zero)
        {
            MoveByKeyboard(input.moveDirection);
        }
        else if (input.targetPos.HasValue)
        {
            MoveToTargetPos(input.targetPos.Value);
        }
        else
        {
            if (!agent.hasPath && agent.velocity.sqrMagnitude > 0.01f)
            {
                agent.velocity = Vector3.zero;
            }
        }
    }

    
    void InitCardinal()
    {
        GameBalance balance = InGameManager.Instance.Balance;

        hp = balance.InitialHp;
        influence = balance.InitialInfluence;
        piety = balance.InitialPiety;
        moveSpeed = balance.InitialMoveSpeed;
    }

    //네브메시 이동 함수
    void MoveToTargetPos(Vector2 targetPos)
    {
        //기존 마우스 클릭함수를 네브메시로 대체

        // 클릭 위치를 넘겨받아 클릭 위치로 Player 이동
        Vector3 destination = new Vector3(targetPos.x , targetPos.y, transform.position.z);
        agent.SetDestination(destination);
    }

    void MoveByKeyboard(Vector2 direction)
    {
        // 마우스 이동 경로 초기화
        if (agent.hasPath)
        {
            agent.ResetPath();
        }

        //키보드 입력
        agent.velocity = new Vector3(direction.x, direction.y, 0) * moveSpeed;
    }

    public void ChangeHp(int delta)
    {
        hp = Mathf.Clamp(hp + delta, 0, 100);
    }

    public void ChangeInfluence(int delta)
    {
        influence = Mathf.Clamp(influence + delta, 0, 100);
    }

    public void ChangePiety(int delta)
    {
        influence = Mathf.Clamp(piety + delta, 0, 100);
    }

    // 기도 함수
    public void Pray()
    {
        GameBalance balance = InGameManager.Instance.Balance;

        // 아이템 이벤트 로직
        foreach(var item in items)
        {
            item?.OnPray(this);
        }

        if(Random.value < balance.PraySuccessChance)
        {
            // 기도 성공
            ChangePiety(balance.PraySuccessDeltaPiety);
            ChangeHp(balance.PraySuccessDeltaHp);
        }
        else
        {
            // 기도 실패
            ChangePiety(balance.PrayFailDeltaPiety);
            ChangeHp(balance.PrayFailDeltaHp);
        }
    }

    // 연설 함수
    public void Speech()
    {
        GameBalance balance = InGameManager.Instance.Balance;

        // 아이템 이벤트 로직
        foreach(var item in items)
        {
            item?.OnSpeech(this);
        }

        if(Random.value < balance.SpeechSuccessChance)
        {
            // 연설 성공
            int speechSuccessDeltaInfluence = Random.Range(balance.SpeechSuccessDeltaInfluenceMin, balance.SpeechSuccessDeltaInfluenceMax + 1);
            ChangeInfluence(speechSuccessDeltaInfluence);
            ChangeHp(balance.SpeechSuccessDeltaHp);
        }
        else
        {
            // 연설 실패
            ChangeInfluence(balance.SpeechFailDeltaInfluence);
            ChangeHp(balance.SpeechFailDeltaHp);
        }
    }

    // 공작 함수
    public void Plot() {}
}
