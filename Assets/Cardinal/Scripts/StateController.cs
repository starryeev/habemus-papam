using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.AI;

public enum CardinalState
{
    Idle,
    ReadyPraying,
    Praying,
    ReadyInSpeech,
    InSpeech,
    ChatMaster,
    Chatting,
    Scheme, // Plot..? 일단 기획서에는 Scheme 라 써있어서 남겨둠 SchemeChatiing 도 같이 수행
    CutScene
}

public class StateController : MonoBehaviour
{
    [Header("상태 정보")]
    [Tooltip("현재 캐릭터가 수행 중인 상태")]
    [SerializeField] private CardinalState currentState = CardinalState.CutScene;

    [Header("AI 배회 설정")]
    [Tooltip("이 오브젝트의 Y좌표가 배회 한계선이 됩니다. (이 선 위로는 안 올라감)")]
    [SerializeField] private Transform wanderLimitTransform;

    [Header("Chat 설정")]
    [Tooltip("채팅 시작을 알리고 NPC를 모으는 트리거(말풍선) 프리팹")]
    [SerializeField] private GameObject chatTriggerPrefab;

    [Tooltip("채팅 상태가 유지되는 시간 (초)")]
    [SerializeField] private float chatDuration = 5.0f;

    [Tooltip("Idle 상태에서 ChatMaster(말하기) 상태로 전환될 확률 (%)")]
    [SerializeField] private float ChatMaster = 5f;

    [Header("말풍선 설정 (디버깅용)")]
    [Tooltip("말하는 사람(Master) 머리 위에 표시될 말풍선 프리팹")]
    [SerializeField] private GameObject masterBubblePrefab;

    [Tooltip("듣는 사람(Listener) 머리 위에 표시될 말풍선 프리팹")]
    [SerializeField] private GameObject listenerBubblePrefab;

    [Tooltip("플레이어 감지 시 표시될 언짢은 이모티콘 프리팹")]
    [SerializeField] private GameObject masterAlertBubblePrefab;

    [Tooltip("기도(Praying) 상태일 때 표시될 말풍선 프리팹")]
    [SerializeField] private GameObject prayingBubblePrefab;

    [Tooltip("연설(Speeching) 상태일 때 표시될 말풍선 프리팹")]
    [SerializeField] private GameObject SpeechingBubblePrefab;

    [Tooltip("캐릭터 위치를 기준으로 말풍선이 생성될 오프셋 (높이 조절)")]
    [SerializeField] private Vector3 bubbleOffset = new Vector3(0, 2.5f, 0);

    [Header("Pray 설정")]
    [Tooltip("기도 상태를 유지할 시간 (초)")]
    [SerializeField] private float prayDuration = 3.0f;

    [Header("Speech 설정")]
    [Tooltip("연설 상태를 유지할 시간 (초)")]
    [SerializeField] private float speechDuration = 3.0f;

    // 대기열 상태인지 확인하는 플래그
    private bool isWaitingInLine = false;
    // 진짜 기도 위치 저장용
    private Vector3 finalPrayerPos;

    // Scheme 상태 NPC
    public bool IsSchemer { get; private set; } = false;

    private SpriteRenderer spriteRenderer;

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
    private Coroutine aiWanderCoroutine;        //Idle
    private Coroutine chatSequenceCoroutine;    //Chat
    private Coroutine praySequenceCoroutine;    //Praying
    private Coroutine speechSequenceCoroutine;  //Speeching

    // 프로퍼티
    public bool IsMoving => pathCoroutine != null;
    public CardinalState CurrentState => currentState;
    public bool ConClaving { get; set; } = false;

    void Awake()
    {
        cardinal = GetComponent<Cardinal>();
        agent = GetComponent<NavMeshAgent>();
        inputController = GetComponent<ICardinalController>();
        spriteRenderer = GetComponentInChildren<SpriteRenderer>();
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
            case CardinalState.InSpeech: // 연설 중 상태
                
                break;
            case CardinalState.ReadyInSpeech: // 연설 준비 상태
               
                break;
            case CardinalState.Scheme:  //공작가 상태... 쉽지 않..
                HandleSchemeState();
                break;
                // 다른 상태들...
        }
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
                // Idle 진입 시 에이전트 상태 강제 초기화
                if (agent != null && agent.isOnNavMesh)
                {
                    agent.isStopped = false;
                    agent.ResetPath();
                }

                if (!CompareTag("Player") && aiWanderCoroutine == null)
                {
                    aiWanderCoroutine = StartCoroutine(AIWanderRoutine());
                }
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
                // 컷씬 진입 시 이동 방해 요소 제거
                if (agent != null && agent.isOnNavMesh)
                {
                    agent.isStopped = false; // 이동 가능하도록 설정
                    agent.ResetPath();
                    agent.avoidancePriority = 50; // 우선순위 복구
                }

                if (cardinal != null)
                {
                    cardinal.SetAgentSize(0.1f, 0.1f);
                }
                break;
            case CardinalState.ReadyPraying:
                if (cardinal != null)
                {
                    cardinal.SetAgentSize(0.1f, 0.1f);
                }
                if (agent.isOnNavMesh) agent.ResetPath(); // 기존 경로 초기화
                agent.avoidancePriority = 0;
                break;
            case CardinalState.Praying:
                if (cardinal != null)
                {
                    cardinal.SetAgentSize(0.1f, 0.1f);
                }

                if (agent.isOnNavMesh)
                {
                    agent.ResetPath();
                    agent.isStopped = true;
                    agent.velocity = Vector3.zero;
                }

                ShowBubble(prayingBubblePrefab);
                break;
            case CardinalState.ReadyInSpeech:
                if (cardinal != null) cardinal.SetAgentSize(0.1f, 0.1f); // 크기 조절

                if (agent != null && agent.isOnNavMesh)
                {
                    agent.isStopped = false;
                    agent.avoidancePriority = 50;
                }
                break;

            case CardinalState.InSpeech:
                if (cardinal != null) cardinal.SetAgentSize(0.1f, 0.1f); // 크기 조절

                // 기존 시퀀스 코루틴 정리 -----> 레거시 함수.. 나중에 버그 발생시 활성화

                if (speechSequenceCoroutine != null)
                {
                    // StopCoroutine(speechSequenceCoroutine); 
                    // speechSequenceCoroutine = null;
                }

                if (agent != null && agent.isOnNavMesh)
                {
                    agent.ResetPath();
                    agent.isStopped = true;
                    agent.velocity = Vector3.zero;
                    agent.avoidancePriority = 50;
                }
                ShowBubble(SpeechingBubblePrefab);
                break;
            case CardinalState.Scheme:
                if (agent != null && agent.isOnNavMesh)
                {
                    agent.isStopped = false;
                    agent.ResetPath();
                }
                if (aiWanderCoroutine == null)
                {
                    aiWanderCoroutine = StartCoroutine(AIWanderRoutine());
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

            case CardinalState.ReadyPraying:
                if (cardinal != null)
                {
                    cardinal.SetAgentSize(0.5f, 1f);
                }
                if (agent != null) agent.avoidancePriority = 50;
                break;

            case CardinalState.Praying:
                if (cardinal != null)
                {
                    cardinal.SetAgentSize(0.5f, 1f);
                }

                if (praySequenceCoroutine != null)
                {
                    StopCoroutine(praySequenceCoroutine);
                    praySequenceCoroutine = null;
                }

                if (agent != null && agent.isOnNavMesh)
                {
                    agent.avoidancePriority = 50;
                    agent.isStopped = false;
                    agent.ResetPath();
                }

                HideBubble();
                break;
            case CardinalState.ReadyInSpeech:
                if (cardinal != null) cardinal.SetAgentSize(0.5f, 1f); // 원래 크기로
                if (agent != null && agent.isOnNavMesh)
                {
                    agent.avoidancePriority = 50;
                }
                break;

            case CardinalState.InSpeech:
                if (cardinal != null) cardinal.SetAgentSize(0.5f, 1f); // 원래 크기로

                if (agent != null && agent.isOnNavMesh)
                {
                    agent.avoidancePriority = 50;
                    agent.isStopped = false;
                    agent.ResetPath();
                }
                HideBubble();
                break;
            case CardinalState.Scheme:
                if (aiWanderCoroutine != null)
                {
                    StopCoroutine(aiWanderCoroutine);
                    aiWanderCoroutine = null;
                }
                if (agent != null && agent.isOnNavMesh) agent.ResetPath();
                break;
        }
    }

    // ---------------------------------------------------------
    // 상태별 로직
    // ---------------------------------------------------------

    // 배회상태일때
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

    //공작 상태일때 
    void HandleSchemeState()
    {

        if (aiWanderCoroutine == null)
        {
            aiWanderCoroutine = StartCoroutine(AIWanderRoutine());
        }
    }

    void HandleCutSceneState()
    {
        // 컷씬 중 필요한 로직 작성.. 아직 작성하지 않아 남겨둠
    }

    // ---------------------------------------------------------
    // 플레이어 입력 처리 (이동속도는 Cardinal.cs 참조)
    // ---------------------------------------------------------

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

    void HandlePlayerInput()
    {
        if (inputController == null || agent == null) return;

        CardinalInputData input = inputController.GetInput();

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
    public void RestoreStateAfterAction()
    {
        if (IsSchemer)
        {
            ChangeState(CardinalState.Scheme);
        }
        else
        {
            ChangeState(CardinalState.Idle);
        }
    }
    

    // ---------------------------------------------------------
    // [이동 로직] AI 배회 (Idle)
    // ---------------------------------------------------------

    private IEnumerator AIWanderRoutine()
    {
        while (currentState == CardinalState.Idle || currentState == CardinalState.Scheme)
        {
            if (currentState != CardinalState.Idle && currentState != CardinalState.Scheme) yield break;

            
            if (CardinalManager.Instance != null && CardinalManager.Instance.GetCurrentChatMasterCount() < 2)
            {
                if (Random.Range(0f, 100f) < ChatMaster)
                {
                    ChangeState(CardinalState.ChatMaster);
                    yield break;
                }
            }

            Vector3 targetPosition;
            bool isRecoveryMove = false;

            float currentLimitY = (wanderLimitTransform != null) ? wanderLimitTransform.position.y : transform.position.y;

            if (transform.position.y > currentLimitY)
            {
                // 기준선보다 1~3m 아래로 강제 설정
                float recoveryY = currentLimitY - Random.Range(1.0f, 3.0f);
                float randomX = transform.position.x + Random.Range(-3.0f, 3.0f);

                targetPosition = new Vector3(randomX, recoveryY, 0);
                isRecoveryMove = true;
            }

            else
            {
                Vector2 randomCircle = Random.insideUnitCircle * Random.Range(2.0f, 5.0f);
                targetPosition = transform.position + new Vector3(randomCircle.x, randomCircle.y, 0);

                if (targetPosition.y > currentLimitY)
                {
                    targetPosition.y = currentLimitY - Random.Range(0.1f, 1.0f);
                }
            }

            // =========================================================
            // NavMesh 이동 명령 
            // =========================================================
            NavMeshHit hit;
            float sampleRange = isRecoveryMove ? 5.0f : 2.0f;

            if (NavMesh.SamplePosition(targetPosition, out hit, sampleRange, NavMesh.AllAreas))
            {
                if (agent.isOnNavMesh)
                {
                    agent.SetDestination(hit.position);
                    agent.isStopped = false;
                }
            }
            yield return new WaitUntil(() =>
                (currentState != CardinalState.Idle && currentState != CardinalState.Scheme) ||
                (!agent.pathPending && agent.remainingDistance <= agent.stoppingDistance && agent.velocity.sqrMagnitude <= 0.1f)
            );

            if (currentState == CardinalState.Idle || currentState == CardinalState.Scheme)
            {
                float waitTime = isRecoveryMove ? Random.Range(0.5f, 1.5f) : Random.Range(1.5f, 4f);
                float timer = 0f;
                Vector2 randomLookDir = Random.insideUnitCircle.normalized;

                while (timer < waitTime)
                {

                    if (currentState != CardinalState.Idle && currentState != CardinalState.Scheme) yield break;

                    if (animController != null) animController.SetLookDirection(randomLookDir);
                    if (agent != null) agent.velocity = Vector3.zero;
                    timer += Time.deltaTime;
                    yield return null;
                }
            }
        }
    }

    // ---------------------------------------------------------
    // [이동 로직] 컷씬 강제 이동 
    // ---------------------------------------------------------
    public void MoveToWaypoints(Transform[] pathNodes)
    {
        // 실행 중인 모든 시퀀스 코루틴 강제 종료
        if (chatSequenceCoroutine != null)
        {
            StopCoroutine(chatSequenceCoroutine);
            chatSequenceCoroutine = null;
        }
        if (praySequenceCoroutine != null)
        {
            StopCoroutine(praySequenceCoroutine);
            praySequenceCoroutine = null;
        }
        if (aiWanderCoroutine != null)
        {
            StopCoroutine(aiWanderCoroutine);
            aiWanderCoroutine = null;
        }

        if (speechSequenceCoroutine != null)
        {
            StopCoroutine(speechSequenceCoroutine);
            speechSequenceCoroutine = null;
        }

        // 말풍선 등 정리
        HideBubble();

        // 상태 변경 (CutScene)
        ChangeState(CardinalState.CutScene);

        //에이전트 물리 상태 강제 리셋
        if (agent != null && agent.isOnNavMesh)
        {
            agent.isStopped = false;        
            agent.ResetPath();              
            agent.velocity = Vector3.zero;  
            agent.avoidancePriority = 50;   
        }

        // 경로 설정 및 이동 시작
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

    public void EnterChatListener()
    {
        if (currentState != CardinalState.CutScene && currentState != CardinalState.ChatMaster)
        {
            ChangeState(CardinalState.Chatting);
        }
    }

    public void MoveToPosition(Vector3 targetPos)
    {
        if (agent != null && agent.isOnNavMesh)
        {
            agent.isStopped = false;
            agent.SetDestination(targetPos);
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
        // 이동 정지
        if (agent.isOnNavMesh) agent.ResetPath();

        // 위치 선정
        Vector3 spawnPos = transform.position;
        Animator myAnim = GetComponentInChildren<Animator>();
        if (myAnim != null)
        {
            Vector2 facingDir = new Vector2(myAnim.GetFloat("InputX"), myAnim.GetFloat("InputY"));
            if (facingDir == Vector2.zero) facingDir = Vector2.down;
            spawnPos += (Vector3)facingDir.normalized * 1.5f;
        }

        // ChatTrigger 생성
        GameObject triggerObj = null;
        ChatTrigger triggerScript = null;
        BoxCollider2D triggerCollider = null;

        if (chatTriggerPrefab != null)
        {
            triggerObj = Instantiate(chatTriggerPrefab, spawnPos, Quaternion.identity);
            triggerScript = triggerObj.GetComponent<ChatTrigger>();
            triggerCollider = triggerObj.GetComponent<BoxCollider2D>();
        }

        bool playerDetected = false;

        // 초기 대기 
        yield return new WaitForSeconds(0.5f);

        // 리스너 선별 (플레이어 제외)
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

        // 배치 로직
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
        // 대형 완성 -> 다각형 콜라이더 생성
        // -----------------------------------------------------------------------
        if (triggerScript != null)
        {
            List<Transform> participantTransforms = new List<Transform>();
            foreach (var p in allParticipants) participantTransforms.Add(p.transform);

            triggerScript.CreateFormationCollider(participantTransforms);
        }

        // -----------------------------------------------------------------------
        // 대화 진행 
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

        foreach (var listener in listeners)
        {
            if (listener.CurrentState == CardinalState.Chatting) listener.ChangeState(CardinalState.Idle);
        }
        if (triggerObj != null) Destroy(triggerObj);
        ChangeState(CardinalState.Idle);
    }

    // ---------------------------------------------------------
    // 기도 시퀀스
    // ---------------------------------------------------------


    public void OrderToPray(Vector3 targetPos, bool isQueueing)
    {
        // 상태 초기화
        isWaitingInLine = isQueueing;
        finalPrayerPos = Vector3.zero; 

        ChangeState(CardinalState.ReadyPraying);

        if (praySequenceCoroutine != null) StopCoroutine(praySequenceCoroutine);
        praySequenceCoroutine = StartCoroutine(ProcessPraySequence(targetPos));
    }

    public void ProceedToRealPrayer(Vector3 realTarget)
    {
        if (currentState == CardinalState.ReadyPraying && isWaitingInLine)
        {
            isWaitingInLine = false; 
            finalPrayerPos = realTarget; 
        }
    }


    private IEnumerator ProcessPraySequence(Vector3 firstTargetPos)
    {
        if (agent.isOnNavMesh)
        {
            agent.SetDestination(firstTargetPos);
            agent.isStopped = false;
        }

        // 1차 목적지 도착 대기
        yield return new WaitUntil(() =>
            !agent.pathPending &&
            agent.remainingDistance <= agent.stoppingDistance &&
            agent.velocity.sqrMagnitude <= 0.1f
        );

        if (currentState != CardinalState.ReadyPraying) yield break;


        if (isWaitingInLine)
        {
            if (agent.isOnNavMesh)
            {
                agent.isStopped = true;
                agent.velocity = Vector3.zero;
            }

            // 대기 중 방향 고정
            Vector2 waitDir = Vector2.left;

            // 대기 신호가 꺼질 때까지 계속 루프
            while (isWaitingInLine)
            {
                // 매 프레임 왼쪽을 보도록 설정
                if (animController != null) animController.SetLookDirection(waitDir);

                if (agent != null) agent.velocity = Vector3.zero;

                yield return null; // 다음 프레임까지 대기
            }
        }

        if (finalPrayerPos != Vector3.zero)
        {
            if (agent.isOnNavMesh)
            {
                agent.isStopped = false;
                agent.SetDestination(finalPrayerPos);
            }

            // 기도 장소 도착 대기
            yield return new WaitUntil(() =>
                !agent.pathPending &&
                agent.remainingDistance <= agent.stoppingDistance &&
                agent.velocity.sqrMagnitude <= 0.1f
            );
        }

        if (currentState != CardinalState.ReadyPraying) yield break;

        ChangeState(CardinalState.Praying);

        // 왼쪽 보기 ,Priority 변경
        Vector2 leftDir = Vector2.left;
        float rotateTimer = 0f;
        while (rotateTimer < 0.5f)
        {
            if (currentState != CardinalState.Praying) yield break;
            if (animController != null) animController.SetLookDirection(leftDir);
            if (agent != null) agent.velocity = Vector3.zero;
            rotateTimer += Time.deltaTime;
            yield return null;
        }

        if (agent != null) agent.avoidancePriority = 0;

        float currentPrayTimer = 0f;
        while (currentPrayTimer < prayDuration)
        {
            if (currentState != CardinalState.Praying) yield break;

            // 지속적인 방향 보정 
            if (animController != null) animController.SetLookDirection(leftDir);

            if (agent != null) agent.velocity = Vector3.zero;

            currentPrayTimer += Time.deltaTime;
            yield return null; 
        }

        //기도 함수 호출
        if (cardinal != null)
        {
            cardinal.Pray(); 
        }

        ChangeState(CardinalState.Idle);
    }

    // =================================================================
    // 연설(Speech) 시퀀스 함수들
    // =================================================================

    public void OrderToSpeech(Vector3 targetPos, bool isQueueing)
    {
        isWaitingInLine = isQueueing;
        finalPrayerPos = Vector3.zero; 

        ChangeState(CardinalState.ReadyInSpeech);

        if (speechSequenceCoroutine != null) StopCoroutine(speechSequenceCoroutine);
        speechSequenceCoroutine = StartCoroutine(ProcessSpeechSequence(targetPos));
    }

    public void ProceedToRealSpeech(Vector3 realTarget)
    {
        if (currentState == CardinalState.ReadyInSpeech && isWaitingInLine)
        {
            isWaitingInLine = false;
            finalPrayerPos = realTarget;
        }
    }

    private IEnumerator ProcessSpeechSequence(Vector3 firstTargetPos)
    {
        // 대기소 이동
        if (agent.isOnNavMesh)
        {
            agent.SetDestination(firstTargetPos);
            agent.isStopped = false;
        }

        yield return new WaitUntil(() =>
            !agent.pathPending &&
            agent.remainingDistance <= agent.stoppingDistance &&
            agent.velocity.sqrMagnitude <= 0.1f
        );

        if (currentState != CardinalState.ReadyInSpeech) yield break;

        // 대기열 대기 (Queueing)
        if (isWaitingInLine)
        {
            if (agent.isOnNavMesh)
            {
                agent.isStopped = true;
                agent.velocity = Vector3.zero;
            }

            Vector2 waitDir = Vector2.right; // 대기 방향 (필요 시 수정)

            while (isWaitingInLine)
            {
                if (animController != null) animController.SetLookDirection(waitDir);
                if (agent != null) agent.velocity = Vector3.zero;
                yield return null;
            }
        }

        // 진짜 연설 장소 이동
        if (finalPrayerPos != Vector3.zero)
        {
            if (agent.isOnNavMesh)
            {
                agent.isStopped = false;
                agent.SetDestination(finalPrayerPos);
            }

            yield return new WaitUntil(() =>
                !agent.pathPending &&
                agent.remainingDistance <= agent.stoppingDistance &&
                agent.velocity.sqrMagnitude <= 0.1f
            );
        }

        if (currentState != CardinalState.ReadyInSpeech) yield break;

        // 연설 시작 (InSpeech)
        ChangeState(CardinalState.InSpeech);

        Vector2 speechDir = Vector2.down;

        float rotateTimer = 0f;
        while (rotateTimer < 0.5f)
        {
            if (currentState != CardinalState.InSpeech) yield break;
            if (animController != null) animController.SetLookDirection(speechDir);
            if (agent != null) agent.velocity = Vector3.zero;
            rotateTimer += Time.deltaTime;
            yield return null;
        }

        if (agent != null) agent.avoidancePriority = 0;

        float currentSpeechTimer = 0f;
        while (currentSpeechTimer < speechDuration) 
        {
            if (currentState != CardinalState.InSpeech) yield break;
            if (animController != null) animController.SetLookDirection(speechDir);
            if (agent != null) agent.velocity = Vector3.zero;
            currentSpeechTimer += Time.deltaTime;
            yield return null;
        }

        if (cardinal != null)
        {
            cardinal.Speech(); // Cardinal.cs의 Speech() 호출
        }

        ChangeState(CardinalState.Idle);
    }

    // =========================================================
    // Scheme 상태로 만드는 함수
    // =========================================================
    public void SetSchemerMode(bool active)
    {
        IsSchemer = active;

        if (active)
        {
            if (spriteRenderer != null) spriteRenderer.color = Color.blue;

            if (currentState == CardinalState.Idle || currentState == CardinalState.Chatting)
            {
                ChangeState(CardinalState.Scheme);
            }
        }
        else
        {
            // 색상 복구 (흰색)
            if (spriteRenderer != null) spriteRenderer.color = Color.white;
            IsSchemer = false;
            ChangeState(CardinalState.Idle);
        }
    }


    private void OnTriggerEnter2D(Collider2D other)
    {
        // 플레이어와 충돌
        if (currentState == CardinalState.Scheme && other.CompareTag("Player"))
        {
            Debug.Log($"[Scheme] 모략가 {name}가 플레이어를 감지했습니다!");

            // Plot() 함수 실행 
        }
    }

   
}