using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.AI;

public enum CardinalState
{
    Idle,
    Praying,
    InSpeech,
    ChatMaster,
    Chatting,
    Scheme, // Plot..? 일단 기획서에는 Scheme 라 써있어서 남겨둠
    SchemeChatting,
    CutScene
}

public class StateController : MonoBehaviour
{
    [Header("상태 정보")]
    [SerializeField] private CardinalState currentState = CardinalState.CutScene;

    [Header("Chat 설정")]
    [SerializeField] private GameObject chatPrefab; // 말풍선(채팅) 프리팹
    [SerializeField] private float chatDuration = 5.0f; // 채팅 상태 유지 시간

    // 컴포넌트 참조
    private Cardinal cardinal;
    private NavMeshAgent agent;
    private ICardinalController inputController; // 입력 처리를 위해 가져옴

    private Animation_Controller animController;

    // 이동 경로 큐 (컷씬용)
    private Queue<Vector3> waypoints = new Queue<Vector3>();

    // 코루틴
    private Coroutine pathCoroutine;
    private Coroutine aiWanderCoroutine;
    private Coroutine chatSequenceCoroutine; // [변경] 채팅 시퀀스 통합 관리

    // 프로퍼티
    public bool IsMoving => pathCoroutine != null;
    public CardinalState CurrentState => currentState;
    public bool ConClaving { get; set; } = false;

    void Awake()
    {
        cardinal = GetComponent<Cardinal>();
        agent = GetComponent<NavMeshAgent>();
        inputController = GetComponent<ICardinalController>();

        animController = GetComponentInChildren<Animation_Controller>();
    }

    void Start()
    {
        ChangeState(CardinalState.CutScene);
    }

    void Update()
    {
        switch (currentState)
        {
            case CardinalState.Idle:
                HandleIdleState();
                break;

            case CardinalState.CutScene:
                HandleCutSceneState();
                break;
            case CardinalState.ChatMaster:
                // ChatMaster 상태일 때 필요한 로직 (예: 애니메이션 등)
                break;
            case CardinalState.Chatting: // 듣는 상태
                // 필요한 경우 대기 로직
                break;

                // 다른 상태들...
        }
    }

    // ---------------------------------------------------------
    // 상태별 로직
    // ---------------------------------------------------------

    void HandleIdleState()
    {
        // Player 태그일 때: 직접 조작 
        if (CompareTag("Player"))
        {
            HandlePlayerInput();
        }
        else
        {
            // AI일 때: 배회 코루틴이 돌고 있지 않다면 시작 
            if (aiWanderCoroutine == null)
            {
                aiWanderCoroutine = StartCoroutine(AIWanderRoutine());
            }
        }
    }

    void HandleCutSceneState()
    {
        // 컷씬 중 필요한 로직 작성.. 아직 작성하지 않아 남겨둠
    }

    // ---------------------------------------------------------
    // 플레이어 입력 처리 (이동속도는 Cardinal.cs 참조)
    // ---------------------------------------------------------
    void HandlePlayerInput()
    {
        if (inputController == null || agent == null) return;

        CardinalInputData input = inputController.GetInput();

        // 1순위 키보드 이동 
        if (input.moveDirection != Vector2.zero)
        {
            MoveByKeyboard(input.moveDirection);
        }
        // 2순위 마우스 이동
        else if (input.targetPos.HasValue)
        {
            MoveToTargetPos(input.targetPos.Value);
        }
        else
        {
            // 입력이 없고 경로도 없다면 정지
            if (!agent.hasPath && agent.velocity.sqrMagnitude > 0.01f)
            {
                agent.velocity = Vector3.zero;
            }
        }
    }

    // 키보드 이동 실행
    private void MoveByKeyboard(Vector2 direction)
    {
        if (agent.hasPath) agent.ResetPath();

        // Cardinal의 MoveSpeed를 참조하여 이동
        agent.velocity = new Vector3(direction.x, direction.y, 0) * cardinal.MoveSpeed;
    }

    // 마우스/타겟 이동 실행
    private void MoveToTargetPos(Vector2 targetPos)
    {
        Vector3 destination = new Vector3(targetPos.x, targetPos.y, transform.position.z);
        if (agent.isOnNavMesh) agent.SetDestination(destination);
    }

    // ---------------------------------------------------------
    // [이동 로직] AI 배회 (Idle)
    // ---------------------------------------------------------
    private IEnumerator AIWanderRoutine()
    {
        while (currentState == CardinalState.Idle)
        {
            float waitTime = Random.Range(1f, 3f);
            yield return new WaitForSeconds(waitTime);

            if (currentState != CardinalState.Idle) yield break;

            // 1. 10% 확률로 ChatMaster 상태 전환
            if (Random.Range(0f, 100f) < 10f)
            {
                ChangeState(CardinalState.ChatMaster);
                yield break;
            }

            // 2. 이동 로직 (기존과 동일)
            Vector3 randomOffset = new Vector3(Random.Range(-2f, 2f), Random.Range(-2f, 2f), 0);
            Vector3 randomDest = transform.position + randomOffset;
            NavMeshHit hit;
            if (NavMesh.SamplePosition(randomDest, out hit, 1.0f, NavMesh.AllAreas))
            {
                if (agent.isOnNavMesh) agent.SetDestination(hit.position);
            }

            yield return new WaitUntil(() =>
                currentState != CardinalState.Idle ||
                (!agent.pathPending && agent.remainingDistance <= agent.stoppingDistance && agent.velocity.sqrMagnitude <= 0.1f)
            );
        }
    }

    // ---------------------------------------------------------
    // [이동 로직] 컷씬 강제 이동 
    // ---------------------------------------------------------
    public void MoveToWaypoints(Transform[] pathNodes)
    {
        ChangeState(CardinalState.CutScene);

        waypoints.Clear();
        foreach (Transform t in pathNodes)
        {
            waypoints.Enqueue(t.position);
        }

        if (pathCoroutine != null) StopCoroutine(pathCoroutine);
        pathCoroutine = StartCoroutine(ProcessMoveQueue());
    }

    private IEnumerator ProcessMoveQueue()
    {
        while (waypoints.Count > 0)
        {
            Vector3 nextPos = waypoints.Dequeue();

            // 좌표 오차 적용 -> 무작위를 위한 로직 입장때만 실행 됨
            if (waypoints.Count == 0 && ConClaving == false)
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

            if (agent.isOnNavMesh) agent.SetDestination(nextPos);
            agent.avoidancePriority = 1;

            yield return new WaitUntil(() =>
                !agent.pathPending &&
                agent.remainingDistance <= agent.stoppingDistance &&
                (!agent.hasPath || agent.velocity.sqrMagnitude == 0f)
            );
        }

        if (CompareTag("Player")) agent.avoidancePriority = 10;
        else agent.avoidancePriority = 50;

        pathCoroutine = null;
    }

    // ---------------------------------------------------------
    // 상태 변경 관리
    // ---------------------------------------------------------
    public void ChangeState(CardinalState newState)
    {
        if (currentState == newState) return;
        ExitState(currentState);
        currentState = newState;
        EnterState(currentState);
    }

    private void EnterState(CardinalState state)
    {
        switch (state)
        {
            case CardinalState.Idle:
                if (!CompareTag("Player") && aiWanderCoroutine == null)
                    aiWanderCoroutine = StartCoroutine(AIWanderRoutine());
                break;

            case CardinalState.ChatMaster:
                // [변경] ChatMaster 진입 시 시퀀스 코루틴 시작
                if (chatSequenceCoroutine != null) StopCoroutine(chatSequenceCoroutine);
                chatSequenceCoroutine = StartCoroutine(ProcessChatSequence());
                break;

            case CardinalState.Chatting:
                if (agent != null && agent.isOnNavMesh)
                {
                    agent.ResetPath();
                    agent.velocity = Vector3.zero;
                    agent.isStopped = true; // 이동 정지
                }
                break;

            case CardinalState.CutScene:
                if (agent != null && agent.hasPath) agent.ResetPath();
                break;
        }
    }

    private void ExitState(CardinalState state)
    {
        switch (state)
        {
            case CardinalState.Idle:
                if (aiWanderCoroutine != null)
                {
                    StopCoroutine(aiWanderCoroutine);
                    aiWanderCoroutine = null;
                }
                if (agent != null && agent.isOnNavMesh) agent.ResetPath();
                break;

            case CardinalState.ChatMaster:
                // 상태 나갈 때 시퀀스 코루틴 정리
                if (chatSequenceCoroutine != null)
                {
                    StopCoroutine(chatSequenceCoroutine);
                    chatSequenceCoroutine = null;
                }
                break;

            case CardinalState.Chatting:
                if (agent != null && agent.isOnNavMesh)
                {
                    agent.isStopped = false; // 이동 재개 가능
                }
                break;
        }
    }

    // ---------------------------------------------------------
    // [추가됨] ChatMaster 로직 및 충돌 처리 지원
    // ---------------------------------------------------------

    private IEnumerator ProcessChatSequence()
    {
        // 1. 이동 정지
        if (agent.isOnNavMesh) agent.ResetPath();

        // 2. ChatTrigger 프리팹 생성
        GameObject triggerObj = null;
        ChatTrigger triggerScript = null;

        if (chatPrefab != null)
        {
            triggerObj = Instantiate(chatPrefab, transform.position, Quaternion.identity, this.transform);
            triggerScript = triggerObj.GetComponent<ChatTrigger>();
        }

        // 3. Trigger가 NPC들을 감지할 시간을 줌 (0.5초 대기)
        // BoxCollider가 활성화되고 충돌 이벤트를 받아낼 물리 프레임 확보
        yield return new WaitForSeconds(0.5f);

        // 4. 감지된 NPC 중 랜덤 3명 뽑기
        List<StateController> listeners = new List<StateController>();

        if (triggerScript != null && triggerScript.collectedNPCs.Count > 0)
        {
            // 리스트를 랜덤으로 섞음 (System.Linq 사용)
            var shuffled = triggerScript.collectedNPCs.OrderBy(x => Random.value).ToList();

            // 최대 3명, 혹은 그보다 적으면 전체 선택
            int countToPick = Mathf.Min(3, shuffled.Count);

            for (int i = 0; i < countToPick; i++)
            {
                listeners.Add(shuffled[i]);
            }
        }

        // 5. 선택된 NPC들에게 상태 부여 및 방향 설정
        foreach (var listener in listeners)
        {
            // A. 상태 변경 (Chatting 상태로 전환하여 멈춤)
            listener.EnterChatListener();

            // B. ChatMaster(나)를 바라보게 설정
            if (listener.animController != null)
            {
                // 방향 벡터 계산: (내 위치 - 상대 위치)
                Vector2 directionToMaster = (this.transform.position - listener.transform.position).normalized;

                // Animation_Controller에 추가한 함수 호출
                listener.animController.SetLookDirection(directionToMaster);
            }
        }

        // 6. 5초 동안 대기 (채팅 진행 중)
        yield return new WaitForSeconds(chatDuration);

        // 7. 종료 처리: 선택된 리스너들 해제
        foreach (var listener in listeners)
        {
            // 리스너가 여전히 Chatting 상태라면 Idle로 복귀
            if (listener.CurrentState == CardinalState.Chatting)
            {
                listener.ChangeState(CardinalState.Idle);
            }
        }

        // 8. 프리팹(말풍선) 삭제
        if (triggerObj != null)
        {
            Destroy(triggerObj);
        }

        // 9. 나 자신도 Idle로 복귀
        ChangeState(CardinalState.Idle);
    }

    private IEnumerator RevertToIdleAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);

        // [수정] ChatMaster(말하는 애) 뿐만 아니라 Chatting(듣는 애)도 시간이 지나면 풀려야 함
        if (currentState == CardinalState.ChatMaster || currentState == CardinalState.Chatting)
        {
            ChangeState(CardinalState.Idle);
        }
    }

    public void EnterChatListener()
    {
        // 컷씬이나 이미 마스터가 아니라면 변경
        if (currentState != CardinalState.CutScene && currentState != CardinalState.ChatMaster)
        {
            ChangeState(CardinalState.Chatting);

            // 주의: 해제는 Master(말 건 사람)가 5초 뒤에 ChangeState(Idle)을 호출해줌으로써 이루어짐.
            // 만약 안전장치가 필요하다면 여기에 별도의 타임아웃 코루틴을 둘 수 있음.
        }
    }
}