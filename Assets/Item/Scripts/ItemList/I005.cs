using UnityEngine;

[CreateAssetMenu(fileName = "I005", menuName = "Items/태양주")]
public class I005 : Item
{
    [Header("태양주 설정")]
    [Tooltip("기도 시 추가로 회복할 체력량")]
    [SerializeField] private int prayerBonusHp;

    public override bool IsDurationBuff => true;

    void Reset()
    {
        itemID = "I005";
        itemGrade = ItemGrade.Common;
        itemExpirationType = ItemExpirationType.Conclave;

        usageType = ItemUsageType.Active;

        itemName = "태양주";
        itemDescription = "고향집 뒷산 포도밭에서 따온 포도를 대충 증류해서 만든 달달한 술";
        itemEffectDescription = "사용한 콘클라베 동안 기도 시 <color=#5BD65B>체력</color>을 <color=#66CCFF>+1</color> 추가 회복한다.";

        prayerBonusHp = 1;
    }

    public override void OnUse()
    {
        
        Cardinal player = FindPlayer();
        if (player != null)
        {
            Debug.Log("[아이템 사용] 캬! 태양주를 들이켰습니다. 아우 취한다.");
        }
    }

    public override void OnPray(Cardinal owner)
    {
        owner.ChangeHp(prayerBonusHp);
        Debug.Log($"[버프 효과] 그냥 태양주의 기운! 기도 추가 회복: +{prayerBonusHp}");
    }

    private Cardinal FindPlayer()
    {
        if (InventoryManager.Instance != null) return InventoryManager.Instance.Player;
        return null;
    }

}
