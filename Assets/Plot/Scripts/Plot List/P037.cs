using UnityEngine;

[CreateAssetMenu(fileName = "P037", menuName = "Plot/위험한 장난", order = 037)]
public class P037 : Plot
{

    [Header("해당 공작 설정")]
    [SerializeField] private int minInfluence;
    [SerializeField] private int pietyCost;
    [SerializeField] private int hpDelta;

    public override int cost => pietyCost;

    void Reset()
    {
        // 설정 기본값
        plotID = "P037";
        plotGrade = PlotGrade.Common;

        // 수치 기본값
        plotWeightBase = 20;
        plotWeightMultiplier = 0f;

        minInfluence = 5;
        pietyCost = 4;
        hpDelta = -3;

        // 아이콘 기본값
        PlotIconImageIndexes iconIndexes = new PlotIconImageIndexes();
        iconIndexes.icon1 = 5;
        iconIndexes.icon1S = 11;
        iconIndexes.icon3 = 0;
        iconIndexes.icon3S = 4;
        plotIconImageIndexes = iconIndexes;

        // 텍스트 기본값
        plotName = "위험한 장난";
        plotDescription = "우리 다 보도록 해?";
        plotEffect = "무작위 상대 후보 한 명 체력<sprite name=hp> 3 감소";
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
        int targetIndex = Random.Range(0, 3);
        ApplyHpDelta(performer, cm.Cardinals[targetIndex], hpDelta);
    }
}

