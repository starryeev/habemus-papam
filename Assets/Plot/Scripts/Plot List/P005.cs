
using UnityEngine;

[CreateAssetMenu(fileName = "P005", menuName = "Plot/골탕 먹이기", order = 005)]

public class P005 : Plot
{

    [Header("해당 공작 설정")]
    [SerializeField] private int minInfluence;
    [SerializeField] private int pietyCost;
    [SerializeField] private int hpDelta;

    public override int cost => pietyCost;

    void Reset()
    {
        // 설정 기본값
        plotID = "P005";
        plotGrade = PlotGrade.Common;
        
        // 텍스트 기본값
        plotName = "골탕 먹이기";
        plotDescription = "안 아 줘 요";
        plotEffect = "무작위 상대 후보 한 명 체력<sprite name=hp> 2 감소";
        plotCondiText = $"<sprite name=influence>{minInfluence}<sprite name=up>";
        plotCostText = $"<sprite name=piety>  {cost}";

        // 아이콘 기본값
        PlotIconImageIndexes iconIndexes = new PlotIconImageIndexes();
        iconIndexes.icon1 = 5;
        iconIndexes.icon1S = 11;
        iconIndexes.icon3 = 0;
        iconIndexes.icon3S = 4;
        plotIconImageIndexes = iconIndexes;

        // 수치 기본값
        plotWeightBase = 20;
        plotWeightMultiplier = 0f;

        minInfluence = 2;
        pietyCost = 2;
        hpDelta = -2;
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
