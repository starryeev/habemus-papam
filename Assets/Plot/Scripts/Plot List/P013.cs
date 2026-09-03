using UnityEngine;

[CreateAssetMenu(fileName = "P013", menuName = "Plot/심도 있는 논의", order = 013)]

public class P013 : Plot
{

    [Header("해당 공작 설정")]
    [SerializeField] private int minInfluence;
    [SerializeField] private int pietyCost;
    [SerializeField] private int influenceDelta;

    public override int cost => pietyCost;

    void Reset()
    {
        // 설정 기본값
        plotID = "P013";
        plotGrade = PlotGrade.Rare;

        // 수치 기본값
        plotWeightBase = 10;
        plotWeightMultiplier = 0.15f;

        minInfluence = 1;
        pietyCost = 2;
        influenceDelta = 2;

        // 아이콘 기본값
        PlotIconImageIndexes iconIndexes = new PlotIconImageIndexes();
        iconIndexes.icon2 = 2;
        iconIndexes.icon2S = 3;
        plotIconImageIndexes = iconIndexes;

        // 텍스트 기본값
        plotName = "심도 있는 논의";
        plotDescription = "파인애플 피자는 이단인가?";
        plotEffect = "정치력<sprite name=influence> 2 증가";
        plotCondiText = $"<sprite name=influence>{minInfluence}<sprite name=up>";
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
        if(!CanExecute(performer)) return;

        PayCost(performer);

        performer.ChangeInfluence(influenceDelta);
    }
    
}
