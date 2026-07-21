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
        option1Requirement = "체력 60 이상";
        option1SuccessDescription = "역시 몸이 좋으면 머리가 고생을 안 한다.\n\n체력 5 증가!\n정치력 15 감소!";
        option1SuccessResult = "체력 + 5\n정치력 - 15";

        option2 = "힘이 부족하다.\n다른 수단을 찾아봐야 할 듯 한데....";
        option2Chance = 0.5f;
        option2SuccessDescription = "문이 열렸다. 씨름한 보람이 있네!\n\n정치력 5 감소!";
        option2SuccessResult = "정치력 - 5";
        option2FailDescription = "꿈쩍도 안 하네. 괜히 시간만 버렸다.\n\n6초간 행동 불가!";
        option2FailResult = "행동 불가 6초";
    }

    public override bool CanChoiceOption1(Cardinal performer)
    {
        if(performer.Hp < 60f) return false;
        return true;
    }

    public override bool CanChoiceOption2(Cardinal performer)
    {
        return true;
    }

    public override bool OnChoiceOption1(Cardinal performer)
    {
        if(!CanChoiceOption1(performer)) return false;
        performer.ChangeHp(5f);
        performer.ChangeInfluence(-15f);
        return true;
    }

    public override bool OnChoiceOption2(Cardinal performer)
    {
        if(!CanChoiceOption2(performer)) return false;

        if(Random.value <= option2Chance)
        {
            performer.ChangeInfluence(-5f);
            return true;
        }

        StateController stateController = performer.GetComponent<StateController>();
        if(stateController != null) stateController.ApplyStun(6f);
        return false;
    }
}
