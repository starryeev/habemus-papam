using UnityEngine;

[CreateAssetMenu(fileName = "I009", menuName = "Items/게르마늄 태양 팔찌")]
public class I009 : Item
{
    [System.Serializable]
    private class RuntimeState
    {
        public bool hasUsedRevive;
    }

    [Header("게르마늄 태양 팔찌 설정")]
    [Tooltip("기도 시 추가 경건함")]
    [SerializeField] private float pietyBonus;

    [Tooltip("기절 방어 시 회복할 체력")]
    [SerializeField] private float reviveHpAmount;

    // 1회 방어를 체크하기 위한 변수
    private bool hasUsedRevive = false;

    void Reset()
    {
        itemID = "I009";
        itemName = "게르마늄 태양 팔찌";
        itemDescription = "태양의 에너지 덕분인지 게르마늄의 원적외선 덕분인지, 몸이 홀가분해졌다!";
        itemEffectDescription = "기도 시 <color=#FFD84D>경건함</color>을 <color=#66CCFF>+1</color> 추가 획득한다. <color=#5BD65B>체력</color>이 0이 될 때 한 번 기절을 막고 <color=#5BD65B>체력</color>을 <color=#66CCFF>+3</color> 회복한다.";

        itemGrade = ItemGrade.Rare;
        itemExpirationType = ItemExpirationType.Permanent;
        usageType = ItemUsageType.Passive;

        pietyBonus = 1f;
        reviveHpAmount = 3f;
    }

    // 아이템을 처음 획득할 때 1회 방어 기회 활성화
    public override void OnAcquire()
    {
        hasUsedRevive = false;
    }

    public override void OnPray(Cardinal owner)
    {
        float beforePiety = owner.Piety;
        owner.ChangePiety(pietyBonus);
        Debug.Log($"[아이템 효과] 게르마늄 팔찌: 경건함 {beforePiety} -> {owner.Piety}");
    }

    public override float PreviewPrayerPiety(float originalDelta, GameBalance balance, bool isSuccess)
    {
        return originalDelta + pietyBonus;
    }

    public override bool OnHpReachedZero(Cardinal owner)
    {
        if (!hasUsedRevive)
        {
            hasUsedRevive = true; 

            owner.ChangeHp(reviveHpAmount); // 체력 30 회복

            Debug.Log($"[아이템 효과 발동!!] 게르마늄의 힘으로 기절을 극복했습니다! 체력 회복: {reviveHpAmount}");

            return true; 
        }

        return false; 
    }

    public override void ResetRuntimeState()
    {
        hasUsedRevive = false;
    }

    public override string CaptureRuntimeState()
    {
        RuntimeState state = new RuntimeState
        {
            hasUsedRevive = hasUsedRevive
        };

        return JsonUtility.ToJson(state);
    }

    public override void RestoreRuntimeState(string runtimeStateJson)
    {
        ResetRuntimeState();

        if (string.IsNullOrWhiteSpace(runtimeStateJson))
        {
            return;
        }

        RuntimeState state = JsonUtility.FromJson<RuntimeState>(runtimeStateJson);
        if (state != null)
        {
            hasUsedRevive = state.hasUsedRevive;
        }
    }
}
