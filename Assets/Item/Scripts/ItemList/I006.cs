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
        itemEffectDescription = "사용한 날 동안 기도 시 <color=#5BD65B>체력</color>을 <color=#66CCFF>+1</color> 추가 회복한다.";

        prayerBonusHp = 1;
    }

    public override void OnUse()
    {
        Cardinal player = FindPlayer();
        if (player != null)
        {
            Debug.Log("[아이템 사용] 캬! 최고급 태양주를 들이켰습니다. 몸에 열기가 돕니다.");
        }
    }

    public override void OnPray(Cardinal owner)
    {
        owner.ChangeHp(prayerBonusHp);
        Debug.Log($"[버프 효과] 최고급 태양주의 기운! 기도 추가 회복: +{prayerBonusHp}");
    }

    private Cardinal FindPlayer()
    {
        if (InventoryManager.Instance != null) return InventoryManager.Instance.Player;
        return null;
    }
}
