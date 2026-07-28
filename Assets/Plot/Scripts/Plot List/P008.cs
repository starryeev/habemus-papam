
using UnityEngine;

[CreateAssetMenu(fileName = "P008", menuName = "Plot/삼위일체", order = 008)]

public class P008 : Plot
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
        plotID = "P008";
        plotGrade = PlotGrade.Common;

        // 수치 기본값
        plotWeightBase = 20;
        plotWeightMultiplier = 0f;

        minInfluence = 3;
        pietyCost = 0;
        hpDelta = 3;
        influenceDelta = 3;
        pietyDelta = 3;

        // 텍스트 기본값
        plotName = "삼위일체";
        plotDescription = "트포다 트포";
        plotEffect = "체력<sprite name=hp> 33 증가\n정치력<sprite name=influence>, 경건함<sprite name=piety> 3 증가";
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
