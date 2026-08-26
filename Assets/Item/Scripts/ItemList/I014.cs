using UnityEngine;

[CreateAssetMenu(fileName = "I014", menuName = "Items/주사위")]
public class I014 : Item
{
    void Reset()
    {
        itemID = "I014";
        itemName = "주사위";
        itemDescription = "신비한 주사위다. 굴리다 보면 새로운 아이디어가 떠오른다!";
        itemEffectDescription = "사용 시, 공작 NPC의 공작들을 모두 새로고침";

        itemGrade = ItemGrade.Common; 
        itemExpirationType = ItemExpirationType.Permanent; 
        usageType = ItemUsageType.Active; 
    }

    public override void OnUse()
    {
        var pm = PlotManager.Instance;

        for (int i = 0; i < 2; i++)
        {
            pm.RerollPlotSet(i);
        }
    }
}
