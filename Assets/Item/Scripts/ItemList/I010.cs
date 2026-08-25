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
        itemName = "교단 주보";
        itemDescription = "교단의 동향을 파악해 교주 자리를 한 발 빠르게 피한다.";
        itemEffectDescription = "오늘 연설 시 <color=#4488FF>정치력</color> 변화량을 <color=#66CCFF>0</color>으로 고정하고, 대신 <color=#FFD84D>경건함</color>을 <color=#66CCFF>+1</color> 회복";

        itemGrade = ItemGrade.Common;
        itemExpirationType = ItemExpirationType.Conclave; 
        usageType = ItemUsageType.Passive;

        pietyBonus = 1f;
    }

    public override float ModifySpeechInfluence(float originalDelta, GameBalance balance, bool isSuccess)
    {
        float fixedInfluence = 0;

        return fixedInfluence;
    }

    public override float PreviewSpeechInfluence(float originalDelta, GameBalance balance, bool isSuccess)
    {
        return 0;
    }

    public override void OnSpeech(Cardinal owner)
    {
        owner.ChangePiety(pietyBonus);
    }
}
