using UnityEngine;

[CreateAssetMenu(fileName = "I007", menuName = "Items/태양의 은총")]
public class I007 : Item
{
    [Header("태양의 은총 설정")]
    [Tooltip("처음 공작 사용 시 추가로 획득할 정치력")]
    [SerializeField] private float plotInfluenceBonus = 1f;
    [SerializeField] private float plotHpBonus = 1f;

    void Reset()
    {
        itemID = "I007";
        itemGrade = ItemGrade.Rare;
        itemExpirationType = ItemExpirationType.Day;
        usageType = ItemUsageType.Passive;

        itemName = "태양의 은총";
        itemDescription = "은으로 만든 총이다. 발사 기능은 없지만 뇌물로서의 가치는 뛰어나다.";

        plotInfluenceBonus = 1f;
        plotHpBonus = 1f;
        itemEffectDescription = "3일 동안 공작마다<color=#5BD65B>체력</color>과 <color=#4488FF>정치력</color> <color=#66CCFF>+1</color> 추가 획득. 4일째에 이 아이템을 소지했다면, <color=#4488FF>정치력</color>을 <color=#FF4D4D>0</color>으로 초기화";
    }

    public override void OnPlot(Cardinal owner)
    {
        if (owner == null)
        {
            return;
        }

        owner.ChangeHp(plotHpBonus);
        owner.ChangeInfluence(plotInfluenceBonus);
    }

    public override void OnExpiration()
    {
        Cardinal player = FindPlayer();
        if (player == null)
        {
            return;
        }

        player.ChangeInfluence(-player.Influence);
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
