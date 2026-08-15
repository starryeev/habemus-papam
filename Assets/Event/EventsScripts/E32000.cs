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
        option1SuccessDescription = "당신은 이교도를 설득할 방법을 찾았고, 평화주의자인 (후보 2)이(가) 나섰다. 이교도는 끝내 폭탄 조끼를 벗었다.\n\n해당 콘클라베 즉시 종료!\n(후보 2)의 경건함 8 증가!\n(후보 2)의 체력 10 감소!";
        option1SuccessResult = "해당 콘클라베 즉시 종료\n(후보 2) 경건함 +8\n(후보 2) 체력 -10";
        option1FailDescription = "설득이 실패하고 폭탄 조끼가 터졌다. 회의장은 혼비백산에 빠졌다.\n\n해당 콘클라베 즉시 종료!\n(후보 2) 탈락!\n플레이어와 후보 1, 2, 3의 체력 2 감소!\n플레이어와 후보 1, 2, 3의 정치력 3 감소!";
        option1FailResult = "해당 콘클라베 즉시 종료\n(후보 2) 탈락\n플레이어와 후보 1, 2, 3 체력 -2\n플레이어와 후보 1, 2, 3 정치력 -3";

        option2 = "저격수를 배치해 이교도를 사살한다!";
        option2Chance = 1f;
        option2SuccessDescription = "교단 근위대의 저격수가 이교도를 제압했고 군중은 해산했다.\n\n해당 콘클라베 즉시 종료!\n플레이어와 후보 1, 2, 3의 경건함 4 감소!";
        option2SuccessResult = "해당 콘클라베 즉시 종료\n플레이어와 후보 1, 2, 3 경건함 -4";
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
                aiCardinals[1].ChangePiety(8f);
                aiCardinals[1].ChangeHp(-10f);
            }

            return FinishChoice(1, true);
        }
        else
        {  // 실패했을 때 로직
            EndCurrentConclave();

            EliminateCandidate(2);
            foreach(var cardinal in GetPlayerAndMainCandidates(performer))
            {
                cardinal.ChangeHp(-2f);
                cardinal.ChangeInfluence(-3f);
            }

            return FinishChoice(1, false);
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
                cardinal.ChangePiety(-4f);
            }

            return FinishChoice(2, true);
        }
        else
        {  // 실패했을 때 로직
            

            return FinishChoice(2, false);
        }
    }

    private void EndCurrentConclave()
    {
        // 해당 콘클라베 즉시 종료 처리
        InGameManager.Instance.EndCurrentConclave();
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
