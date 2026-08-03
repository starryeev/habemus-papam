using UnityEngine;

[CreateAssetMenu(fileName = "P033", menuName = "Plot/나는 용서하마", order = 033)]

public class P033 : Plot
{
    [Header("해당 공작 설정")]
    [SerializeField] private int minInfluence;
    [SerializeField] private int pietyCost;

    public override int cost => pietyCost;

    void Reset()
    {
        // 설정 기본값
        plotID = "P033";
        plotGrade = PlotGrade.Legendary;

        // 수치 기본값
        plotWeightBase = 15;
        plotWeightMultiplier = 0f;

        minInfluence = 6;
        pietyCost = 6;

        // 텍스트 기본값
        plotName = "나는 용서하마";
        plotDescription = "하지만 이 녀석이 용서할까?";
        plotEffect = "다음 콘클라베 때, 이번 콘클라베 동안 감소한 체력<sprite name=hp>만큼 모든 상대 후보의 체력<sprite name=hp> 감소";
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
        InGameManager.Instance?.ScheduleNextConclaveRevenge(plotID, performer);
    }
}
