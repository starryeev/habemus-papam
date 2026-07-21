using UnityEngine;

[CreateAssetMenu(fileName = "E32001", menuName = "Events/이교도와 성전")]
public class E32001 : Event
{
    void Reset()
    {
        eventID = "E32001";
        eventName = "이교도와 성전";
        eventDescription = "대신전 폭탄 테러 사건을 수습하고 다시 열린 썬ㅡ클라베.\n(후보 1)은(는) 당장 회의를 중단하고 성전을 일으킬 것을 종용한다.\n신중파인 (후보 3)은(는) 외교적 방법을 제안하며 필사적으로 성전을 막고 있다.\n교단의 운명이 당신의 손에 달렸다! (후보 3)을(를) 도와 성전을 막을 것인가?";
        maxAppear = 1;

        eventWeightBase = Mathf.Infinity;
        eventWeightMultiplier = 0f;

        // 발생 조건: 20번 이벤트에서 1번 선택지 실패 필요
        // 기피 이벤트: 22번 이벤트 필요
        // 현재 이벤트 스크립트 구조에서는 특정 선택지 결과 확인 및 기피 이벤트 연결 로직 필요

        option1 = "성전만큼은 막아야 한다!";
        option1Chance = 1f;
        option1Requirement = "경건함 50 이상";
        option1SuccessDescription = "끈질긴 설득 끝에, 원대한 군사 작전은 경제 재제와 외교적 항의로 축소되었다!\n교단이 평화를 되찾았다! 당신은 평화의 사도로서 큰 존경을 받게 되었다! 좋은 건가?\n\n경건함 20 증가!\n정치력 15 증가!";
        option1SuccessResult = "경건함 +20\n정치력 +15";

        option2 = "(후보 1) 교주 시키고 나는 은퇴나 할란다!";
        option2Chance = 1f;
        option2SuccessDescription = "(후보 1)는 화려한 언변으로 좌중을 압도하였다!\n누군가는 벌벌 떨리는 손으로, 또 누군가는 한껏 격앙된 얼굴로 말없이 그에게 표를 던졌다.\n썬ㅡ클라베는 끝났다. 주사위는 던져졌다. (후보 1)의 취임 연설에는 조금의 망설임도 없었다.\n이단은 척결되어야 한다!\n\n썬ㅡ클라베 종료!";
        option2SuccessResult = "게임 종료\n\"성전\" 엔딩";
    }

    public override bool CanChoiceOption1(Cardinal performer)
    {
        if(performer.Piety < 50f) return false;
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
            performer.ChangePiety(20f);
            performer.ChangeInfluence(15f);

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
            // 성전 엔딩 처리 필요

            return true;
        }
        else
        {  // 실패했을 때 로직
            

            return false;
        }
    }
}
