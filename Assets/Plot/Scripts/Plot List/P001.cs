using NUnit.Framework;
using UnityEngine;

[CreateAssetMenu(fileName = "P001", menuName = "Plot/한 숨 돌리기", order = 001)]

public class P001 : Plot
{
    [Header("해당 공작 설정")]
    [SerializeField] private int minInfluence;
    [SerializeField] private int pietyCost;
    [SerializeField] private int hpDelta;

    public override int cost => pietyCost;

    void Reset()
    {
        // 설정 기본값
        plotID = "P001";
        plotGrade = PlotGrade.Common;

        // 수치 기본값
        plotWeightBase = 20;
        plotWeightMultiplier = 0f;

        minInfluence = 2;
        pietyCost = 2;
        hpDelta = 2;

        // 텍스트 기본값
        plotName = "한 숨 돌리기";
        plotDescription = "휴우...";
        plotEffect = "체력<sprite name=hp> 20 증가";
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
    }
    
}
