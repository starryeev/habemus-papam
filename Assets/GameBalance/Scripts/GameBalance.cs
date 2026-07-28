using UnityEngine;

[CreateAssetMenu(fileName = "GameBalance", menuName = "Scriptable Objects/GameBalance")]
public class GameBalance : ScriptableObject
{
    [Header("추기경 기본 설정")]
    [Tooltip("추기경 기본 체력")]
    [SerializeField] private float initialHp = 10f;
    public float InitialHp => initialHp;

    [Tooltip("추기경 기본 정치력")]
    [SerializeField] private float initialInfluence = 2f;
    public float InitialInfluence => initialInfluence;

    [Tooltip("추기경 기본 경건함")]
    [SerializeField] private float initialPiety = 2f;
    public float InitialPiety => initialPiety;

    [Tooltip("추기경 기본 이동속도 계수")]
    [SerializeField] private float initialMoveSpeed = 3.0f;
    public float InitialMoveSpeed => initialMoveSpeed;


    [Header("추기경 행동 - 기도 설정")]
    [Tooltip("기도 성공 확률")]
    [SerializeField] private float praySuccessChance = 0.8f;
    public float PraySuccessChance => praySuccessChance;

    [Tooltip("기도 성공 시 경건함 변화량")]
    [SerializeField] private float praySuccessDeltaPiety = 2f;
    public float PraySuccessDeltaPiety => praySuccessDeltaPiety;

    [Tooltip("기도 성공 시 체력 변화량")]
    [SerializeField] private float praySuccessDeltaHp = 1f;
    public float PraySuccessDeltaHp => praySuccessDeltaHp;

    [Tooltip("기도 실패 시 경건함 변화량")]
    [SerializeField] private float prayFailDeltaPiety = 1f;
    public float PrayFailDeltaPiety => prayFailDeltaPiety;

    [Tooltip("기도 실패 시 체력 변화량")]
    [SerializeField] private float prayFailDeltaHp = 2f;
    public float PrayFailDeltaHp => prayFailDeltaHp;


    [Header("추기경 행동 - 연설 설정")]
    [Tooltip("연설 성공 확률")]
    [SerializeField] private float speechSuccessChance = 0.9f;
    public float SpeechSuccessChance => speechSuccessChance;

    [Tooltip("연설 성공 시 정치력 변화량(최소)")]
    [SerializeField] private float speechSuccessDeltaInfluenceMin = 1f;
    public float SpeechSuccessDeltaInfluenceMin => speechSuccessDeltaInfluenceMin;

    [Tooltip("연설 성공 시 정치력 변화량(최대)")]
    [SerializeField] private float speechSuccessDeltaInfluenceMax = 1f;
    public float SpeechSuccessDeltaInfluenceMax => speechSuccessDeltaInfluenceMax;

    [Tooltip("연설 성공 시 체력 변화량")]
    [SerializeField] private float speechSuccessDeltaHp = -1f;
    public float SpeechSuccessDeltaHp => speechSuccessDeltaHp;

    [Tooltip("연설 실패 시 정치력 변화량")]
    [SerializeField] private float speechFailDeltaInfluence = -1f;
    public float SpeechFailDeltaInfluence => speechFailDeltaInfluence;

    [Tooltip("연설 실패 시 체력 변화량")]
    [SerializeField] private float speechFailDeltaHp = -1f;
    public float SpeechFailDeltaHp => speechFailDeltaHp;


}
