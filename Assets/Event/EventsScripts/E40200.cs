using UnityEngine;

[CreateAssetMenu(fileName = "E40200", menuName = "Events/고장난 문")]
public class E40200 : Event
{
    void Reset()
    {
        eventID = "E40200";
        eventName = "고장난 문";
        eventDescription = "여느 때와 같이 복도를 거닐던 당신, 발을 동동 구르는 한 수행원과 마주쳤다.\n보아하니 여기 문이 고장 난 거 같은데... 힘 좀 보태 줄까?";
        maxAppear = 1;

        eventWeightBase = 20f;
        eventWeightMultiplier = 0f;

        option1 = "젖 먹던 힘을 다해 당기자!";
        option1Chance = 1f;
        option1Requirement = "체력 6 이상";
        option1SuccessDescription = "역시 몸이 좋으면 머리가 고생을 안 한다.\n\n체력 1 증가!\n정치력 2 감소!";
        option1SuccessResult = "체력 +1\n정치력 -2";

        option2 = "힘이 부족하다.\n다른 수단을 찾아봐야 할 듯 한데....";
        option2Chance = 0.5f;
        option2SuccessDescription = "문이 열렸다. 씨름한 보람이 있네!\n\n정치력 1 감소!";
        option2SuccessResult = "정치력 -1";
        option2FailDescription = "꿈쩍도 안 하네. 애를 쓴 덕분인지 몸이 더 가벼워졌다.\n\n현재 턴 행동 횟수 1회 증가!";
        option2FailResult = "현재 턴 행동 횟수 +1";
    }

    public override bool CanChoiceOption1(Cardinal performer)
    {
        if(performer.Hp < 6f) return false;
        return true;
    }

    public override bool CanChoiceOption2(Cardinal performer)
    {
        return true;
    }

    public override bool OnChoiceOption1(Cardinal performer)
    {
        if(!CanChoiceOption1(performer)) return false;
        performer.ChangeHp(1f);
        performer.ChangeInfluence(-2f);
        return true;
    }

    public override bool OnChoiceOption2(Cardinal performer)
    {
        if(!CanChoiceOption2(performer)) return false;

        if(Random.value <= option2Chance)
        {
            performer.ChangeInfluence(-1f);
            return true;
        }

        InGameManager.Instance.ChangeCurrentTurnActions(1,
            PlayerActionEffectSourceType.Event, eventID, eventName);
        return false;
    }
}
