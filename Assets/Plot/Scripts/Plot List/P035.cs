using UnityEngine;

[CreateAssetMenu(fileName = "P035", menuName = "Plot/체력 전부 회복", order = 035)]
public class P035 : Plot
{

    [Header("해당 공작 설정")]
    [SerializeField] private int minInfluence;
    [SerializeField] private int pietyCost;

    public override int cost => pietyCost;

    void Reset()
    {
        // 설정 기본값
        plotID = "P035";
        plotGrade = PlotGrade.Rare;

        // 수치 기본값
        plotWeightBase = 10;
        plotWeightMultiplier = 0.05f;

        minInfluence = 9;
        pietyCost = 4;

        // 아이콘 기본값
        PlotIconImageIndexes iconIndexes = new PlotIconImageIndexes();
        iconIndexes.icon1 = 0;
        iconIndexes.icon1S = 3;
        iconIndexes.icon2 = 0;
        iconIndexes.icon2S = 3;
        iconIndexes.icon3 = 0;
        iconIndexes.icon3S = 3;
        plotIconImageIndexes = iconIndexes;

        // 텍스트 기본값
        plotName = "체력 전부 회복";
        plotDescription = "설명은 필요 없다!";
        plotEffect = "체력<sprite name=hp> 전부 회복";
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

        performer.ChangeHp(performer.MaxHp - performer.Hp);
    }
}

