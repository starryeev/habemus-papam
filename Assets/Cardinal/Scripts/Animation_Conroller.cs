using UnityEngine;
using UnityEngine.AI;

public class Animation_Controller : MonoBehaviour
{
    private Animator animator;
    private NavMeshAgent agent;

    private Vector2 lastMoveDirection = Vector2.down;
    private bool isPlayer = false; // 플레이어 여부를 저장할 변수

    void Start()
    {
        // 컴포넌트 할당
        animator = GetComponent<Animator>();
        // NavMeshAgent가 같은 오브젝트에 있을 수도, 부모에 있을 수도 있으므로 둘 다 고려
        agent = GetComponent<NavMeshAgent>();
        if (agent == null) agent = GetComponentInParent<NavMeshAgent>();

        if (agent == null)
        {
            Debug.LogError($"{gameObject.name} : NavMeshAgent를 찾을 수 없습니다!");
        }

        // 이 오브젝트가 플레이어인지 태그로 확인하여 저장
        if (gameObject.CompareTag("Player"))
        {
            isPlayer = true;
        }
    }

    void Update()
    {
        HandleAnimation();
    }

    void HandleAnimation()
    {
        if (animator == null) return;

        Vector2 moveDir = Vector2.zero;
        bool isMoving = false;

        float h = 0f;
        float v = 0f;

        // 1. 키보드 입력 확인
        if (isPlayer)
        {
            h = Input.GetAxisRaw("Horizontal");
            v = Input.GetAxisRaw("Vertical");
        }

        // 입력이 감지되면 (플레이어의 직접 조작)
        if (h != 0 || v != 0)
        {
            moveDir = new Vector2(h, v).normalized;
            isMoving = true;
        }
        // 2. NavMeshAgent 속도 확인 (플레이어가 입력을 안 하거나, AI인 경우)
        else if (agent != null && agent.velocity.sqrMagnitude > 0.1f)
        {
            // 3D NavMeshAgent를 사용할 경우 velocity.z가 세로(Vertical) 방향인 경우가 많습니다.
            // 만약 2D 프로젝트(XY평면)라면 velocity.y가 맞지만, 
            // 3D 프로젝트(XZ평면)라면 아래 주석처럼 x, z를 사용해야 합니다.

            // [현재 코드 유지]: 2D 게임이거나 Y축 이동을 사용하는 경우
            moveDir = new Vector2(agent.velocity.x, agent.velocity.y).normalized;

            // [참고]: 일반적인 3D Top-Down 뷰인 경우 보통 아래와 같이 씁니다.
            // moveDir = new Vector2(agent.velocity.x, agent.velocity.z).normalized; 

            isMoving = true;
        }

        // 3. 애니메이터 업데이트
        if (isMoving)
        {
            lastMoveDirection = moveDir;
            animator.SetFloat("InputX", moveDir.x);
            animator.SetFloat("InputY", moveDir.y);
            animator.SetBool("IsMoving", true);
        }
        else
        {
            animator.SetFloat("InputX", lastMoveDirection.x);
            animator.SetFloat("InputY", lastMoveDirection.y);
            animator.SetBool("IsMoving", false);
        }
    }
}