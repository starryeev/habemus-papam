using System.Linq;
using UnityEngine;

[CreateAssetMenu(fileName = "P015", menuName = "Plot/열렬한 찬양", order = 015)]

public class P015 : Plot
{
    [Header("해당 공작 설정")]
    [SerializeField] private int minInfluence;
    [SerializeField] private int pietyCost;
    [SerializeField] private int pietyDelta;

    public override int cost => pietyCost;

    void Reset()
    {
        // 설정 기본값
        plotID = "P015";
        plotGrade = PlotGrade.Common;

        // 수치 기본값
        plotWeightBase = 20;
        plotWeightMultiplier = 0f;

        minInfluence = 40;
        pietyCost = 15;
        pietyDelta = 30;

        // 텍스트 기본값
        plotName = "열렬한 찬양";
        plotDescription = "추기경님 축지법 쓰신다";
        plotEffect = "가장 정치력<sprite name=influence>이 높은 상대 후보 경건함<sprite name=piety> 30 증가";
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
            .OrderByDescending(c => c.Influence)
            .ThenBy(c => Random.value)
            .FirstOrDefault();

        target.ChangePiety(pietyDelta);
    }

}
