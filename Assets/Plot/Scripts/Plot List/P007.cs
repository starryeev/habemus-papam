
using UnityEngine;

[CreateAssetMenu(fileName = "P007", menuName = "Plot/드랍 더 비트", order = 007)]

public class P007 : Plot
{

    [Header("해당 공작 설정")]
    [SerializeField] private int minInfluence;
    [SerializeField] private int pietyCost;

    public override int cost => pietyCost;

    void Reset()
    {
        // 설정 기본값
        plotID = "P007";
        plotGrade = PlotGrade.Common;

        // 수치 기본값
        plotWeightBase = 15;
        plotWeightMultiplier = 0f;

        minInfluence = 4;
        pietyCost = 2;

        // 아이콘 기본값
        PlotIconImageIndexes iconIndexes = new PlotIconImageIndexes();
        iconIndexes.icon1 = 6;
        iconIndexes.icon3 = 7;
        plotIconImageIndexes = iconIndexes;

        // 텍스트 기본값
        plotName = "드랍 더 비트";
        plotDescription = "새긴다! 태양의 비트!";
        plotEffect = "플레이어의 현재 턴 행동 횟수 1회 감소, 모든 NPC의 다음 턴 행동 1회 차단";
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
            InGameManager.Instance.ChangeCurrentTurnActions(-1,
                PlayerActionEffectSourceType.Plot, plotID, plotName,
                PlayerActionEffectPersistence.CurrentDay, true);
            for (int candidateNumber = 1; candidateNumber <= 3; candidateNumber++)
            {
                InGameManager.Instance.BlockNpcNextTurnAction(candidateNumber);
            }
        }
    }

}
