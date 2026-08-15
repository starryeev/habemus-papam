using UnityEngine;

[CreateAssetMenu(fileName = "E40000", menuName = "Events/산책")]
public class E40000 : Event
{
    void Reset()
    {
        eventID = "E40000";
        eventName = "산책";
        eventDescription = "새들은 지저귀고, 꽃들은 피어나고... 산책 나가기 딱 좋은 날이다.\n그런데 잠깐! 개울을 넘기 위해 징검다리로 가니 한 남자가 화들짝 놀라며 급하게 자리를 떴다!\n쭈그려 앉아 무언가를 하던데... 과연 건너는게 맞을까?";
        maxAppear = 1;

        eventWeightBase = 20f;
        eventWeightMultiplier = 0f;

        option1 = "흠... 아무리 봐도 수상하다.\n주위를  살피며 조심히 돌아서 가자.";
        option1Chance = 1f;
        option1Requirement = "경건함 5 이상";
        option1SuccessDescription = "오늘도 무사히 산책을 마쳤다. 이 또한 태양신의 은혜겠지요.\n\n체력 2 증가!\n경건함 1 증가!";
        option1SuccessResult = "체력 +2\n경건함 +1";
        option1FailDescription = "-";
        option1FailResult = "-";

        option2 = "돌다리는 나약한 자나 두들겨 보는 것이다.\n그냥 가자.";
        option2Chance = 1f;
        option2SuccessDescription = "미끄러져 물에 빠지고 몸이 무거워졌다.\n\n체력 1 감소!\n다음 턴 행동 횟수 1회 감소!";
        option2SuccessResult = "체력 -1\n다음 턴 행동 횟수 -1";
    }

    public override bool CanChoiceOption1(Cardinal performer)
    {
        if(performer.Piety < 5f) return false;
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
            performer.ChangeHp(2f);
            performer.ChangePiety(1f);
            return true;
        }

        return false;
    }

    public override bool OnChoiceOption2(Cardinal performer)
    {
        if(!CanChoiceOption2(performer)) return false;

        if(Random.value <= option2Chance)
        {
            performer.ChangeHp(-1f);
            InGameManager.Instance.QueueNextTurnActionDelta(-1);
            return true;
        }

        return false;
    }

}
