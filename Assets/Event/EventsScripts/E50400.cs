using UnityEngine;

[CreateAssetMenu(fileName = "E50400", menuName = "Events/삼위일체")]
public class E50400 : Event
{
    void Reset()
    {
        eventID = "E50400";
        eventName = "삼위일체";
        eventDescription = "(후보 n)과(와)의 딱밤 내기에서 패배했다. 손 푸는 꼴을 보아하니 어지간히도 벼르고 있었나 보다....\n\"부자는 망해도 3 대를 버틴다는데, 어디 후보님은 몇 대까지 버티시나 볼까요?\"";
        maxAppear = 3;

        eventWeightBase = 20f;
        eventWeightMultiplier = 0f;

        option1 = "악! 악! 악!";
        option1Chance = 1f;
        option1Requirement = "-";
        option1SuccessDescription = "푸쉬이이.... 이마에 삼위일체가 시뻘겋게 수놓였다.\n태양의 신실한 종 다운 트리플 혹이었다.\n\n체력 33 감소!\n정치력 3 증가!\n경건함 3 증가!";
        option1SuccessResult = "체력 - 33\n정치력 + 3\n경건함 + 3";
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
        performer.ChangeHp(-33f);
        performer.ChangeInfluence(3f);
        performer.ChangePiety(3f);
        return true;
    }

    public override bool OnChoiceOption2(Cardinal performer)
    {
        return true;
    }
}
