using UnityEngine;
using static UnityEngine.GraphicsBuffer;

[CreateAssetMenu(fileName = "P011", menuName = "Plot/태양의 혀", order = 011)]

public class P011 : Plot
{
    [Header("해당 공작 설정")]
    [SerializeField] private int minInfluence;
    [SerializeField] private int pietyCost;
    [SerializeField] private int hpIncrease;
    [SerializeField] private int hpDecrease;
    [SerializeField] private int influenceDelta;

    public override int cost => pietyCost;

    void Reset()
    {
        // 설정 기본값
        plotID = "P011";
        plotGrade = PlotGrade.Common;

        // 수치 기본값
        plotWeightBase = 20;
        plotWeightMultiplier = 0f;

        minInfluence = 20;
        pietyCost = 0;
        hpIncrease = 10;
        hpDecrease = -20;
        influenceDelta = 20;

        // 텍스트 기본값
        plotName = "태양의 혀";
        plotDescription = "태양의 혀를 지닌 자여, 정당함을 말하라.";
        plotEffect = "자신을 포함한 모든 후보, 50% 확률로 체력<sprite name=hp> 20 감소 및 정치력<sprite name=influence> 20 증가.\n50% 확률로 체력<sprite name=hp> 10 증가.";
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

        if (Random.value < 0.5f)
        {
            ApplyHpDelta(performer, performer, hpDecrease);
            performer.ChangeInfluence(influenceDelta);
        }
        else
        {
            performer.ChangeHp(hpIncrease);
        }

        for (int i = 0; i < 3; i++)
        {
            if (Random.value < 0.5f)
            {
                ApplyHpDelta(performer, cm.Cardinals[i], hpDecrease);
                cm.Cardinals[i].ChangeInfluence(influenceDelta);
            }
            else
            {
                cm.Cardinals[i].ChangeHp(hpIncrease);
            }
        }
    }

}
