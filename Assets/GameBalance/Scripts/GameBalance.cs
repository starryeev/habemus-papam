using UnityEngine;

[CreateAssetMenu(fileName = "GameBalance", menuName = "Scriptable Objects/GameBalance")]
public class GameBalance : ScriptableObject
{
    [Header("추기경 기본 설정")]
    [Tooltip("추기경 기본 체력")]
    [SerializeField] private float initialHp = 100f;
    public float InitialHp => initialHp;

    [Tooltip("추기경 기본 정치력")]
    [SerializeField] private float initialInfluence = 20f;
    public float InitialInfluence => initialInfluence;

    [Tooltip("추기경 기본 경건함")]
    [SerializeField] private float initialPiety = 20f;
    public float InitialPiety => initialPiety;

    [Tooltip("추기경 기본 이동속도 계수")]
    [SerializeField] private float initialMoveSpeed = 3.0f;
    public float InitialMoveSpeed => initialMoveSpeed;


    [Header("추기경 행동 - 기도 설정")]
    [Tooltip("기도 성공 확률")]
    [SerializeField] private float praySuccessChance = 0.8f;
    public float PraySuccessChance => praySuccessChance;

    [Tooltip("기도 성공 시 경건함 변화량")]
    [SerializeField] private float praySuccessDeltaPiety = 15f;
    public float PraySuccessDeltaPiety => praySuccessDeltaPiety;

    [Tooltip("기도 성공 시 체력 변화량")]
    [SerializeField] private float praySuccessDeltaHp = 10f;
    public float PraySuccessDeltaHp => praySuccessDeltaHp;

    [Tooltip("기도 실패 시 경건함 변화량")]
    [SerializeField] private float prayFailDeltaPiety = 5f;
    public float PrayFailDeltaPiety => prayFailDeltaPiety;

    [Tooltip("기도 실패 시 체력 변화량")]
    [SerializeField] private float prayFailDeltaHp = 20f;
    public float PrayFailDeltaHp => prayFailDeltaHp;


    [Header("추기경 행동 - 연설 설정")]
    [Tooltip("연설 성공 확률")]
    [SerializeField] private float speechSuccessChance = 0.9f;
    public float SpeechSuccessChance => speechSuccessChance;

    [Tooltip("연설 성공 시 정치력 변화량(최소)")]
    [SerializeField] private float speechSuccessDeltaInfluenceMin = 3f;
    public float SpeechSuccessDeltaInfluenceMin => speechSuccessDeltaInfluenceMin;

    [Tooltip("연설 성공 시 정치력 변화량(최대)")]
    [SerializeField] private float speechSuccessDeltaInfluenceMax = 7f;
    public float SpeechSuccessDeltaInfluenceMax => speechSuccessDeltaInfluenceMax;

    [Tooltip("연설 성공 시 체력 변화량")]
    [SerializeField] private float speechSuccessDeltaHp = -5f;
    public float SpeechSuccessDeltaHp => speechSuccessDeltaHp;

    [Tooltip("연설 실패 시 정치력 변화량")]
    [SerializeField] private float speechFailDeltaInfluence = -2f;
    public float SpeechFailDeltaInfluence => speechFailDeltaInfluence;

    [Tooltip("연설 실패 시 체력 변화량")]
    [SerializeField] private float speechFailDeltaHp = -5f;
    public float SpeechFailDeltaHp => speechFailDeltaHp;


    [Header("게임 진행 설정")]
    [Tooltip("한 콘클라베 당 자유 행동 시간")]
    [SerializeField] private float maxConclaveTime = 60.0f;
    public float MaxConclaveTime => maxConclaveTime;

    [Tooltip("초당 추기경 체력 변화량")]
    [SerializeField] private float hpDeltaPerSec = -0.5f;
    public float HpDeltaPerSec => hpDeltaPerSec;
}
