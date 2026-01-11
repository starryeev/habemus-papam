using UnityEngine;
using UnityEngine.AI;

public class Animation_Controller : MonoBehaviour
{
    private Animator animator;
    private NavMeshAgent agent;

    private Vector2 lastMoveDirection = Vector2.down;

    void Start()
    {
        
        //컴포넌트 할당
        animator = GetComponent<Animator>();
        agent = GetComponentInParent<NavMeshAgent>();

        
        if (agent == null)
        {
            Debug.LogError("부모 오브젝트에서 NavMeshAgent를 찾을 수 없습니다!");
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

       
        float h = Input.GetAxisRaw("Horizontal");
        float v = Input.GetAxisRaw("Vertical");

        // 1. 키보드 입력 확인
        if (h != 0 || v != 0)
        {
            moveDir = new Vector2(h, v).normalized;
            isMoving = true;
        }
        // 2. NavMeshAgent 속도 확인
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