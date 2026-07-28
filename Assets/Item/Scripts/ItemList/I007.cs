using UnityEngine;

[CreateAssetMenu(fileName = "I007", menuName = "Items/태양의 은총")]
public class I007 : Item
{
    [System.Serializable]
    private class RuntimeState
    {
        public bool hasTriggeredToday;
    }

    [Header("태양의 은총 설정")]
    [Tooltip("처음 공작 사용 시 추가로 획득할 정치력")]
    [SerializeField] private float plotInfluenceBonus = 2f;

    private bool hasTriggeredToday;

    void Reset()
    {
        itemID = "I007";
        itemGrade = ItemGrade.Rare;
        itemExpirationType = ItemExpirationType.Day;
        usageType = ItemUsageType.Passive;

        itemName = "태양의 은총";
        itemDescription = "은으로 만든 총이다. 발사 기능은 없지만 뇌물로서의 가치는 뛰어나다.";
        itemEffectDescription = "오늘 공작 시 정치력 20 증가, 내일 0으로 초기화";

        plotInfluenceBonus = 2f;
    }

    public override void OnAcquire()
    {
        hasTriggeredToday = false;
    }

    public override void OnPlot(Cardinal owner)
    {
        if (owner == null || hasTriggeredToday)
        {
            return;
        }

        owner.ChangeInfluence(plotInfluenceBonus);
        hasTriggeredToday = true;

        Debug.Log($"[아이템 효과] 태양의 은총: 첫 공작 보너스 정치력 +{plotInfluenceBonus}");
    }

    public override void OnRemove()
    {
        if (!hasTriggeredToday)
        {
            return;
        }

        Cardinal player = FindPlayer();
        if (player == null)
        {
            return;
        }

        player.ChangeInfluence(-player.Influence);
        hasTriggeredToday = false;

        Debug.Log("[아이템 효과] 태양의 은총 종료: 정치력을 0으로 초기화");
    }

    public override void ResetRuntimeState()
    {
        hasTriggeredToday = false;
    }

    public override string CaptureRuntimeState()
    {
        RuntimeState state = new RuntimeState
        {
            hasTriggeredToday = hasTriggeredToday
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
            hasTriggeredToday = state.hasTriggeredToday;
        }
    }

    private Cardinal FindPlayer()
    {
        if (InventoryManager.Instance != null)
        {
            return InventoryManager.Instance.Player;
        }

        return null;
    }
}
