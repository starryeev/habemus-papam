using UnityEngine;

[CreateAssetMenu(fileName = "P027", menuName = "Plot/젠체하기", order = 027)]

public class P027 : Plot
{
    [Header("해당 공작 설정")]
    [SerializeField] private int minInfluence;
    [SerializeField] private int pietyCost;

    public override int cost => pietyCost;

    void Reset()
    {
        // 설정 기본값
        plotID = "P027";
        plotGrade = PlotGrade.Legendary;

        // 수치 기본값
        plotWeightBase = 20;
        plotWeightMultiplier = 0f;

        minInfluence = 6;
        pietyCost = 7;

        // 텍스트 기본값
        plotName = "젠체하기";
        plotDescription = "큰 망신을 당할 수도...";
        plotEffect = "그러나 아무 일도 일어나지 않았다";
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

        
    }

}
