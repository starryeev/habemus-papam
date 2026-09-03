using System.Linq;
using UnityEngine;

[CreateAssetMenu(fileName = "P017", menuName = "Plot/이 불경한 자가", order = 017)]

public class P017 : Plot
{

    [Header("해당 공작 설정")]
    [SerializeField] private int minInfluence;
    [SerializeField] private int pietyCost;
    [SerializeField] private int hpDelta;

    public override int cost => pietyCost;

    void Reset()
    {
        // 설정 기본값
        plotID = "P017";
        plotGrade = PlotGrade.Rare;

        // 수치 기본값
        plotWeightBase = 10;
        plotWeightMultiplier = 0.05f;

        minInfluence = 5;
        pietyCost = 5;
        hpDelta = -4;

        // 아이콘 기본값
        PlotIconImageIndexes iconIndexes = new PlotIconImageIndexes();
        iconIndexes.icon1 = 5;
        iconIndexes.icon1S = 9;
        iconIndexes.icon3 = 0;
        iconIndexes.icon3S = 4;
        plotIconImageIndexes = iconIndexes;

        // 텍스트 기본값
        plotName = "이 불경한 자가";
        plotDescription = "깡!";
        plotEffect = "가장 경건함<sprite name=piety>이 낮은 상대 후보 체력<sprite name=hp> 4 감소";
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

        var target = cm.Cardinals.Take(3)
            .OrderByDescending(c => c.Hp)
            .ThenBy(c => Random.value)
            .LastOrDefault();

        ApplyHpDelta(performer, target, hpDelta);
    }

}
