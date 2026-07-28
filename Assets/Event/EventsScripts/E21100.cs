using UnityEngine;

[CreateAssetMenu(fileName = "E21100", menuName = "Events/신앙인가, 과학인가?")]
public class E21100 : Event
{
    void Reset()
    {
        eventID = "E21100";
        eventName = "신앙인가, 과학인가?";
        maxAppear = 1;

        eventWeightBase = 0f;
        eventWeightMultiplier = 0.2f;

        option1Chance = 1f;
        option2Chance = 1f;
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
            Cardinal candidate1 = GetCandidate(1);
            if(candidate1 != null) candidate1.ChangeInfluence(1f);
            InGameManager.Instance.EventManager.SetPlotDamageBonus(2, 1f);

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
            performer.ChangeInfluence(1f);
            Cardinal candidate2 = GetCandidate(2);
            if(candidate2 != null) candidate2.ChangeInfluence(1f);

            performer.prayDeltaHpEvent = 5f;

            return FinishChoice(2, true);
        }
        else
        {  // 실패했을 때 로직
            

            return FinishChoice(2, false);
        }
    }
}
