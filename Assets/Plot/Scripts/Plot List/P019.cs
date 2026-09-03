using UnityEngine;

[CreateAssetMenu(fileName = "P019", menuName = "Plot/목 좀 축이세요", order = 019)]

public class P019 : Plot
{

    [Header("해당 공작 설정")]
    [SerializeField] private int maxInfluence;
    [SerializeField] private int pietyCost;

    public override int cost => pietyCost;

    void Reset()
    {
        // 설정 기본값
        plotID = "P019";
        plotGrade = PlotGrade.Rare;

        // 수치 기본값
        plotWeightBase = 15;
        plotWeightMultiplier = 0f;

        maxInfluence = 4;
        pietyCost = 4;

        // 아이콘 기본값
        PlotIconImageIndexes iconIndexes = new PlotIconImageIndexes();
        iconIndexes.icon2 = 7;
        plotIconImageIndexes = iconIndexes;

        // 텍스트 기본값
        plotName = "목 좀 축이세요";
        plotDescription = "푸룬 주스가 뭘까...?";
        plotEffect = "모든 후보의 다음 2회 행동 불가";
        plotCondiText = $"<sprite name=influence>{maxInfluence}<sprite name=up>";
        plotCostText = $"<sprite name=piety>  {cost}";

    }

    public override bool CanExecute(Cardinal performer)
    {
        return performer.Influence >= maxInfluence;
    }

    public override bool IsCostEnough(Cardinal performer)
    {
        return performer.Piety >= cost;
    }

    public override void Execute(Cardinal performer)
    {
        if (!CanExecute(performer)) return;

        PayCost(performer);

        if (InGameManager.Instance != null)
        {
            InGameManager.Instance.BlockPlayerActions(2,
                PlayerActionEffectSourceType.Plot, plotID, plotName,
                PlayerActionEffectPersistence.CurrentDay, true);
            for (int candidateNumber = 1; candidateNumber <= 3; candidateNumber++)
                InGameManager.Instance.BlockNpcNextTurnActions(candidateNumber, 2);
        }
    }

}
