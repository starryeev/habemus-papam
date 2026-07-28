using UnityEngine;

[CreateAssetMenu(fileName = "P019", menuName = "Plot/목 좀 축이세요", order = 019)]

public class P019 : Plot
{
    [Header("해당 공작 설정")]
    [SerializeField] private int maxInfluence;
    [SerializeField] private int pietyCost;

    public override int cost => pietyCost;

    void Reset()
    {
        // 설정 기본값
        plotID = "P019";
        plotGrade = PlotGrade.Rare;

        // 수치 기본값
        plotWeightBase = 15;
        plotWeightMultiplier = 0f;

        maxInfluence = 4;
        pietyCost = 4;

        // 텍스트 기본값
        plotName = "목 좀 축이세요";
        plotDescription = "푸룬 주스가 뭘까...?";
        plotEffect = "모든 후보의 다음 2회 행동 불가";
        plotCondiText = $"<sprite name=influence>{maxInfluence}<sprite name=down>";
        plotCostText = $"<sprite name=piety>  {cost}";

    }

    public override bool CanExecute(Cardinal performer)
    {
        return performer.Influence <= maxInfluence;
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

        int highestHpCardinal = 0;

        for (int i = 1; i < 3; i++)
        {
            if (cm.Cardinals[highestHpCardinal].Hp < cm.Cardinals[i].Hp)
            {
                highestHpCardinal = i;
            }
        }


        Cardinal target = cm.Cardinals[highestHpCardinal];

        if (target != null)
        {
            if (target.CompareTag("Player")) InGameManager.Instance.BlockPlayerTurnActions();
            else Debug.Log("[Turn] NPC 행동 슬롯 시스템 도입 전까지 P019 행동 불가 효과는 보류됩니다.");
        }
    }

}
