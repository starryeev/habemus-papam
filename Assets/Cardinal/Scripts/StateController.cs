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
    [Tooltip("현재 캐릭터가 수행 중인 상태")]
    [SerializeField] private CardinalState currentState = CardinalState.CutScene;

    [Header("Chat 설정")]
    [Tooltip("채팅 시작을 알리고 NPC를 모으는 트리거(말풍선) 프리팹")]
    [SerializeField] private GameObject chatTriggerPrefab;

    [Tooltip("채팅 상태가 유지되는 시간 (초)")]
    [SerializeField] private float chatDuration = 5.0f;

    [Tooltip("Idle 상태에서 ChatMaster(말하기) 상태로 전환될 확률 (%)")]
    [SerializeField] private float ChatMaster = 5f;

    [Header("Chat Visual 설정 (말풍선)")]
    [Tooltip("말하는 사람(Master) 머리 위에 표시될 말풍선 프리팹")]
    [SerializeField] private GameObject masterBubblePrefab;

    [Tooltip("듣는 사람(Listener) 머리 위에 표시될 말풍선 프리팹")]
    [SerializeField] private GameObject listenerBubblePrefab;

    [Tooltip("플레이어 감지 시 표시될 언짢은 이모티콘 프리팹")]
    [SerializeField] private GameObject masterAlertBubblePrefab;

    [Tooltip("캐릭터 위치를 기준으로 말풍선이 생성될 오프셋 (높이 조절)")]
    [SerializeField] private Vector3 bubbleOffset = new Vector3(0, 2.5f, 0);

    // 컴포넌트 참조
    private Cardinal cardinal;                  
    private NavMeshAgent agent;
    private ICardinalController inputController; 
    private Animation_Controller animController;

    // 말풍선 인스턴스
    private GameObject currentBubbleInstance; 

    // 이동 경로 큐 (컷씬용)
    private Queue<Vector3> waypoints = new Queue<Vector3>();

    // 코루틴
    private Coroutine pathCoroutine;
    private Coroutine aiWanderCoroutine;
    private Coroutine chatSequenceCoroutine;

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
        EnterState(CardinalState.CutScene);
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

            // 1. 일정 확률로 ChatMaster 상태 전환 || ChatMaster 상태 1명 이하일때만 
            if (CardinalManager.Instance != null && CardinalManager.Instance.GetCurrentChatMasterCount() < 2)
            {
                // 설정된 확률(ChatMaster 변수) 체크
                if (Random.Range(0f, 100f) < ChatMaster)
                {
                    ChangeState(CardinalState.ChatMaster);
                    yield break;
                }
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
                if (cardinal != null)
                {
                    cardinal.SetAgentSize(0.1f, 0.1f);
                }

                if (chatSequenceCoroutine != null) StopCoroutine(chatSequenceCoroutine);
                chatSequenceCoroutine = StartCoroutine(ProcessChatSequence());

                ShowBubble(masterBubblePrefab); // 말풍선
                break;

            case CardinalState.Chatting:
                if (cardinal != null)
                {
                    cardinal.SetAgentSize(0.1f, 0.1f);
                }

                if (agent != null && agent.isOnNavMesh)
                {
                    agent.ResetPath();
                    agent.velocity = Vector3.zero;
                    agent.isStopped = true; // 이동 정지
                }

                ShowBubble(listenerBubblePrefab);
                break;

            case CardinalState.CutScene:
                if (agent != null && agent.hasPath) agent.ResetPath();
                if (cardinal != null)
                {
                    cardinal.SetAgentSize(0.1f, 0.1f);
                }
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
                if (cardinal != null)
                {
                    cardinal.SetAgentSize(0.5f, 1f);
                }

                // 상태 나갈 때 시퀀스 코루틴 정리
                if (chatSequenceCoroutine != null)
                {
                    StopCoroutine(chatSequenceCoroutine);
                    chatSequenceCoroutine = null;
                }

                HideBubble();
                break;

            case CardinalState.Chatting:
                if (cardinal != null)
                {
                    cardinal.SetAgentSize(0.5f, 1f);
                }

                if (agent != null && agent.isOnNavMesh)
                {
                    agent.isStopped = false; // 이동 재개 가능
                }

                HideBubble();
                break;
            case CardinalState.CutScene:
                if (cardinal != null)
                {
                    cardinal.SetAgentSize(0.5f, 1f);
                }
                break;
        }
    }

    private void ShowBubble(GameObject prefab)
    {
        HideBubble();

        if (prefab != null)
        {
            currentBubbleInstance = Instantiate(prefab, transform.position + bubbleOffset, Quaternion.identity, transform);
        }
    }

    private void HideBubble()
    {
        if (currentBubbleInstance != null)
        {
            Destroy(currentBubbleInstance);
            currentBubbleInstance = null;
        }
    }

    // ---------------------------------------------------------
    // ChatMaster 로직 및 충돌 처리 지원
    // ---------------------------------------------------------

    private IEnumerator ProcessChatSequence()
    {
        // 1. [Master] 이동 정지
        if (agent.isOnNavMesh) agent.ResetPath();

        // [위치 선정]
        Vector3 spawnPos = transform.position;
        Animator myAnim = GetComponentInChildren<Animator>();
        if (myAnim != null)
        {
            Vector2 facingDir = new Vector2(myAnim.GetFloat("InputX"), myAnim.GetFloat("InputY"));
            if (facingDir == Vector2.zero) facingDir = Vector2.down;
            spawnPos += (Vector3)facingDir.normalized * 1.5f;
        }

        // 2. ChatTrigger 생성
        GameObject triggerObj = null;
        ChatTrigger triggerScript = null;
        BoxCollider2D triggerCollider = null;

        if (chatTriggerPrefab != null)
        {
            triggerObj = Instantiate(chatTriggerPrefab, spawnPos, Quaternion.identity);
            triggerScript = triggerObj.GetComponent<ChatTrigger>();
            triggerCollider = triggerObj.GetComponent<BoxCollider2D>();
        }

        // [상태 변수]
        bool playerDetected = false;

        // 3. 초기 대기 (NPC 수집)
        yield return new WaitForSeconds(0.5f);

        // 4. 리스너 선별 (플레이어 제외)
        List<StateController> listeners = new List<StateController>();
        if (triggerScript != null && triggerScript.collectedNPCs.Count > 0)
        {
            var candidates = triggerScript.collectedNPCs
                .Where(npc => !npc.CompareTag("Player") && npc.gameObject != this.gameObject)
                .OrderBy(x => Random.value)
                .ToList();

            int countToPick = Mathf.Min(3, candidates.Count);
            for (int i = 0; i < countToPick; i++) listeners.Add(candidates[i]);
        }

        int totalCount = 1 + listeners.Count;

        // [배치 로직]
        float radius = (triggerCollider != null) ? Mathf.Max(triggerCollider.size.x, triggerCollider.size.y) * 0.3f : 0.8f;
        List<StateController> allParticipants = new List<StateController>();
        allParticipants.Add(this);
        allParticipants.AddRange(listeners);

        foreach (var l in listeners) l.EnterChatListener();

        if (listeners.Count > 0)
        {
            float angleStep = 360f / totalCount;
            float currentAngle = 90f;
            foreach (var participant in allParticipants)
            {
                float rad = currentAngle * Mathf.Deg2Rad;
                Vector3 circleOffset = new Vector3(Mathf.Cos(rad), Mathf.Sin(rad), 0) * radius;
                participant.MoveToPosition(triggerObj.transform.position + circleOffset);
                currentAngle += angleStep;
            }
        }

        // 5. 이동 및 자리잡기

        yield return new WaitForSeconds(1.5f); // 이동 대기

        // 방향 보정 (0.5초)
        float orientationTimer = 0f;
        while (orientationTimer < 0.5f)
        {
            foreach (var participant in allParticipants)
            {
                if (participant != null && participant.animController != null)
                {
                    if (participant.agent != null) { participant.agent.isStopped = true; participant.agent.velocity = Vector3.zero; }
                    Vector2 dirToCenter = (triggerObj.transform.position - participant.transform.position).normalized;
                    if (dirToCenter.sqrMagnitude > 0.001f) participant.animController.SetLookDirection(dirToCenter);
                }
            }
            orientationTimer += Time.deltaTime;
            yield return null;
        }

        // -----------------------------------------------------------------------
        // [중요] 대형 완성 -> 다각형 콜라이더 생성!
        // -----------------------------------------------------------------------
        if (triggerScript != null)
        {
            // 참가자들의 Transform 리스트 생성
            List<Transform> participantTransforms = new List<Transform>();
            foreach (var p in allParticipants) participantTransforms.Add(p.transform);

            // ChatTrigger에게 "이 좌표들을 잇는 다각형을 만들어라" 명령
            triggerScript.CreateFormationCollider(participantTransforms);
        }

        // -----------------------------------------------------------------------
        // 6. 대화 진행 (실제 감지 시작)
        // -----------------------------------------------------------------------
        float chatTimer = 0f;
        float totalWaitTime = Mathf.Max(0, chatDuration - 1.5f);

        while (chatTimer < totalWaitTime)
        {
            // 다각형 내부 감지 변수 체크 (IsPlayerInFormation)
            if (!playerDetected && triggerScript != null && triggerScript.IsPlayerInFormation)
            {
                GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
                if (playerObj != null)
                {
                    playerDetected = true; 

                    // 말풍선 교체
                    if (masterAlertBubblePrefab != null) ShowBubble(masterAlertBubblePrefab);

                    // 정치력 감소
                    Cardinal playerCardinal = playerObj.GetComponent<Cardinal>();
                    if (playerCardinal != null)
                    {
                        playerCardinal.ChangeInfluence(-3f);
                    }
                }
            }

            chatTimer += Time.deltaTime;
            yield return null;
        }

        // 7. 종료 처리
        foreach (var listener in listeners)
        {
            if (listener.CurrentState == CardinalState.Chatting) listener.ChangeState(CardinalState.Idle);
        }
        if (triggerObj != null) Destroy(triggerObj);
        ChangeState(CardinalState.Idle);
    }


    public void EnterChatListener()
    {
        if (currentState != CardinalState.CutScene && currentState != CardinalState.ChatMaster)
        {
            ChangeState(CardinalState.Chatting);
        }
    }

    public void MoveToPosition(Vector3 targetPos) // 채팅 상태일때 움직이도록
    {
        if (agent != null && agent.isOnNavMesh)
        {
            agent.isStopped = false; // 정지 해제
            agent.SetDestination(targetPos);
        }
    }
}