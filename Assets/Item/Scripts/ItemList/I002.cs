using UnityEngine;

[CreateAssetMenu(fileName = "I002", menuName = "Items/나무지팡이")]
public class I002 : Item
{
    [Header("나무지팡이 설정")]
    [Tooltip("사용 시 감소시킬 체력")]
    [SerializeField] private int damageAmount = 4;
    private GameContext subscribedContext;

    void Reset()
    {
        itemID = "I002";
        itemGrade = ItemGrade.Rare; 
        itemExpirationType = ItemExpirationType.Permanent;
        usageType = ItemUsageType.Active;

        itemName = "나무지팡이";
        itemDescription = "걷기가 편해진다. 마음에 안 드는 사람을 위협할 수도 있다!";
        itemEffectDescription = "소지 시, 턴당 행동 횟수 <color=#66CCFF>+1</color>회 증가. 사용 시, <color=#5BD65B>체력</color>이 가장 낮은 후보 NPC의 <color=#5BD65B>체력</color> <color=#FF4D4D>-4</color> 감소";

        damageAmount = 4;
    }

    public override void OnAcquire()
    {
        ApplyActionBonus();
        SubscribeToTurns();
    }

    public override void OnReapply(Cardinal owner)
    {
        SubscribeToTurns();
    }

    public override void OnRemove()
    {
        UnsubscribeFromTurns();
    }

    public override void ResetRuntimeState()
    {
        UnsubscribeFromTurns();
    }

    public override void OnUse()
    {
        Cardinal target = FindWeakestNPC();

        if (target != null)
        {
            float beforeHp = target.Hp;
            target.ChangeHp(-damageAmount); 
            
        }
    }

    private Cardinal FindWeakestNPC()
    {
        StatsUI statsUI = FindAnyObjectByType<StatsUI>();
        if (statsUI == null) return null;

        Cardinal[] cardinals = statsUI.LinkedCardinals;
        if (cardinals == null) return null;

        Cardinal weakestTarget = null;
        float minHp = float.MaxValue;

        foreach (var c in cardinals)
        {
            if (c == null || !c.gameObject.activeSelf || c.CompareTag("Player")) continue;

            if (c.Hp < minHp)
            {
                minHp = c.Hp;
                weakestTarget = c;
            }
        }

        return weakestTarget;
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
        if (eventType == GameContext.GameContextEvent.TurnStart)
        {
            ApplyActionBonus();
        }
    }

    private void ApplyActionBonus()
    {
        if (InGameManager.Instance == null) return;

        InGameManager.Instance.ChangeCurrentTurnActions(1,
            PlayerActionEffectSourceType.Item, itemID, itemName);
    }

}
