using UnityEngine;

[CreateAssetMenu(fileName = "E50500", menuName = "Events/이 불경한 자가")]
public class E50500 : Event
{
    void Reset()
    {
        eventID = "E50500";
        eventName = "이 불경한 자가";
        eventDescription = "(후보 n)이(가) 당신의 경전 해석 방식에 심대하고도 지대하며 말로 이루 표현할 수 없는 심각한 문제가 있음을 지적했다.\n그게 무슨 강아지 풀 뜯어먹는 소리냐고 대꾸하기도 전에....";
        maxAppear = 1;

        eventWeightBase = 20f;
        eventWeightMultiplier = 0f;

        option1 = "깡!";
        option1Chance = 1f;
        option1Requirement = "-";
        option1SuccessDescription = "불과 5분만에 이단 신문의 모든 과정이 전개됐다.\n혀를 내두를만한 진행 속도다. 그 대상이 당신만 아니었다면 말이다!\n\n체력 40 감소!";
        option1SuccessResult = "체력 - 40";
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
        performer.ChangeHp(-40f);
        return true;
    }

    public override bool OnChoiceOption2(Cardinal performer)
    {
        return true;
    }
}
