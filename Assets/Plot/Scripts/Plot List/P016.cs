using UnityEngine;

[CreateAssetMenu(fileName = "P016", menuName = "Plot/삼위일체?", order = 016)]

public class P016 : Plot
{

    [Header("해당 공작 설정")]
    [SerializeField] private int minInfluence;
    [SerializeField] private int pietyCost;
    [SerializeField] private int hpDelta;
    [SerializeField] private int influenceDelta;
    [SerializeField] private int pietyDelta;

    public override int cost => pietyCost;

    void Reset()
    {
        // 설정 기본값
        plotID = "P016";
        plotGrade = PlotGrade.Rare;

        // 수치 기본값
        plotWeightBase = 20;
        plotWeightMultiplier = 0f;

        minInfluence = 3;
        pietyCost = 0;
        hpDelta = 3;
        influenceDelta = -3;
        pietyDelta = -3;

        // 아이콘 기본값
        PlotIconImageIndexes iconIndexes = new PlotIconImageIndexes();
        iconIndexes.icon1 = 0;
        iconIndexes.icon1S = 3;
        iconIndexes.icon2 = 1;
        iconIndexes.icon2S = 4;
        iconIndexes.icon3 = 2;
        iconIndexes.icon3S = 4;
        plotIconImageIndexes = iconIndexes;


        // 텍스트 기본값
        plotName = "삼위일체?";
        plotDescription = "삼삼 금지";
        plotEffect = "체력<sprite name=hp> 3 증가\n정치력<sprite name=influence>, 경건함<sprite name=piety> 3 감소";
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

        performer.ChangeHp(hpDelta);
        performer.ChangeInfluence(influenceDelta);
        performer.ChangePiety(pietyDelta);
    }
    
}
