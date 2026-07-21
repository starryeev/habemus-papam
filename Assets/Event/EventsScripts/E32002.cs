using UnityEngine;

[CreateAssetMenu(fileName = "E32002", menuName = "Events/이교도와 성전2")]
public class E32002 : Event
{
    void Reset()
    {
        eventID = "E32002";
        eventName = "이교도와 성전2";
        eventDescription = "대신전 폭탄 테러 미수 사건으로 썬ㅡ클라베는 큰 혼란에 휩싸였다.\n이교도들의 교주는, 해당 폭탄테러는 교단과 관계없다며 빠르게 사과의 사절을 보내 왔다.\n강경파인 (후보 1)은(는) 이백 년 동안의 평화를 끝내고 성전을 일으킬 것을 건의하는 반면,\n신중파인 (후보 3)은(는) 교주와의 회담에 응하고 외교에서 유리한 위치를 점할 것을 주장한다.\n누구를 견제할지는 당신의 손에 달렸다!\n\n퀘스트 : 지지하지 않는 후보를 탈락시킨 후 지지하는 후보를 당선시키세요.\n성공 시 다른 엔딩을 볼 수 있습니다.";
        maxAppear = 1;

        eventWeightBase = Mathf.Infinity;
        eventWeightMultiplier = 0f;

        // 선행 이벤트: E32000
        // 기피 이벤트: E32001
        // 현재 이벤트 스크립트만으로는 preEvents/conflictEvents 에셋 연결 필요

        option1 = "(후보 1)을(를) 지지한다.";
        option1Chance = 1f;
        option1SuccessDescription = "당신은 (후보 1)의 지지자를 자처하며, 성전을 일으킬 것을 천명하였다.\n태양의 앞길을 막은 자들은 모두 태양에 의해 불살라질 것이다!\n\n경건함 10 감소!\n연설 시마다 (후보 1)의 정치력 5 증가!\n\n퀘스트 : (후보 3)을(를) 탈락시킨 후 (후보 1)을(를) 당선시키세요.\n성공 시 다른 엔딩을 볼 수 있습니다.";
        option1SuccessResult = "경건함 10 감소\n연설 시 후보 1 정치력 5 증가\n후보 3이 탈락한 상태에서 후보 1 당선 시 \"성전\" 엔딩";

        option2 = "(후보 3)을(를) 지지한다!";
        option2Chance = 1f;
        option2SuccessDescription = "태양은 모두를 굽어살피는 평화의 상징이다. 낡은 시대의 과오를 반복할 수는 없다.\n당신은 (후보 3)의 지지자를 자처하며, 성전을 막을 것을 선언하였다.\n\n정치력 10 증가!\n연설 시마다 (후보 3)의 정치력 5 증가!\n\n퀘스트 : (후보 1)을(를) 탈락시킨 후 (후보 3)을(를) 당선시키세요.\n성공 시 다른 엔딩을 볼 수 있습니다.";
        option2SuccessResult = "정치력 10 증가\n연설 시 후보 3 정치력 5 증가\n후보 3이 탈락한 상태에서 후보 1 당선 시 \"외교 승리\" 엔딩";
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
            performer.ChangePiety(-10f);

            // 연설 시마다 후보 1 정치력 5 증가 처리 필요
            // 후보 3 탈락 + 후보 1 당선 시 성전 엔딩 처리 필요

            return true;
        }
        else
        {  // 실패했을 때 로직
            

            return false;
        }
    }


    public override bool OnChoiceOption2(Cardinal performer)
    {
        if(!CanChoiceOption2(performer)) return false;

        if(Random.value <= option2Chance)
        {  // 성공했을 때 로직
            performer.ChangeInfluence(10f);

            // 연설 시마다 후보 3 정치력 5 증가 처리 필요
            // 외교 승리 엔딩 조건 확인 필요: 설명은 후보 1 탈락 + 후보 3 당선이나, 효과 문구는 후보 3 탈락 + 후보 1 당선으로 적혀 있음

            return true;
        }
        else
        {  // 실패했을 때 로직
            

            return false;
        }
    }
}
