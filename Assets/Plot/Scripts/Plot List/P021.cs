using UnityEngine;

[CreateAssetMenu(fileName = "P021", menuName = "Plot/무릎 꿇기", order = 021)]

public class P021 : Plot
{

    [Header("해당 공작 설정")]
    [SerializeField] private int minInfluence;
    [SerializeField] private int pietyCost;
    [SerializeField] private int influenceDelta;
    [SerializeField] private int nextConclaveHpDamagePerConclave;

    public override int cost => pietyCost;

    void Reset()
    {
        // 설정 기본값
        plotID = "P021";
        plotGrade = PlotGrade.Rare;

        // 수치 기본값
        plotWeightBase = 15;
        plotWeightMultiplier = 0.05f;

        minInfluence = 6;
        pietyCost = 1;
        influenceDelta = -2;
        nextConclaveHpDamagePerConclave = 1;

        // 아이콘 기본값
        PlotIconImageIndexes iconIndexes = new PlotIconImageIndexes();
        iconIndexes.icon1 = 6;
        iconIndexes.icon2 = 2;
        iconIndexes.icon2S = 4;
        iconIndexes.icon3 = 0;
        iconIndexes.icon3S = 4;
        plotIconImageIndexes = iconIndexes;

        // 텍스트 기본값
        plotName = "무릎 꿇기";
        plotDescription = "추진력을 얻기 위함이었다!";
        plotEffect = "모든 상대 후보 정치력<sprite name=influence> 2 감소\n다음 날 첫 콘클라베 때 사용 시점에 따라 모든 상대 후보 정치력<sprite name=influence> 2 ~ 5 증가";
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

        InGameManager gameManager = InGameManager.Instance;
        if (gameManager == null) return;

        foreach (Cardinal target in gameManager.GetRepresentativeCandidates())
        {
            if (target != null && target != performer) target.ChangeInfluence(influenceDelta);
        }

        int pastConclaveCount = ActionRecordManager.Instance != null
            ? ActionRecordManager.Instance.GetCurrentConclaveCount()
            : 0;
        int totalHpDamage = Mathf.Max(0, pastConclaveCount/4) * nextConclaveHpDamagePerConclave;
        gameManager.ScheduleNextConclaveHpDamage(plotID, performer, totalHpDamage);
    }
}
