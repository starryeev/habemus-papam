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
            
            moveDir = new Vector2(agent.velocity.x, agent.velocity.y).normalized;
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