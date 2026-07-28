using UnityEngine;
using static UnityEngine.GraphicsBuffer;

[CreateAssetMenu(fileName = "P028", menuName = "Plot/레수르스망", order = 028)]

public class P028 : Plot
{
    [Header("해당 공작 설정")]
    [SerializeField] private int maxInfluence;
    [SerializeField] private int pietyCost;

    public override int cost => pietyCost;

    void Reset()
    {
        // 설정 기본값
        plotID = "P028";
        plotGrade = PlotGrade.Legendary;

        // 수치 기본값
        plotWeightBase = 5;
        plotWeightMultiplier = 0.15f;

        maxInfluence = 5;
        pietyCost = 4;

        // 텍스트 기본값
        plotName = "레수르스망";
        plotDescription = "나 다시 돌아갈래!";
        plotEffect = "모든 후보의 정치력<sprite name=influence>을 경건함<sprite name=piety>으로 전환";
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
        if (!CanExecute(performer)) return;

        PayCost(performer);

        var cm = CardinalManager.Instance;

        float currentInfluence = performer.Influence;

        performer.ChangeInfluence(-currentInfluence);
        performer.ChangePiety(currentInfluence);

        for (int i = 0; i < 3; i++)
        {
            currentInfluence = cm.Cardinals[i].Influence;

            cm.Cardinals[i].ChangeInfluence(-currentInfluence);
            cm.Cardinals[i].ChangePiety(currentInfluence);
        }

    }

}
