using UnityEngine;

[CreateAssetMenu(fileName = "P018", menuName = "Plot/스몰 토크", order = 018)]

public class P018 : Plot
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
        plotID = "P018";
        plotGrade = PlotGrade.Rare;

        // 수치 기본값
        plotWeightBase = 20;
        plotWeightMultiplier = 0f;

        minInfluence = 4;
        pietyCost = 4;
        hpDelta = -2;
        influenceDelta = 2;

        // 아이콘 기본값
        PlotIconImageIndexes iconIndexes = new PlotIconImageIndexes();
        iconIndexes.icon1 = 2;
        iconIndexes.icon1S = 3;
        iconIndexes.icon2 = 6;
        iconIndexes.icon3 = 0;
        iconIndexes.icon3S = 4;
        plotIconImageIndexes = iconIndexes;

        // 텍스트 기본값
        plotName = "스몰 토크";
        plotDescription = "제가 LA에 갔을 때 말이죠...";
        plotEffect = "정치력<sprite name=influence> 2 증가\n모든 상대 후보 체력<sprite name=hp> 2 감소";
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
            ApplyHpDelta(performer, cm.Cardinals[i], hpDelta);
        }
    }

}
