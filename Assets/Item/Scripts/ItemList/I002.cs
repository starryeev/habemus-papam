using UnityEngine;

[CreateAssetMenu(fileName = "I002", menuName = "Items/나무지팡이")]
public class I002 : Item
{
    [Header("나무지팡이 설정")]
    [Tooltip("사용 시 감소시킬 체력")]
    [SerializeField] private int damageAmount = 4;

    void Reset()
    {
        itemID = "I002";
        itemGrade = ItemGrade.Rare; 
        itemExpirationType = ItemExpirationType.Permanent;
        usageType = ItemUsageType.Active;

        itemName = "나무지팡이";
        itemDescription = "걷기가 편해진다. 마음에 안 드는 사람을 위협할 수도 있다!";
        itemEffectDescription = "획득 시 다음 턴 행동 횟수 1회 증가. 사용 시 체력이 가장 낮은 후보(NPC)의 체력 4 감소.";

        damageAmount = 4;
    }

    public override void OnAcquire()
    {
        Cardinal player = FindPlayer();
        if (player != null && InGameManager.Instance != null)
        {
            InGameManager.Instance.QueueNextTurnActionDelta(1);
        }
    }

    public override void OnReapply(Cardinal owner)
    {
        if (owner != null && InGameManager.Instance != null)
        {
            InGameManager.Instance.QueueNextTurnActionDelta(1);
        }
    }

    public override void OnRemove()
    {
        // 예약된 행동 보너스는 다른 효과와 섞일 수 있어 제거 시 역보정을 하지 않는다.
    }

    public override void OnUse()
    {
        Cardinal target = FindWeakestNPC();

        if (target != null)
        {
            float beforeHp = target.Hp;
            target.ChangeHp(-damageAmount); 
            
        }
        else
        {
            Debug.Log("[아이템 사용 실패] 공격할 대상이 없습니다.");
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

    private Cardinal FindPlayer()
    {
        if (InventoryManager.Instance != null) return InventoryManager.Instance.Player;
        return null;
    }
}
