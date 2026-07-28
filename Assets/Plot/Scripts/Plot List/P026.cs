using UnityEngine;

[CreateAssetMenu(fileName = "P026", menuName = "Plot/주사위 굴리기", order = 026)]

public class P026 : Plot
{
    [Header("해당 공작 설정")]
    [SerializeField] private int minInfluence;
    [SerializeField] private int pietyCost;
    [SerializeField] private GameObject diceItem;
    [SerializeField] private int rewardCount;

    public override int cost => pietyCost;

    void Reset()
    {
        // 설정 기본값
        plotID = "P026";
        plotGrade = PlotGrade.Rare;

        // 수치 기본값
        plotWeightBase = 15;
        plotWeightMultiplier = 0f;

        minInfluence = 0;
        pietyCost = 20;
        rewardCount = 2;

        // 텍스트 기본값
        plotName = "주사위 굴리기";
        plotDescription = "주사위는 던져졌다";
        plotEffect = "일반 아이템 '주사위' 2개 획득";
        plotCondiText = $"";
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

        for (int i = 0; i < rewardCount; i++)
        {
            FieldItem rewardItem = diceItem.GetComponent<FieldItem>();

            if (rewardItem != null)
            {
                Item data = rewardItem.ItemData;

                if (data != null)
                {
                    InventoryManager.Instance.AddItem(data);
                }
            }
        }
    }

}
