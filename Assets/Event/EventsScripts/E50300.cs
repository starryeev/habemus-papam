using UnityEngine;

[CreateAssetMenu(fileName = "E50300", menuName = "Events/심도 있는 논의")]
public class E50300 : Event
{
    void Reset()
    {
        eventID = "E50300";
        eventName = "심도 있는 논의";
        eventDescription = "토론이 한창 진행되던 중, (후보 n)에게서 곤란한 질문이 날아왔다.\n\"과거 장기자랑에서 문워크를 추셨는데, 이거 뭡니까? 혹시 달 추종자세요?\"";
        maxAppear = 3;

        eventWeightBase = 10f;
        eventWeightMultiplier = 0.1f;

        option1 = "아니요. 그런 게 아니라....";
        option1Chance = 1f;
        option1Requirement = "-";
        option1SuccessDescription = "궁지에 몰린 당신! 그때 당신을 위해 몇몇 추기경이 나섰다!\n그야말로 '신들린' 세이브였다!\n\n정치력 15 증가";
        option1SuccessResult = "정치력 +15";
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
        performer.ChangeInfluence(15f);
        return true;
    }

    public override bool OnChoiceOption2(Cardinal performer)
    {
        return true;
    }
}
