using UnityEngine;

[CreateAssetMenu(fileName = "E50000", menuName = "Events/열렬한 찬양")]
public class E50000 : Event
{
    void Reset()
    {
        eventID = "E50000";
        eventName = "열렬한 찬양";
        eventDescription = "(후보 n)이(가) 오늘부로 당신을 향한 지지를 철회했다.\n애초에 지지를 하긴 했었냐는 나쁜 말을 한 추기경은 곧 사라졌다. 실로 기적이었다.\n아무튼 (후보 n)은(는) 당신과의 지지 관계에서 벗어나 한 몸으로 일체가 되길 원한다!";
        maxAppear = 3;

        eventWeightBase = 20f;
        eventWeightMultiplier = 0f;

        option1 = "이게 대체 무슨 일이람!";
        option1Chance = 1f;
        option1Requirement = "-";
        option1SuccessDescription = "\"세상에 70억명의 팬이 있다면....\"\n영문 모를 소리에 멍해진 당신은 어느새 (후보 n)와(과) 악수를 하고 있었다.\n추기경들의 박수 소리가 들린다. 당신의 명망이 더욱 높아졌다!\n\n경건함 30 증가!";
        option1SuccessResult = "경건함 +30";
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
            performer.ChangePiety(30f);
            return true;
        }

        return false;
    }

    public override bool OnChoiceOption2(Cardinal performer)
    {
        return true;
    }
}
