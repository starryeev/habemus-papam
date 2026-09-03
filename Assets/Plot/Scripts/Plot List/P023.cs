using System.Linq;
using UnityEngine;

[CreateAssetMenu(fileName = "P023", menuName = "Plot/노블레스 오블리주", order = 023)]

public class P023 : Plot
{

    [Header("해당 공작 설정")]
    [SerializeField] private int minInfluence;
    [SerializeField] private int pietyCost;
    [SerializeField] private int pietyIncrease;
    [SerializeField] private int pietyDecrease;

    public override int cost => pietyCost;

    void Reset()
    {
        // 설정 기본값
        plotID = "P023";
        plotGrade = PlotGrade.Rare;

        // 수치 기본값
        plotWeightBase = 15;
        plotWeightMultiplier = 0f;

        minInfluence = 7;
        pietyCost = 0;
        pietyIncrease = 7;
        pietyDecrease = -3;

        // 아이콘 기본값
        PlotIconImageIndexes iconIndexes = new PlotIconImageIndexes();
        iconIndexes.icon1 = 5;
        iconIndexes.icon1S = 10;
        iconIndexes.icon2 = 1;
        iconIndexes.icon2S = 4;
        iconIndexes.icon3 = 1;
        iconIndexes.icon3S = 3;
        plotIconImageIndexes = iconIndexes;

        // 텍스트 기본값
        plotName = "노블레스 오블리주";
        plotDescription = "큰 힘에는 큰 책임이 따른다";
        plotEffect = "가장 정치력<sprite name=influence>이 높은 후보 경건함<sprite name=piety> 3 감소\n가장 정치력<sprite name=influence>이 낮은 후보 경건함<sprite name=piety> 7 증가";
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

        var candidates = cm.Cardinals.Take(3).ToList();
        if (!candidates.Contains(performer)) candidates.Add(performer);

        var sorted = candidates
            .OrderByDescending(c => c.Influence)    // 정치력 내림차순 정렬
            .ThenBy(c => Random.value)              // 동률 시 랜덤
            .ToList();

        Cardinal highest = sorted[0];
        Cardinal lowest = sorted[sorted.Count - 1];

        lowest.ChangePiety(pietyIncrease);
        highest.ChangePiety(pietyDecrease);
    }

}
