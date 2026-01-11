using UnityEngine;

[CreateAssetMenu(fileName = "I004", menuName = "Items/은으로 만든 성배")]
public class I004 : Item
{
    [Header("은으로 만든 성배")]
    [SerializeField] private int hpDelta;
    [SerializeField] private int influenceDelta;

    void Reset()
    {
        // 설정 기본값
        itemID = "I004";
        itemGrade = ItemGrade.Common;
        itemExpirationType = ItemExpirationType.Conclave;

        // 텍스트 기본값
        itemName = "은으로 만든 성배";
        itemDescription = "이 잔에 미사를 할 때마다 태양주를 한 잔씩 마실 수 있다. 그것이 미사니까! (끄덕)";
        

        // 수치 기본값
        hpDelta = -5;
        influenceDelta = 5;
    }

    public override void OnPray(Cardinal owner)
    {
        owner.ChangeHp(hpDelta);
        owner.ChangePiety(influenceDelta);
    }
}
