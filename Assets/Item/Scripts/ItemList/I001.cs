using UnityEngine;

[CreateAssetMenu(fileName = "I001", menuName = "Items/아령")]
public class I001 : Item
{
    [System.Serializable]
    private class RuntimeState
    {
        public int heldTurns;
    }

    [Header("아령 설정")]
    [SerializeField] private int healPerHeldTurn = 1;
    private int heldTurns;
    private GameContext subscribedContext;

    // 초기화
    void Reset()
    {
        itemID = "I001";
        itemGrade = ItemGrade.Common;
        itemExpirationType = ItemExpirationType.Permanent;
        usageType = ItemUsageType.Active;

        itemName = "묵직한 아령";
        itemEffectDescription = "소지 중에는 기도로 <color=#5BD65B>체력</color>을 회복할 수 없다. 사용 시 보유했던 턴 수만큼 <color=#5BD65B>체력</color>을 회복한다.";
        healPerHeldTurn = 1;
    }

    public override void OnAcquire()
    {
        heldTurns = Mathf.Max(1, heldTurns);
        SubscribeToTurns();
        Debug.Log("[아이템] 아령 획득: 기도 회복 차단 시작");
    }

    public override void OnReapply(Cardinal owner)
    {
        SubscribeToTurns();
    }

    public override void OnRemove()
    {
        UnsubscribeFromTurns();
        Debug.Log("[아이템] 아령 제거: 기도 회복 차단 해제");
    }

    public override void OnUse()
    {
        Cardinal player = FindPlayer();
        if (player != null)
        {
            player.ChangeHp(heldTurns * healPerHeldTurn);
        }
        UnsubscribeFromTurns();
    }

    private void SubscribeToTurns()
    {
        GameContext context = InGameManager.Instance != null ? InGameManager.Instance.Context : null;
        if (context == null || context == subscribedContext) return;
        UnsubscribeFromTurns();
        subscribedContext = context;
        subscribedContext.OnGameContextEvent += HandleContextEvent;
    }

    private void UnsubscribeFromTurns()
    {
        if (subscribedContext == null) return;
        subscribedContext.OnGameContextEvent -= HandleContextEvent;
        subscribedContext = null;
    }

    private void HandleContextEvent(GameContext.GameContextEvent eventType)
    {
        if (eventType == GameContext.GameContextEvent.TurnStart) heldTurns++;
    }

    private Cardinal FindPlayer()
    {
        if (InventoryManager.Instance != null) return InventoryManager.Instance.Player;
        return null;
    }

    public override void ResetRuntimeState()
    {
        UnsubscribeFromTurns();
        heldTurns = 0;
    }

    public override string CaptureRuntimeState()
    {
        RuntimeState state = new RuntimeState
        {
            heldTurns = heldTurns
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
            heldTurns = Mathf.Max(1, state.heldTurns);
        }
    }
}
