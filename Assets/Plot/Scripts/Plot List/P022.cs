using UnityEngine;

[CreateAssetMenu(fileName = "P022", menuName = "Plot/꼬리 자르기", order = 022)]

public class P022 : Plot
{
    [Header("해당 공작 설정")]
    [SerializeField] private int minInfluence;
    [SerializeField] private int pietyCost;

    public override int cost => pietyCost;

    void Reset()
    {
        // 설정 기본값
        plotID = "P022";
        plotGrade = PlotGrade.Rare;

        // 수치 기본값
        plotWeightBase = 10;
        plotWeightMultiplier = 0.1f;

        minInfluence = 5;
        pietyCost = 7;

        // 텍스트 기본값
        plotName = "꼬리 자르기";
        plotDescription = "쌀을 내주고 벼를 취한다";
        plotEffect = "이번 턴 행동 가능 횟수 2회 추가";
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

        if (performer != null && performer.CompareTag("Player"))
        {
            InGameManager.Instance.AddCurrentTurnActions(2);
        }
    }

}
