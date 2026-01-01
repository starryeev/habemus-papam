using UnityEngine;

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

    // 기타 멤버변수
    private ICardinalController controller;
    private Rigidbody2D rb;
    private Vector2? targetPos = null;  // 이동 시 목표지점

    // 추기경 기본 프로퍼티 설정
    public int Hp => hp;
    public int Influence => influence;
    public int Piety => piety;


    void Awake()
    {
        // 멤버변수 초기화
        rb = GetComponentInChildren<Rigidbody2D>();    
        controller = GetComponent<ICardinalController>();
    }

    void Start() {}

    void Update()
    {
        CardinalInputData input = controller.GetInput();
        
        if(input.targetPos.HasValue)
        {
            targetPos = input.targetPos.Value;
        }

        if(targetPos.HasValue)
        {
            MoveToTargetPos(targetPos.Value);
        }
    }

    void MoveToTargetPos(Vector2 targetPos)
    {
        if(Vector2.Distance(transform.position, targetPos) <= 0.01f)
        {
            transform.position = targetPos;
            this.targetPos = null;

            return;
        }

        // 실제 임시 이동 로직 추후에 NavMesh로 변경필요
        transform.position = Vector2.MoveTowards(transform.position, targetPos, moveSpeed * Time.deltaTime);
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
