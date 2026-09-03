using System.Linq;
using UnityEngine;
using static UnityEngine.GraphicsBuffer;

[CreateAssetMenu(fileName = "P034", menuName = "Plot/언더독", order = 034)]

public class P034 : Plot
{

    [Header("해당 공작 설정")]
    [SerializeField] private int minInfluence;
    [SerializeField] private int pietyCost;
    [SerializeField] private int pietyDelta;

    public override int cost => pietyCost;


    void Reset()
    {
        // 설정 기본값
        plotID = "P034";
        plotGrade = PlotGrade.Legendary;

        // 수치 기본값
        plotWeightBase = 20;
        plotWeightMultiplier = 0f;

        minInfluence = 5;
        pietyCost = 4;
        pietyDelta = 6;

        // 아이콘 기본값
        PlotIconImageIndexes iconIndexes = new PlotIconImageIndexes();
        iconIndexes.icon1 = 5;
        iconIndexes.icon1S = 0;
        iconIndexes.icon2 = 0;
        iconIndexes.icon2S = 3;
        iconIndexes.icon3 = 1;
        iconIndexes.icon3S = 3;
        plotIconImageIndexes = iconIndexes;

        // 텍스트 기본값
        plotName = "언더독";
        plotDescription = "중요한 것은 깎이지 않는 마음";
        plotEffect = "자신을 포함한 가장 체력<sprite name=hp>이 낮은 후보 정치력<sprite name=influence>, 경건함<sprite name=piety> 6 증가";
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

        var candidates = cm.Cardinals.Take(3).ToList();

        if (!candidates.Contains(performer))
        {
            candidates.Add(performer);
        }

        var target = candidates
            .OrderBy(c => c.Hp)
            .ThenBy(c => Random.value)
            .FirstOrDefault();

        if (target != null)
        {
            target.ChangeInfluence(pietyDelta);
            target.ChangePiety(pietyDelta);
        }
    }

}
