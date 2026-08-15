using UnityEngine;

[CreateAssetMenu(fileName = "E32002", menuName = "Events/이교도와 성전2")]
public class E32002 : Event
{
    void Reset()
    {
        eventID = "E32002";
        eventName = "이교도와 성전2";
        eventDescription = "대신전 폭탄 테러 미수 사건으로 썬ㅡ클라베는 큰 혼란에 휩싸였다. 강경파인 (후보 1)은(는) 성전을 주장하고, 신중파인 (후보 3)은(는) 외교를 주장한다. 어느 쪽의 손을 들어줄까?";
        maxAppear = 1;

        eventWeightBase = Mathf.Infinity;
        eventWeightMultiplier = 0f;

        // 선행 이벤트: E32000
        // 기피 이벤트: E32001
        // 현재 이벤트 스크립트만으로는 preEvents/conflictEvents 에셋 연결 필요

        option1 = "(후보 1)을(를) 지지한다.";
        option1Chance = 1f;
        option1SuccessDescription = "당신은 (후보 1)의 지지자를 자처하며 성전을 일으킬 것을 천명하였다.\n\n경건함 1 감소!\n게임 종료!\n\"성전\" 엔딩";
        option1SuccessResult = "경건함 -1\n게임 종료\n\"성전\" 엔딩";

        option2 = "(후보 3)을(를) 지지한다!";
        option2Chance = 1f;
        option2SuccessDescription = "당신은 (후보 3)의 지지자를 자처하며 외교로 평화를 지킬 것을 선언하였다.\n\n정치력 1 증가!\n게임 종료!\n\"외교 승리\" 엔딩";
        option2SuccessResult = "정치력 +1\n게임 종료\n\"외교 승리\" 엔딩";
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
        {  // 성공했을 때 로직
            performer.ChangePiety(-1f);

            return FinishChoiceWithEnding(1, EndingType.Crusade);
        }
        else
        {  // 실패했을 때 로직
            

            return FinishChoice(1, false);
        }
    }


    public override bool OnChoiceOption2(Cardinal performer)
    {
        if(!CanChoiceOption2(performer)) return false;

        if(Random.value <= option2Chance)
        {  // 성공했을 때 로직
            performer.ChangeInfluence(1f);

            return FinishChoiceWithEnding(2, EndingType.DiplomaticVictory);
        }
        else
        {  // 실패했을 때 로직
            

            return FinishChoice(2, false);
        }
    }
}
