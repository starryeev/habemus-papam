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

        minInfluence = 40;
        pietyCost = 35;
        hpDelta = -15;
        influenceDelta = 20;

        // 텍스트 기본값
        plotName = "스몰 토크";
        plotDescription = "제가 LA에 갔을 때 말이죠...";
        plotEffect = "정치력<sprite name=influence> 20 증가\n모든 상대 후보 체력<sprite name=hp> 15 감소";
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
