using UnityEngine;

[CreateAssetMenu(fileName = "E40600", menuName = "Events/운수 좋은 날")]
public class E40600 : Event
{
    void Reset()
    {
        eventID = "E40600";
        eventName = "운수 좋은 날";
        eventDescription = "늦은 오후, 봉사를 마치고 돌아오는 길.\n저 멀리서 해가 지고 있다.";
        maxAppear = 1;

        eventWeightBase = 20f;
        eventWeightMultiplier = 0f;

        option1 = "너무 피곤하다.... 잠시 쉬다 가자.";
        option1Chance = 1f;
        option1Requirement = "체력 3 이하";
        option1SuccessDescription = "네잎 클로버를 발견했다. 무엇이든 해낼 수 있을 것 같은 기분이 든다!\n\n체력 4 증가!\n다음 기도/연설 시 무조건 성공!";
        option1SuccessResult = "체력 +4\n다음 기도/연설 시 무조건 성공";

        option2 = "자리를 뜬다.";
        option2Chance = 1f;
        option2SuccessDescription = "대신전에 도착했다!\n\n아무 일도 일어나지 않았다.";
        option2SuccessResult = "-";
    }

    public override bool CanChoiceOption1(Cardinal performer)
    {
        if(performer.Hp > 3f) return false;
        return true;
    }

    public override bool CanChoiceOption2(Cardinal performer)
    {
        return true;
    }

    public override bool OnChoiceOption1(Cardinal performer)
    {
        if(!CanChoiceOption1(performer)) return false;
        performer.ChangeHp(4f);
        InGameManager.Instance.EventManager.GuaranteeNextPrayerOrSpeech();
        return FinishChoice(1, true);
    }

    public override bool OnChoiceOption2(Cardinal performer)
    {
        if(!CanChoiceOption2(performer)) return false;
        return FinishChoice(2, true);
    }
}
