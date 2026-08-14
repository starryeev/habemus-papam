using UnityEngine;

[CreateAssetMenu(fileName = "I008", menuName = "Items/태양 팔찌")]
public class I008 : Item
{
    [Header("태양 팔찌 설정")]
    [Tooltip("기도 시 추가로 회복할 경건함 수치")]
    [SerializeField] private float pietyBonus;

    void Reset()
    {
        itemID = "I008";
        itemName = "태양 팔찌";
        itemDescription = "태양의 힘이 확실히 느껴지는 경건한 팔찌다!";
        itemEffectDescription = "기도 시 <color=#FFD84D>경건함</color>을 <color=#66CCFF>+1</color> 추가 획득한다.";

        itemGrade = ItemGrade.Common;
        itemExpirationType = ItemExpirationType.Permanent;
        usageType = ItemUsageType.Passive; 

        pietyBonus = 1f;
    }

    public override void OnPray(Cardinal owner)
    {
        float beforePiety = owner.Piety;

        owner.ChangePiety(pietyBonus);

        Debug.Log($"[아이템 효과 발동] 태양 팔찌: 경건함 {beforePiety} -> {owner.Piety} (변화량: +{pietyBonus})");
    }

    public override float PreviewPrayerPiety(float originalDelta, GameBalance balance, bool isSuccess)
    {
        return originalDelta + pietyBonus;
    }
}
