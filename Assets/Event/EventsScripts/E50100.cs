using UnityEngine;

[CreateAssetMenu(fileName = "E50100", menuName = "Events/커피 회동")]
public class E50100 : Event
{
    void Reset()
    {
        eventID = "E50100";
        eventName = "커피 회동";
        eventDescription = "(후보 n)이(가) 커피를 대접해준다하자, 당신은 기쁜 마음으로 이를 받아들였다.\n기쁜 마음은 오래 가지 못했다. (후보 n)이(가) 넘어지며 주인 잃은 커피가 당신을 덮쳐 왔기 때문이다!\n비상하는 커피 사이로 스치듯 올라가는 입꼬리를 보았지만..., 지금은 먼저 피할 때다. ";
        maxAppear = 3;

        eventWeightBase = 20f;
        eventWeightMultiplier = 0f;

        option1 = "피하자!";
        option1Chance = 0.5f;
        option1Requirement = "-";
        option1SuccessDescription = "저런, 커피에 온 신경을 집중한 나머지 옆에 기둥이 있는 지도 몰랐다.\n꽝! 잠시 정신이 아득해졌다.\n\n다음 턴 행동 횟수 1회 감소!";
        option1SuccessResult = "다음 턴 행동 횟수 -1";
        option1FailDescription = "앗, 뜨거! 결국 피하지 못하고 커피를 뒤집어썼다.\n\n체력 25 감소!";
        option1FailResult = "체력 - 25";
        option2 = "";

    }

    public override bool CanChoiceOption1(Cardinal performer)
    {
        return true;
    }

    public override bool CanChoiceOption2(Cardinal performer)
    {
        return true;
    }

    public override bool OnChoiceOption1(Cardinal performer)
    {
        if(!CanChoiceOption1(performer)) return false;

        if(Random.value <= option1Chance)
        {
            InGameManager.Instance.QueueNextTurnActionDelta(-1);
            return true;
        }

        performer.ChangeHp(-3f);
        return false;
    }

    public override bool OnChoiceOption2(Cardinal performer)
    {
        return true;
    }

}
