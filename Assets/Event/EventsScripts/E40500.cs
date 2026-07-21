using UnityEngine;

[CreateAssetMenu(fileName = "E40500", menuName = "Events/주화입마")]
public class E40500 : Event
{
    void Reset()
    {
        eventID = "E40500";
        eventName = "주화입마";
        eventDescription = "아침부터 어떤 생각이 당신을 괴롭히고, 또 시시각각 마음을 조여오고 있다.\n'...집에 가스 불 끄고 왔었나?'\n이 불안감을 해소하기 전까진 아무것도 할 수 없을 것 같다!";
        maxAppear = 1;

        eventWeightBase = 20f;
        eventWeightMultiplier = 0f;

        option1 = "집으로 수행원을 보낸다.";
        option1Chance = 0.8f;
        option1Requirement = "정치력 40 이상";
        option1SuccessDescription = "진짜 불이 켜져 있었다!\n휴, 하마터면 집 통째로 태양께 봉헌할 뻔했네.\n\n정치력 15 감소!\n경건함 10 감소!";
        option1SuccessResult = "정치력 - 15\n경건함 - 10";
        option1FailDescription = "걱정은 기우였다.\n무안해진 당신은 다음에 수행원에게 밥 한 끼 사주리라 다짐했다!\n\n아무 일도 일어나지 않았다.";
        option1FailResult = "-";

        option2 = "뭐 손 쓸 수단이 없다.\n이 일을 마치고 빨리 가보는 수밖에.";
        option2Chance = 0.8f;
        option2SuccessDescription = "와장창!\n집중력이 흐트러진 나머지 실수를 해버렸다!\n\n이번 썬ㅡ클라베에서 기도/연설 시간 대폭 증가!";
        option2SuccessResult = "기도/연설 이후 남은 시간 5초 추가 감소";
        option2FailDescription = "어지러웠지만 신심으로 마음을 가라앉혔다.\n당신의 정신력은 많은 추기경들의 귀감이 되었다!\n\n정치력 5 증가!";
        option2FailResult = "정치력 + 5";
    }

    public override bool CanChoiceOption1(Cardinal performer)
    {
        if(performer.Influence < 40f) return false;
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
            performer.ChangeInfluence(-15f);
            performer.ChangePiety(-10f);
            return true;
        }

        return false;
    }

    public override bool OnChoiceOption2(Cardinal performer)
    {
        if(!CanChoiceOption2(performer)) return false;

        if(Random.value <= option2Chance)
        {
            // 이번 썬ㅡ클라베에서 기도/연설 이후 남은 시간 5초 추가 감소 처리 필요
            return true;
        }

        performer.ChangeInfluence(5f);
        return false;
    }
}
