using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class Cardinal : MonoBehaviour
{
    [Header("추기경 기본 설정")]
    [Tooltip("추기경 기본 체력")]
    [SerializeField] private float hp;

    [Tooltip("추기경 기본 정치력")]
    [SerializeField] private float influence;

    [Tooltip("추기경 기본 경건함")]
    [SerializeField] private float piety;

    [Header("이동 관련 설정")]
    [SerializeField] private float moveSpeed;

    // 추기경 멤버변수
    private List<Item> items;
    private NavMeshAgent agent;

    // 외부(StateController)에서 접근을 위한 프로퍼티
    public float Hp => hp;
    public float Influence => influence;
    public float Piety => piety;
    public float MoveSpeed => moveSpeed; 

    void Awake()
    {
        // 멤버변수 초기화
        items = new List<Item>();
        agent = GetComponent<NavMeshAgent>();

        if (agent != null)
        {
            agent.updateRotation = false;
            agent.updateUpAxis = false;
            agent.speed = moveSpeed;
        }
    }

    void Start()
    {
        InitCardinal();
    }

    

    void InitCardinal()
    {
        if (InGameManager.Instance != null)
        {
            GameBalance balance = InGameManager.Instance.Balance;
            hp = balance.InitialHp;
            influence = balance.InitialInfluence;
            piety = balance.InitialPiety;
            moveSpeed = balance.InitialMoveSpeed;

            // 초기화된 속도를 에이전트에도 적용
            if (agent != null) agent.speed = moveSpeed;
        }
    }

    // NavMeshAgent 크기 조절 (Manager에서 접근하므로 유지)
    public void SetAgentSize(float newRadius, float newHeight)
    {
        if (agent != null)
        {
            agent.radius = newRadius;
            agent.height = newHeight;
        }
    }

    public void ChangeHp(float delta)
    {
        hp = Mathf.Clamp(hp + delta, 0f, 100f);
    }

    public void ChangeInfluence(float delta)
    {
        influence = Mathf.Clamp(influence + delta, 0f, 100f);
    }

    public void ChangePiety(float delta)
    {
        piety = Mathf.Clamp(piety + delta, 0f, 100f);
    }

    // ---------------------------------------------------------
    // 고유 행동 함수 (기도, 연설)
    // ---------------------------------------------------------
    public void Pray()
    {
        if (InGameManager.Instance == null) return;
        GameBalance balance = InGameManager.Instance.Balance;

        foreach (var item in items)
        {
            item?.OnPray(this);
        }

        if (Random.value < balance.PraySuccessChance)
        {
            ChangePiety(balance.PraySuccessDeltaPiety);
            ChangeHp(balance.PraySuccessDeltaHp);
        }
        else
        {
            ChangePiety(balance.PrayFailDeltaPiety);
            ChangeHp(balance.PrayFailDeltaHp);
        }
    }

    public void Speech()
    {
        if (InGameManager.Instance == null) return;
        GameBalance balance = InGameManager.Instance.Balance;

        foreach (var item in items)
        {
            item?.OnSpeech(this);
        }

        if (Random.value < balance.SpeechSuccessChance)
        {
            // 연설 성공
            float speechSuccessDeltaInfluence = Random.Range(balance.SpeechSuccessDeltaInfluenceMin, balance.SpeechSuccessDeltaInfluenceMax + 1);
            ChangeInfluence(speechSuccessDeltaInfluence);
            ChangeHp(balance.SpeechSuccessDeltaHp);
        }
        else
        {
            ChangeInfluence(balance.SpeechFailDeltaInfluence);
            ChangeHp(balance.SpeechFailDeltaHp);
        }
    }

    public void Plot() { }
}