using UnityEngine;

[CreateAssetMenu(fileName = "I004", menuName = "Items/금으로 만든 성배")]
public class I004 : Item
{
    [Header("금으로 만든 성배 설정")]
    [Tooltip("연설 시 추가로 획득할 정치력")]
    [SerializeField] private int influenceDelta;

    void Reset()
    {
        itemID = "I004";
        itemGrade = ItemGrade.Common;
        itemExpirationType = ItemExpirationType.Conclave; 
        usageType = ItemUsageType.Passive;

        itemName = "금으로 만든 성배";
        itemDescription = "이 잔으로 미사를 드리자 장로들의 눈길이 쏠린다. 럭셔리하니까!";
        itemEffectDescription = "오늘 연설 시 <color=#4488FF>정치력</color> 획득량 <color=#66CCFF>+2</color> 증가";

        influenceDelta = 2;
    }

    public override void OnSpeech(Cardinal owner)
    {
        // 정치력 증가 적용
        owner.ChangeInfluence(influenceDelta);
    }

    public override float PreviewSpeechInfluenceAfterAction(float originalDelta, GameBalance balance, bool isSuccess)
    {
        return originalDelta + influenceDelta;
    }
}
