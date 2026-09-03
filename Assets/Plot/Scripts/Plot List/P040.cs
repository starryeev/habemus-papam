using UnityEngine;

[CreateAssetMenu(fileName = "P040", menuName = "Plot/뇌물 주기", order = 040)]
public class P040 : Plot
{

    [Header("해당 공작 설정")]
    [SerializeField] private int minInfluence;
    [SerializeField] private int pietyCost;
    [SerializeField] private int statsDelta;

    public override int cost => pietyCost;

    void Reset()
    {
        // 설정 기본값
        plotID = "P040";
        plotGrade = PlotGrade.Common;

        // 수치 기본값
        plotWeightBase = 20;
        plotWeightMultiplier = 0f;

        minInfluence = 9;
        pietyCost = 9;
        statsDelta = 7;

        // 아이콘 기본값
        PlotIconImageIndexes iconIndexes = new PlotIconImageIndexes();
        iconIndexes.icon1 = 5;
        iconIndexes.icon1S = 11;
        iconIndexes.icon2 = 1;
        iconIndexes.icon2S = 3;
        iconIndexes.icon3 = 2;
        iconIndexes.icon3S = 3;
        plotIconImageIndexes = iconIndexes;

        // 텍스트 기본값
        plotName = "뇌물 주기";
        plotDescription = "엄청난 뇌물해야";
        plotEffect = "무작위 상대 후보 한 명 경건함<sprite name=piety>, 정치력<sprite name=influence> 7 증가";
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
        cm.Cardinals[targetIndex].ChangePiety(statsDelta);
        cm.Cardinals[targetIndex].ChangeInfluence(statsDelta);
    }
}

