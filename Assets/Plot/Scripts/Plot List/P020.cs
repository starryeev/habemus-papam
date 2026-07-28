using UnityEngine;

[CreateAssetMenu(fileName = "P020", menuName = "Plot/선물 개봉", order = 020)]

public class P020 : Plot
{
    [Header("해당 공작 설정")]
    [SerializeField] private int minInfluence;
    [SerializeField] private int pietyCost;
    [SerializeField] private int rewardCount;

    public override int cost => pietyCost;

    void Reset()
    {
        // 설정 기본값
        plotID = "P020";
        plotGrade = PlotGrade.Rare;

        // 수치 기본값
        plotWeightBase = 20;
        plotWeightMultiplier = 0f;

        minInfluence = 0;
        pietyCost = 20;
        rewardCount = 2;

        // 텍스트 기본값
        plotName = "선물 개봉";
        plotDescription = "한 번 만에 나왔는데 이거 좋은 건가요?";
        plotEffect = "랜덤 아이템 2개 획득";
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
            GiveRandomItem();
        }
    }

    private void GiveRandomItem()
    {
        GameObject itemPrefab = InGameManager.Instance.GetRandomItemPrefab();

        if (itemPrefab != null)
        {
            FieldItem rewardItem = itemPrefab.GetComponent<FieldItem>();

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
