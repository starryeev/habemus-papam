using UnityEngine;

[CreateAssetMenu(fileName = "E31213", menuName = "Events/비밀 문자 해독")]
public class E31213 : Event
{
    void Reset()
    {
        eventID = "E31213";
        eventName = "비밀 문자 해독";
        eventDescription = "고고학자들이 디아나가 가져온 황금 경판을 해독한 결과를 제출하였다.\n태초에 말씀이 계셨다...어라? 우리가 알고 있는 경전과 다른 부분이 꽤 많다!\n특히 몇몇 부분은 교리에 어긋나는 내용들이 존재한다.\n(후보 3)은(는) 유물은 태양의 계시이므로, 내용을 공표하고 교리를 바꿔야 할 것이라고 주장한다.\n한편 (후보 1)은(는) 교리의 정통성과 신학적 우월함을 주장하며, 경전의 내용을 비밀에 부칠 것을 주장한다.\n누구의 편을 들어야 할까?";
        maxAppear = 1;

        eventWeightBase = 40f;
        eventWeightMultiplier = 0f;

        option1 = "(후보 1)을(를) 지지한다!";
        option1Chance = 1f;
        option1SuccessDescription = "당신은 기존 교리의 정통성을 주장한 (후보 1)을(를) 지지했다.\n\n정치력 1 증가!\n(후보 1)의 정치력 3 증가!";
        option1SuccessResult = "정치력 +1\n(후보 1) 정치력 +3";

        option2 = "(후보 3)을(를) 지지한다!";
        option2Chance = 1f;
        option2SuccessDescription = "당신은 황금 경전의 내용을 공표하자는 (후보 3)을(를) 지지했다.\n\n경건함 2 증가!\n(후보 3)의 정치력 3 증가!";
        option2SuccessResult = "경건함 +2\n(후보 3) 정치력 +3";
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
            performer.ChangeInfluence(1f);

            var aiCardinals = CardinalManager.Instance.GetAICardinlas();
            aiCardinals[0].ChangeInfluence(3f);

            return FinishChoice(1, true);
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
            performer.ChangePiety(2f);

            var aiCardinals = CardinalManager.Instance.GetAICardinlas();
            aiCardinals[2].ChangeInfluence(3f);

            return FinishChoice(2, true);
        }
        else
        {  // 실패했을 때 로직
            

            return FinishChoice(2, false);
        }
    }
}
