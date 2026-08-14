using UnityEngine;

[CreateAssetMenu(fileName = "I013", menuName = "Items/황금 경판")]
public class I013 : Item
{
    void Reset()
    {
        itemID = "I013";
        itemName = "황금 경판";
        itemDescription = "비밀의 고대 문자로 된 비밀 메세지를 담고 있는 아주 비밀스러운 황금 판이다. 그 내용은 비밀이다.";
        itemEffectDescription = "별도의 효과 없이 아이템 창 한 칸을 차지한다. 빛이 난다.";

        itemGrade = ItemGrade.Common; 
        itemExpirationType = ItemExpirationType.Permanent; 
        usageType = ItemUsageType.Passive; 
    }

    
}
