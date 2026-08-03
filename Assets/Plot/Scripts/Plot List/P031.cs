using UnityEngine;
using static UnityEngine.GraphicsBuffer;

[CreateAssetMenu(fileName = "P031", menuName = "Plot/숙면", order = 031)]

public class P031 : Plot
{
    [Header("해당 공작 설정")]
    [SerializeField] private int minInfluence;
    [SerializeField] private int pietyCost;
    [SerializeField] private int influenceTarget;
    [SerializeField] private int hpTarget;

    public override int cost => pietyCost;

    void Reset()
    {
        // 설정 기본값
        plotID = "P031";
        plotGrade = PlotGrade.Legendary;

        // 수치 기본값
        plotWeightBase = 10;
        plotWeightMultiplier = 0.05f;

        minInfluence = 9;
        pietyCost = 5;
        influenceTarget = 2;
        hpTarget = 7;

        // 텍스트 기본값
        plotName = "숙면";
        plotDescription = "드르렁 쿨쿨...";
        plotEffect = "정치력을 2, 체력을 7로 조정\n남은 콘클라베 동안 기도 불가";
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

        float currentInfluence = performer.Influence;

        performer.ChangeInfluence(-currentInfluence);
        performer.ChangeInfluence(influenceTarget);
        performer.ChangeHp(hpTarget - performer.Hp);
        InGameManager.Instance?.BlockPrayerForCurrentConclave(performer);
    }

}
