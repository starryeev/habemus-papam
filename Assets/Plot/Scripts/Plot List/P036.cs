using UnityEngine;

[CreateAssetMenu(fileName = "P036", menuName = "Plot/푸욱 쉬기", order = 036)]
public class P036 : Plot
{

    [Header("해당 공작 설정")]
    [SerializeField] private int minInfluence;
    [SerializeField] private int pietyCost;
    [SerializeField] private int hpDelta;

    public override int cost => pietyCost;

    void Reset()
    {
        // 설정 기본값
        plotID = "P036";
        plotGrade = PlotGrade.Rare;

        // 수치 기본값
        plotWeightBase = 20;
        plotWeightMultiplier = 0f;

        minInfluence = 6;
        pietyCost = 3;
        hpDelta = 4;

        // 아이콘 기본값
        PlotIconImageIndexes iconIndexes = new PlotIconImageIndexes();
        iconIndexes.icon2 = 0;
        iconIndexes.icon2S = 3;
        plotIconImageIndexes = iconIndexes;

        // 텍스트 기본값
        plotName = "푸욱 쉬기";
        plotDescription = "진짜 푸욱 쉬었어 추기경?";
        plotEffect = "체력<sprite name=hp> 4 증가";
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

        performer.ChangeHp(hpDelta);
    }
}

