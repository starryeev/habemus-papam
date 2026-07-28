using UnityEngine;
[CreateAssetMenu(fileName = "P014", menuName = "Plot/태양 경배 자세", order = 014)]

public class P014 : Plot
{
    [Header("해당 공작 설정")]
    [SerializeField] private int minInfluence;
    [SerializeField] private int pietyCost;
    [SerializeField] private int pietyDelta;

    public override int cost => pietyCost;

    void Reset()
    {
        // 설정 기본값
        plotID = "P014";
        plotGrade = PlotGrade.Rare;

        // 수치 기본값
        plotWeightBase = 20;
        plotWeightMultiplier = 0f;

        minInfluence = 2;
        pietyCost = 0;
        pietyDelta = 3;

        // 텍스트 기본값
        plotName = "태양 경배 자세";
        plotDescription = "Yoga 고수가 될 거야!";
        plotEffect = "경건함<sprite name=piety> 30 증가";
        plotCondiText = $"";
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

        performer.ChangePiety(pietyDelta);
    }
    
}
