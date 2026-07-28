
using UnityEngine;
using System.Collections;

[CreateAssetMenu(fileName = "P007", menuName = "Plot/드랍 더 비트", order = 007)]

public class P007 : Plot
{
    [Header("해당 공작 설정")]
    [SerializeField] private int minInfluence;
    [SerializeField] private int pietyCost;
    [SerializeField] private float speedPercentDelta;
    [SerializeField] private int duration;

    public override int cost => pietyCost;

    void Reset()
    {
        // 설정 기본값
        plotID = "P007";
        plotGrade = PlotGrade.Common;

        // 수치 기본값
        plotWeightBase = 15;
        plotWeightMultiplier = 0f;

        minInfluence = 20;
        pietyCost = 15;
        speedPercentDelta = 0.1f;
        duration = 20;

        // 텍스트 기본값
        plotName = "드랍 더 비트";
        plotDescription = "새긴다! 태양의 비트!";
        plotEffect = "모든 후보 이동속도 10% 증가 20초";
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

        performer.StartCoroutine(SpeedBoostRoutine(performer));

        for (int i = 0; i < 3; i++)
        {
            var target = cm.Cardinals[i];
            target.StartCoroutine(SpeedBoostRoutine(target));
        }
    }

    private IEnumerator SpeedBoostRoutine(Cardinal target)
    {
        if (target == null) yield break;

        float delta = speedPercentDelta;

        target.ChangeSpeed(delta);

        yield return new WaitForSeconds(duration);

        if (target != null)
        {
            target.ChangeSpeed(-delta);
        }
    }

}
