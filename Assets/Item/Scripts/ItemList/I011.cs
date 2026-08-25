using UnityEngine;

[CreateAssetMenu(fileName = "I011", menuName = "Items/교황청 비밀문서")]
public class I011 : Item
{
    [Header("교황청 비밀문서 설정")]
    [Tooltip("연설 시 회복할 체력과 경건함 수치")]
    [SerializeField] private float healAmount;

    // ▼▼▼ [핵심] 사용(Active) 시 인벤토리에서 사라지고 버프 리스트로 이동 ▼▼▼
    public override bool IsDurationBuff => true;

    void Reset()
    {
        itemID = "I011";
        itemName = "교단 비밀 문서";
        itemDescription = "이걸 폭로해서 모두의 눈길을 돌리면 자연스럽게 교주 자리를 피할 수 있다!";
        itemEffectDescription = "연설의 <color=#4488FF>정치력</color> 변화량 <color=#FF4D4D>0</color>으로 고정, 대신 <color=#5BD65B>체력</color>과 <color=#FFD84D>경건함</color>을 각각 <color=#66CCFF>+1</color>";

        itemGrade = ItemGrade.Rare; // 고급
        itemExpirationType = ItemExpirationType.Permanent;
        usageType = ItemUsageType.Passive;

        healAmount = 1f;
    }

    // 1. 인벤토리에서 클릭하여 사용했을 때
    public override void OnUse()
    {
        // (InventoryManager가 가상 인벤토리로 옮겨주는 처리를 알아서 해줍니다.)
    }

    // 2. 가상 인벤토리(버프)에 있는 동안: 연설 정치력 변화량 가로채기
    public override float ModifySpeechInfluence(float originalDelta, GameBalance balance, bool isSuccess)
    {
        // "정치력 대신" 이므로 원래 증가/감소할 정치력을 0으로 만듭니다.
        return 0f;
    }

    public override float PreviewSpeechInfluence(float originalDelta, GameBalance balance, bool isSuccess)
    {
        return 0f;
    }

    public override float PreviewSpeechHp(float originalDelta, GameBalance balance, bool isSuccess)
    {
        return originalDelta + healAmount;
    }

    // 3. 가상 인벤토리(버프)에 있는 동안: 연설이 끝난 직후 체력과 경건함 회복
    public override void OnSpeech(Cardinal owner)
    {
        // 체력과 경건함을 동시에 회복
        owner.ChangeHp(healAmount);
        owner.ChangePiety(healAmount);
    }
}
