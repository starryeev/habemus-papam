using UnityEngine;

[CreateAssetMenu(fileName = "P025", menuName = "Plot/앙코르", order = 025)]

public class P025 : Plot
{
    [Header("해당 공작 설정")]
    [SerializeField] private int minInfluence;
    [SerializeField] private int pietyCost;
    [SerializeField] private int influenceDelta;

    public override int cost => pietyCost;

    void Reset()
    {
        // 설정 기본값
        plotID = "P025";
        plotGrade = PlotGrade.Rare;

        // 수치 기본값
        plotWeightBase = 10;
        plotWeightMultiplier = 0.1f;

        minInfluence = 6;
        pietyCost = 4;
        influenceDelta = 3;

        // 텍스트 기본값
        plotName = "앙코르";
        plotDescription = "좋은 건 한 번 더 해야지";
        plotEffect = "저번 콘클라베에서 선출되었던 후보 정치력<sprite name=influence> 30 증가";
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

        var em = ElectionManager.Instance;

        var lastConclaveWinner = em.CurrentWinnerCandidate;

        if (lastConclaveWinner == null)
        {
            return;
        }

        lastConclaveWinner.ChangeInfluence(influenceDelta);
    }

}
