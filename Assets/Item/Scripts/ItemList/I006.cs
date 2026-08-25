using UnityEngine;

[CreateAssetMenu(fileName = "I006", menuName = "Items/최고급 태양주")]
public class I006 : Item
{
    [Header("최고급 태양주 설정")]
    [Tooltip("기도 시 추가로 회복할 체력량")]
    [SerializeField] private int prayerBonusHp;

    public override bool IsDurationBuff => true;

    void Reset()
    {
        itemID = "I006";
        itemGrade = ItemGrade.Rare; 

        itemExpirationType = ItemExpirationType.Day;

        usageType = ItemUsageType.Active;

        itemName = "최고급 태양주";
        itemDescription = "최고급 이탈리아 포도를 발효한 후 수도원 지하에서 다섯 번 증류한 화끈한 술.";
        itemEffectDescription = "획득한 날짜 포함, 3일 동안 기도 시 <color=#5BD65B>체력</color> <color=#66CCFF>+1</color> 추가 회복";

        prayerBonusHp = 1;
    }

    public override void OnUse()
    {
    }

    public override void OnPray(Cardinal owner)
    {
        owner.ChangeHp(prayerBonusHp);
    }

    public override float PreviewPrayerHp(float originalDelta, GameBalance balance, bool isSuccess)
    {
        return originalDelta + prayerBonusHp;
    }
}
