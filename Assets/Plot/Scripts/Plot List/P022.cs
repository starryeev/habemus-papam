using UnityEngine;
using System.Collections;

[CreateAssetMenu(fileName = "P022", menuName = "Plot/꼬리 자르기", order = 022)]

public class P022 : Plot
{
    [Header("해당 공작 설정")]
    [SerializeField] private int minHp;
    [SerializeField] private int hpCost;
    [SerializeField] private float speedPercentDelta;
    [SerializeField] private int duration;

    public override int cost => hpCost;
    public override PlotCostResource CostResource => PlotCostResource.Hp;

    void Reset()
    {
        // 설정 기본값
        plotID = "P022";
        plotGrade = PlotGrade.Rare;

        // 수치 기본값
        plotWeightBase = 10;
        plotWeightMultiplier = 0.1f;

        minHp = 50;
        hpCost = 30;
        speedPercentDelta = 0.3f;
        duration = 30;

        // 텍스트 기본값
        plotName = "꼬리 자르기";
        plotDescription = "쌀을 내주고 벼를 취한다";
        plotEffect = "이동 속도 30% 증가 30초";
        plotCondiText = $"<sprite name=hp>{minHp}<sprite name=up>";
        plotCostText = $"<sprite name=hp>  {cost}";
    }

    public override bool CanExecute(Cardinal performer)
    {
        return performer.Hp >= minHp;
    }

    public override bool IsCostEnough(Cardinal performer)
    {
        return performer.Hp >= cost;
    }

    public override void Execute(Cardinal performer)
    {
        if (!CanExecute(performer)) return;

        PayCost(performer);

        performer.StartCoroutine(SpeedBoostRoutine(performer));
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
