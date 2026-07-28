
using UnityEngine;

[CreateAssetMenu(fileName = "P009", menuName = "Plot/태양의 눈", order = 009)]

public class P009 : Plot
{
    [Header("해당 공작 설정")]
    [SerializeField] private int minInfluence;
    [SerializeField] private int pietyCost;
    [SerializeField] private int hpDelta;

    public override int cost => pietyCost;

    void Reset()
    {
        // 설정 기본값
        plotID = "P009";
        plotGrade = PlotGrade.Common;
        
        // 텍스트 기본값
        plotName = "태양의 눈";
        plotDescription = "태양의 눈을 지닌 자여, 신실함을 보여라.";
        plotEffect = "가장 경건함<sprite name=piety>이 낮은 상대 후보 체력<sprite name=hp> 20 감소";
        plotCondiText = $"<sprite name=influence>{minInfluence}<sprite name=up>";
        plotCostText = $"<sprite name=piety>  {cost}";

        // 수치 기본값
        plotWeightBase = 20;
        plotWeightMultiplier = 0f;

        minInfluence = 2;
        pietyCost = 0;
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

        int lowestPietyCardinal = 0;

        for (int i = 1; i < 3; i++)
        {
            if (cm.Cardinals[i].Piety < cm.Cardinals[lowestPietyCardinal].Piety)
            {
                lowestPietyCardinal = i;
            }
            else if (cm.Cardinals[i].Piety == cm.Cardinals[lowestPietyCardinal].Piety)
            {
                if (Random.value > 0.5f)
                {
                    lowestPietyCardinal = i;
                }
            }
        }

        ApplyHpDelta(performer, cm.Cardinals[lowestPietyCardinal], hpDelta);
    }

}
