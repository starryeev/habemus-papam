using UnityEngine;
using static UnityEngine.GraphicsBuffer;

[CreateAssetMenu(fileName = "P032", menuName = "Plot/굴뚝 조작", order = 032)]

public class P032 : Plot
{

    [Header("해당 공작 설정")]
    [SerializeField] private int minInfluence;
    [SerializeField] private int pietyCost;
    [SerializeField] private GameObject smokeBomb;
    [SerializeField] private int rewardCount;

    public override int cost => pietyCost;

    void Reset()
    {
        // 설정 기본값
        plotID = "P032";
        plotGrade = PlotGrade.Legendary;

        // 수치 기본값
        plotWeightBase = 15;
        plotWeightMultiplier = 0f;

        minInfluence = 7;
        pietyCost = 7;
        rewardCount = 1;

        // 아이콘 기본값
        PlotIconImageIndexes iconIndexes = new PlotIconImageIndexes();
        iconIndexes.icon2 = 7;
        plotIconImageIndexes = iconIndexes;

        // 텍스트 기본값
        plotName = "굴뚝 조작";
        plotDescription = "금단의 비기";
        plotEffect = "특수 아이템 '연막탄' 1개 획득";
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

        for (int i = 0; i < rewardCount; i++)
        {
            FieldItem rewardItem = smokeBomb.GetComponent<FieldItem>();

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
