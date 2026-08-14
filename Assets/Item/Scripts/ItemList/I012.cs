using UnityEngine;

[CreateAssetMenu(fileName = "I012", menuName = "Items/연막탄")]
public class I012 : Item
{
    void Reset()
    {
        itemID = "I012";
        itemGrade = ItemGrade.Rare;
        itemExpirationType = ItemExpirationType.Permanent;
        usageType = ItemUsageType.Passive;

        itemName = "연막탄";
        itemDescription = "굴뚝에 몰래 넣어서 연기 색깔을 바꾼다!\n무슨 색인지는 모르겠지만 검정이면 좋겠다...";
        itemEffectDescription = "교황으로 선출될 때 <b>(10 - 현재 <color=#FFD84D>경건함</color>) × 10%</b> 확률로 당선을 막는다. 성공하면 아이템이 소모된다.";
    }

    public override void OnAcquire()
    {
        Debug.Log("[아이템] 연막탄 획득: 선출 시 자동으로 작동 대기합니다.");
    }

    public bool TryDefendElection(float playerPiety)
    {
        float defenseChance = Mathf.Clamp((10f - playerPiety) * 10f, 0f, 100f);
        float roll = Random.Range(0f, 100f);

        if (roll < defenseChance)
        {
            Debug.Log($"<color=yellow>[아이템 효과]</color> 연막탄 작동 성공! (확률: {defenseChance:F1}%)");
            ConsumeItem(); 
            return true; 
        }

        Debug.Log($"<color=red>[아이템 효과]</color> 연막탄이 불발되었습니다. (확률: {defenseChance:F1}%)");

        return false;
    }

    private void ConsumeItem()
    {
        if (InventoryManager.Instance != null)
        {
            InventoryManager.Instance.RemoveItem(this);
        }
    }

    public override void OnRemove() { }
    public override void OnReapply(Cardinal owner) { }
    public override void OnUse() { } 
}
