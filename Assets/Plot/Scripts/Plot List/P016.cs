using UnityEngine;

[CreateAssetMenu(fileName = "P016", menuName = "Plot/삼위일체?", order = 016)]

public class P016 : Plot
{
    [Header("해당 공작 설정")]
    [SerializeField] private int minHp;
    [SerializeField] private int minInfluence;
    [SerializeField] private int minPiety;
    [SerializeField] private int pietyCost;
    [SerializeField] private int hpDelta;
    [SerializeField] private int influenceDelta;
    [SerializeField] private int pietyDelta;

    public override int cost => pietyCost;

    void Reset()
    {
        // 설정 기본값
        plotID = "P016";
        plotGrade = PlotGrade.Common;

        // 수치 기본값
        plotWeightBase = 20;
        plotWeightMultiplier = 0f;

        minHp = 33;
        minInfluence = 33;
        minPiety = 33;
        pietyCost = 0;
        hpDelta = 33;
        influenceDelta = -33;
        pietyDelta = -33;

        // 텍스트 기본값
        plotName = "삼위일체?";
        plotDescription = "삼삼 금지";
        plotEffect = "체력<sprite name=hp> 33 증가\n정치력<sprite name=influence>, 경건함<sprite name=piety> 33 감소";
        plotCondiText = $"<sprite name=hp>{minHp}<sprite name=hp> <sprite name=influence>{minInfluence}<sprite name=up> <sprite name=piety>{minPiety}<sprite name=up>";
        plotCostText = $"<sprite name=piety>  {cost}";

    }

    public override bool CanExecute(Cardinal performer)
    {
        return (performer.Influence >= minInfluence && 
            performer.Hp >= minHp &&
            performer.Piety >= minPiety);
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
