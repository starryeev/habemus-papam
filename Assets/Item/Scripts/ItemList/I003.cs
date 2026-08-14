using UnityEngine;

[CreateAssetMenu(fileName = "I003", menuName = "Items/은으로 만든 성배")]
public class I003 : Item
{
    [Header("은으로 만든 성배")]
    [SerializeField] private int hpDelta;
    [SerializeField] private int influenceDelta;

    void Reset()
    {
        itemID = "I003";
        itemGrade = ItemGrade.Common;
        itemExpirationType = ItemExpirationType.Conclave;
        usageType = ItemUsageType.Passive;


        itemName = "은으로 만든 성배";
        itemEffectDescription = "이번 콘클라베 동안 기도 시 <color=#5BD65B>체력</color> 회복량에 <color=#FF4D4D>-1</color>, 연설 시 <color=#4488FF>정치력</color> 획득량에 <color=#66CCFF>+1</color>이 적용된다.";
        itemDescription = "이 잔에 미사를 할 때마다 태양주를 한 잔씩 마실 수 있다. 그것이 미사니까! (끄덕)";
        

        hpDelta = -1;
        influenceDelta = 1;
    }

    public override void OnPray(Cardinal owner)
    {
        float beforeHp = owner.Hp;

        owner.ChangeHp(hpDelta);

       // Debug.Log($"[아이템 효과 발동] 기도: 체력 {beforeHp} -> {owner.Hp} (변화량: {hpDelta})");
    }

    public override void OnSpeech(Cardinal owner)
    {
        float beforeInf = owner.Influence;

        owner.ChangeInfluence(influenceDelta);

        //Debug.Log($"[아이템 효과 발동] 연설: 정치력 {beforeInf} -> {owner.Influence} (변화량: {influenceDelta})");
    }
}
