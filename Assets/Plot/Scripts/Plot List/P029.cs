using UnityEngine;
using static UnityEngine.GraphicsBuffer;

[CreateAssetMenu(fileName = "P029", menuName = "Plot/점심 복사 버그(미구현)", order = 029)]

public class P029 : Plot
{
    [Header("해당 공작 설정")]
    [SerializeField] private int minInfluence;
    [SerializeField] private int pietyCost;
    [SerializeField] private int hpDelta;
    [SerializeField] private int influenceDelta;

    public override int cost => pietyCost;

    void Reset()
    {
        // 설정 기본값
        plotID = "P029";
        plotGrade = PlotGrade.Legendary;

        // 수치 기본값
        plotWeightBase = 5;
        plotWeightMultiplier = 0f;

        minInfluence = 0;
        pietyCost = 35;
        hpDelta = 5;
        influenceDelta = 2;

        // 텍스트 기본값
        plotName = "점심 복사 버그(미구현)";
        plotDescription = "떡과 생선이 복사가 돼요";
        plotEffect = "체력<sprite name=hp> 5 증가\n정치력<sprite name=influence> 2 증가\n(경건함)% 확률로 특수 엔딩";
        plotCondiText = $"";
        plotCostText = $"<sprite name=piety>  {cost}";
    }

    public override bool CanExecute(Cardinal performer)
    {
        return performer.Influence >= minInfluence;
    }

    public override bool IsCostEnough(Cardinal performer)
    {
        return performer.Piety >= cost;
    }

    public override void Execute(Cardinal performer)
    {
        if (!CanExecute(performer)) return;

        PayCost(performer);

        performer.ChangeHp(hpDelta);
        performer.ChangeInfluence(influenceDelta);

        float random = Random.Range(0f, 100f);

        if (random < performer.Piety)
        {
            // 특수 엔딩 실행 로직
        }
    }

}
