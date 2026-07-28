using UnityEngine;

[CreateAssetMenu(fileName = "E31000", menuName = "Events/자연주의")]
public class E31000 : Event
{
    void Reset()
    {
        eventID = "E31000";
        eventName = "자연주의";
        maxAppear = 1;

        eventWeightBase = 20f;
        eventWeightMultiplier = 0.1f;

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
            Cardinal candidate3 = GetCandidate(3);
            if(candidate3 != null) candidate3.ChangeInfluence(2f);
            InGameManager.Instance.EventManager.SetPlotDamageBonus(2, 2f);

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
            Cardinal candidate2 = GetCandidate(2);
            if(candidate2 != null) candidate2.ChangeInfluence(2f);
            InGameManager.Instance.EventManager.SetPlotDamageBonus(3, 2f);

            return FinishChoice(2, true);
        }
        else
        {  // 실패했을 때 로직
            performer.ChangeInfluence(-3f);
            performer.ChangeHp(-2f);

            return FinishChoice(2, false);
        }
    }
}
