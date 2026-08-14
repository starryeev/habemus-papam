using UnityEngine;

[CreateAssetMenu(fileName = "I010", menuName = "Items/교황청 주보")]
public class I010 : Item
{
    [Header("교황청 주보 설정")]
    [Tooltip("연설 시 추가로 회복할 경건함 수치")]
    [SerializeField] private float pietyBonus;

    void Reset()
    {
        itemID = "I010";
        itemName = "교황청 주보";
        itemDescription = "교회의 동향을 파악해 교황 자리를 한 발 빠르게 피한다.";
        itemEffectDescription = "연설 시 <color=#4488FF>정치력</color> 변화량을 <color=#66CCFF>+1</color>로 고정하고 <color=#FFD84D>경건함</color>을 <color=#66CCFF>+1</color> 회복한다.";

        itemGrade = ItemGrade.Common;
        itemExpirationType = ItemExpirationType.Day; 
        usageType = ItemUsageType.Passive;

        pietyBonus = 1f;
    }

    public override float ModifySpeechInfluence(float originalDelta, GameBalance balance, bool isSuccess)
    {
        float fixedInfluence = balance.SpeechSuccessDeltaInfluenceMin;

        Debug.Log($"[아이템 효과] 교황청 주보: 원래 변동될 정치력({originalDelta})을 강제로 ({fixedInfluence})로 고정합니다.");

        return fixedInfluence;
    }

    public override void OnSpeech(Cardinal owner)
    {
        float beforePiety = owner.Piety;

        owner.ChangePiety(pietyBonus);

        Debug.Log($"[아이템 효과] 교황청 주보: 경건함 {beforePiety} -> {owner.Piety} (+{pietyBonus})");
    }
}
