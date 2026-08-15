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
        option1Requirement = "정치력 4 이상";
        option1SuccessDescription = "진짜 불이 켜져 있었다! 하마터면 집을 통째로 태양께 봉헌할 뻔했다.\n\n정치력 2 감소!\n경건함 1 감소!";
        option1SuccessResult = "정치력 -2\n경건함 -1";
        option1FailDescription = "걱정은 기우였다.\n무안해진 당신은 다음에 수행원에게 밥 한 끼 사주리라 다짐했다!\n\n아무 일도 일어나지 않았다.";
        option1FailResult = "-";

        option2 = "뭐 손 쓸 수단이 없다.\n이 일을 마치고 빨리 가보는 수밖에.";
        option2Chance = 0.8f;
        option2SuccessDescription = "와장창!\n집중력이 흐트러진 나머지 실수를 해버렸다!\n\n다음 턴 행동 횟수 1회 증가!";
        option2SuccessResult = "다음 턴 행동 횟수 +1";
        option2FailDescription = "어지러웠지만 신심으로 마음을 가라앉혔다. 당신의 정신력은 많은 추기경의 귀감이 되었다.\n\n정치력 1 증가!";
        option2FailResult = "정치력 +1";
    }

    public override bool CanChoiceOption1(Cardinal performer)
    {
        if(performer.Influence < 4f) return false;
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
            performer.ChangeInfluence(-2f);
            performer.ChangePiety(-1f);
            return true;
        }

        return false;
    }

    public override bool OnChoiceOption2(Cardinal performer)
    {
        if(!CanChoiceOption2(performer)) return false;

        if(Random.value <= option2Chance)
        {
            // 이벤트 단계에서는 현재 턴의 행동이 이미 끝났으므로 다음 플레이 가능 턴에 이월한다.
            InGameManager.Instance.QueueNextTurnActionDelta(1);
            return true;
        }

        performer.ChangeInfluence(1f);
        return false;
    }

}
