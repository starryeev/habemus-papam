
using UnityEngine;

[CreateAssetMenu(fileName = "P004", menuName = "Plot/광대 놀음", order = 004)]

public class P004 : Plot
{
    [Header("해당 공작 설정")]
    [SerializeField] private int minInfluence;
    [SerializeField] private int pietyCost;
    [SerializeField] private int influenceDelta;

    public override int cost => pietyCost;

    void Reset()
    {
        // 설정 기본값
        plotID = "P004";
        plotGrade = PlotGrade.Common;

        // 수치 기본값
        plotWeightBase = 10;
        plotWeightMultiplier = 0.1f;

        minInfluence = 6;
        pietyCost = 3;
        influenceDelta = -1;

        // 텍스트 기본값
        plotName = "광대 놀음";
        plotDescription = "상갓집 개 노릇";
        plotEffect = "정치력<sprite name=influence> 10 감소";
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
