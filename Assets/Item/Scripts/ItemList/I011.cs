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
        itemName = "교황청 비밀문서";
        itemDescription = "이걸 폭로해서 모두의 눈길을 돌리면 자연스럽게 교황 자리를 피할 수 있다!";
        itemEffectDescription = "사용한 콘클라베 동안 연설 시 정치력 대신 체력과 경건함을 10씩 회복";

        itemGrade = ItemGrade.Rare; // 고급
        itemExpirationType = ItemExpirationType.Conclave; // 콘클라베 종료 시 소멸
        usageType = ItemUsageType.Active; // 사용해야 발동

        healAmount = 1f;
    }

    // 1. 인벤토리에서 클릭하여 사용했을 때
    public override void OnUse()
    {
        // (InventoryManager가 가상 인벤토리로 옮겨주는 처리를 알아서 해줍니다.)
        Debug.Log("[아이템 사용] 교황청 비밀문서를 품에 품었습니다. 이번 콘클라베 연설에서 엄청난 폭로가 시작됩니다!");
    }

    // 2. 가상 인벤토리(버프)에 있는 동안: 연설 정치력 변화량 가로채기
    public override float ModifySpeechInfluence(float originalDelta, GameBalance balance, bool isSuccess)
    {
        // "정치력 대신" 이므로 원래 증가/감소할 정치력을 0으로 만듭니다.
        Debug.Log($"[아이템 개입] 비밀문서 폭로! 원래의 정치력 변화({originalDelta})를 무효화(0) 합니다.");
        return 0f;
    }

    // 3. 가상 인벤토리(버프)에 있는 동안: 연설이 끝난 직후 체력과 경건함 회복
    public override void OnSpeech(Cardinal owner)
    {
        float beforeHp = owner.Hp;
        float beforePiety = owner.Piety;

        // 체력과 경건함을 동시에 회복
        owner.ChangeHp(healAmount);
        owner.ChangePiety(healAmount);

        Debug.Log($"[아이템 효과] 비밀문서 폭로 성공! 체력 {beforeHp}->{owner.Hp} (+{healAmount}), 경건함 {beforePiety}->{owner.Piety} (+{healAmount})");
    }
}
