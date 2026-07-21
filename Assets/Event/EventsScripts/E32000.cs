using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "E32000", menuName = "Events/폭탄 테러!")]
public class E32000 : Event
{
    void Reset()
    {
        eventID = "E32000";
        eventName = "폭탄 테러!";
        eventDescription = "대신전 앞 광장에서 이교도의 모자를 쓴 한 젊은이가 고함을 지르고 있다.\n'새로운 경전에 따라 태양은 달의 하수인임을 인정하라!'\n그의 조끼에는... 폭탄이 둘러져 있다!";
        maxAppear = 1;

        eventWeightBase = 40f;
        eventWeightMultiplier = 0f;

        // 발생 조건: 19번 이벤트에서 선택지 2 선택 필요
        // 현재 이벤트 스크립트 구조에서는 특정 선택지 선택 여부 확인 로직 필요

        option1 = "이교도와의 대화를 시도한다!";
        option1Chance = 0.9f;
        option1SuccessDescription = "당신은 빠르게 회의장의 분위기를 진정시키고, 이교도를 설득해 돌려보낼 방법을 물색한다.\n평화주의자인 (후보 2)이(가) 나와서, 평화를 설파하며 이교도를 축복하고 포옹하는데...\n푹, 하는 소리와 함께 (후보 2)의 로브에 피가 흥건해진다.\n그러나 그는 일말의 증오도 드러내지 않고, 인자한 웃음으로 용서와 평화의 메세지를 전했다!\n결국 이교도는 눈물을 흘리며 조끼를 벗는다.\n\n썬ㅡ클라베 중단!\n(후보 2)의 경건함 80 증가!\n(후보 2) 쓰러짐!";
        option1SuccessResult = "해당 콘클라베 즉시 종료\n후보 2 경건함 +80\n후보 2 체력 -100";
        option1FailDescription = "당신은 빠르게 회의장의 분위기를 진정시키고, 이교도를 설득해 돌려보낼 방법을 물색한다.\n평화주의자인 (후보 2)이(가) 나와서, 평화를 설파하며 이교도와 악수를 하려고 하자...\n펑! 귀청이 떨어져 나갈 것 같은 무시무시한 폭음과 함께 폭탄 조끼가 터지고 말았다.\n회의장이 혼비백산에 빠진다.\n\n썬ㅡ클라베 중단!\n(후보 2) 사망!\n모든 후보의 체력 20 감소!\n모든 후보의 정치력 30 감소!";
        option1FailResult = "해당 콘클라베 즉시 종료\n후보 2 탈락 처리\n플레이어와 후보 1, 2, 3 체력 -20\n플레이어와 후보 1, 2, 3 정치력 -30";

        option2 = "저격수를 배치해 이교도를 사살한다!";
        option2Chance = 1f;
        option2SuccessDescription = "교단 근위대에 연락하여 은밀히 저격수를 배치하였다.\n근위대와의 긴 대치 끝에 이교도가 폭탄 조끼를 만지려 하자,\n버튼을 누르기도 전에 총알이 그의 머리를 꿰뚫었다.\n혼란에 빠진 군중을 근위대가 빠르게 해산시켰다.\n\n썬ㅡ클라베 중단!\n모든 후보 경건함 40 감소!";
        option2SuccessResult = "플레이어와 후보 1, 2, 3 경건함 -40";
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
            EndCurrentConclave();

            var aiCardinals = CardinalManager.Instance.GetAICardinlas();
            if(aiCardinals.Count > 1)
            {
                aiCardinals[1].ChangePiety(80f);
                aiCardinals[1].ChangeHp(-100f);
            }

            return true;
        }
        else
        {  // 실패했을 때 로직
            EndCurrentConclave();

            // 후보 2 탈락 처리 필요
            foreach(var cardinal in GetPlayerAndMainCandidates(performer))
            {
                cardinal.ChangeHp(-20f);
                cardinal.ChangeInfluence(-30f);
            }

            return false;
        }
    }


    public override bool OnChoiceOption2(Cardinal performer)
    {
        if(!CanChoiceOption2(performer)) return false;

        if(Random.value <= option2Chance)
        {  // 성공했을 때 로직
            EndCurrentConclave();

            foreach(var cardinal in GetPlayerAndMainCandidates(performer))
            {
                cardinal.ChangePiety(-40f);
            }

            return true;
        }
        else
        {  // 실패했을 때 로직
            

            return false;
        }
    }

    private void EndCurrentConclave()
    {
        // 해당 콘클라베 즉시 종료 처리
        InGameManager.Instance.Context.ChangeRemainingTime(-99999f);
    }

    private List<Cardinal> GetPlayerAndMainCandidates(Cardinal performer)
    {
        var targets = new List<Cardinal>();

        if(performer != null)
        {
            targets.Add(performer);
        }

        var aiCardinals = CardinalManager.Instance.GetAICardinlas();

        for(int i = 0; i < 3 && i < aiCardinals.Count; i++)
        {
            targets.Add(aiCardinals[i]);
        }

        return targets;
    }
}
