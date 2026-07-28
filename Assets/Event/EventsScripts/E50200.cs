using UnityEngine;

[CreateAssetMenu(fileName = "E50200", menuName = "Events/목 좀 축이세요")]
public class E50200 : Event
{
    void Reset()
    {
        eventID = "E50200";
        eventName = "목 좀 축이세요";
        eventDescription = "(후보 n)를 지지하는 추기경이 나에게 주스 한 잔을 권한다. 마침 목이 너무 말랐는데!\n싱그러운 과일 향이 나는 진짜 과일 주스다! 게임 개발자의 양심을 걸고 독약 같은 건 절대 아니다.";
        maxAppear = 3;

        eventWeightBase = 0f;
        eventWeightMultiplier = 0.2f;

        option1 = "주스를 마신다.";
        option1Chance = 0.5f;
        option1Requirement = "-";
        option1SuccessDescription = "푸룬 주스였다! 남은 일을 해치우려 했지만 배가 심상치 않다.\n\n체력 1 감소!\n다음 턴 행동 횟수 1회 감소!";
        option1SuccessResult = "체력 -1\n다음 턴 행동 횟수 -1";
        option1FailDescription = "푸룬 주스였다! 당신은 뱃속이 심상치 않음을 느끼고 화장실로 직행한다.\n아무튼 거짓말은 하지 않았다.\n\n이번 썬ㅡ클라베 즉시 종료!\n체력 10 증가!";
        option1FailResult = "체력 + 10\n콘클라베 즉시 종료, 교주 판정으로 직행";
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
            performer.ChangeHp(-1f);
            // 이벤트 단계에서는 현재 턴 행동이 끝난 상태라 다음 플레이 가능 턴에 적용한다.
            InGameManager.Instance.QueueNextTurnActionDelta(-1);
            return FinishChoice(1, true);
        }

        performer.ChangeHp(1f);
        InGameManager.Instance.EndCurrentConclave();
        return FinishChoice(1, false);
    }

    public override bool OnChoiceOption2(Cardinal performer)
    {
        return true;
    }

}
