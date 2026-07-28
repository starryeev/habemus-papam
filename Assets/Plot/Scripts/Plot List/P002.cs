using UnityEngine;

[CreateAssetMenu(fileName = "P002", menuName = "Plot/은밀한 논의", order = 002)]

public class P002 : Plot
{
    [Header("해당 공작 설정")]
    [SerializeField] private int maxInfluence;
    [SerializeField] private int pietyCost;
    [SerializeField] private int influenceDelta;

    public override int cost => pietyCost;

    void Reset()
    {
        // 설정 기본값
        plotID = "P002";
        plotGrade = PlotGrade.Common;

        // 수치 기본값
        plotWeightBase = 20;
        plotWeightMultiplier = 0f;

        maxInfluence = 30;
        pietyCost = 20;
        influenceDelta = 15;

        // 텍스트 기본값
        plotName = "은밀한 논의";
        plotDescription = "...점심 뭐 먹지?";
        plotEffect = "정치력<sprite name=influence> 15 증가";
        plotCondiText = $"<sprite name=influence>{maxInfluence}<sprite name=down>";
        plotCostText = $"<sprite name=piety>  {cost}";

    }

    public override bool CanExecute(Cardinal performer)
    {
        return performer.Influence <= maxInfluence;
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
