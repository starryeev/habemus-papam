using UnityEngine;

[CreateAssetMenu(fileName = "GameBalance", menuName = "Scriptable Objects/GameBalance")]
public class GameBalance : ScriptableObject
{
    [Header("추기경 기본 설정")]
    [Tooltip("추기경 기본 체력")]
    [SerializeField] private int initialHp = 100;
    public int InitialHp => initialHp;

    [Tooltip("추기경 기본 정치력")]
    [SerializeField] private int initialInfluence = 20;
    public int InitialInfluence => initialInfluence;

    [Tooltip("추기경 기본 경건함")]
    [SerializeField] private int initialPiety = 20;
    public int InitialPiety => initialPiety;

    [Tooltip("추기경 기본 이동속도 계수")]
    [SerializeField] private float initialMoveSpeed = 3.0f;
    public float InitialMoveSpeed => initialMoveSpeed;

    [Header("게임 진행 설정")]
    [Tooltip("한 콘클라베 당 자유 행동 시간")]
    [SerializeField] private float maxConclaveTime = 60.0f;
    public float MaxConclaveTime => maxConclaveTime;
}
