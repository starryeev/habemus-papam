using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class Cardinal : MonoBehaviour
{
    [Header("추기경 기본 설정")]
    [Tooltip("추기경 기본 체력")]
    [SerializeField] private int hp = 100;

    [Tooltip("추기경 기본 정치력")]
    [SerializeField] private int influence = 20;

    [Tooltip("추기경 기본 경건함")]
    [SerializeField] private int piety= 20;

    [Header("이동 관련 설정")]
    [SerializeField] private float moveSpeed = 3.0f;

    // 추기경 멤버변수
    private List<Item> items;

    // 기타 멤버변수
    private ICardinalController controller;
    private Rigidbody2D rb;
    private Vector2? targetPos = null;  // 이동 시 목표지점

    private NavMeshAgent agent;

    // 추기경 기본 프로퍼티 설정
    public int Hp => hp;
    public int Influence => influence;
    public int Piety => piety;


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

    void Start() {}

    void Update()
    {
        if (agent != null)
        {
            // Player , AI_NPC 구분
            if (CompareTag("Player"))
            {
                CardinalInputData input = controller.GetInput();
                
                //1순위 키보드 이동 
                if(input.moveDirection != Vector2.zero)
                {
                    MoveByKeyboard(input.moveDirection);
                }
                //2순위 마우스 이동
                else if (input.targetPos.HasValue)
                {
                    MoveToTargetPos(input.targetPos.Value);
                }
                else
                {
                    if(!agent.hasPath && agent.velocity.sqrMagnitude> 0.01f)
                    {
                        agent.velocity = Vector3.zero;
                    }
                }
            }
            
        }
        
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
    public void Pray() {}

    // 연설 함수
    public void Speech() {}

    // 공작 함수
    public void Plot() {}
}
