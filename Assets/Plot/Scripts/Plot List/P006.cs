
using UnityEngine;

[CreateAssetMenu(fileName = "P006", menuName = "Plot/커피 회동", order = 006)]

public class P006 : Plot
{

    [Header("해당 공작 설정")]
    [SerializeField] private int minInfluence;
    [SerializeField] private int pietyCost;
    [SerializeField] private int influenceDelta;

    public override int cost => pietyCost;

    void Reset()
    {
        // 설정 기본값
        plotID = "P006";
        plotGrade = PlotGrade.Common;

        // 수치 기본값
        plotWeightBase = 15;
        plotWeightMultiplier = 0f;

        minInfluence = 3;
        pietyCost = 2;
        influenceDelta = 1;

        // 아이콘 기본값
        PlotIconImageIndexes iconIndexes = new PlotIconImageIndexes();
        iconIndexes.icon1 = 6;
        iconIndexes.icon3 = 2;
        iconIndexes.icon3S = 3;
        plotIconImageIndexes = iconIndexes;

        // 텍스트 기본값
        plotName = "커피 회동";
        plotDescription = "커피 한 잔의 여유";
        plotEffect = "모든 후보 정치력<sprite name=influence> 1 증가";
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
        if (!CanExecute(performer)) return;

        PayCost(performer);

        var cm = CardinalManager.Instance;

        performer.ChangeInfluence(influenceDelta);

        for (int i = 0; i < 3; i++)
        {
            cm.Cardinals[i].ChangeInfluence(influenceDelta);
        }
    }
}
    
