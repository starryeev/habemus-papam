using UnityEngine;

[CreateAssetMenu(fileName = "E21000", menuName = "Events/과학적 발견")]
public class E21000 : Event
{
    void Reset()
    {
        eventID = "E21000";
        eventName = "과학적 발견";
        maxAppear = 1;

        eventWeightBase = 20f;
        eventWeightMultiplier = 0.1f;

        option1Chance = 1f;
        option2Chance = 1f;
    }

    public override bool CanChoiceOption1(Cardinal performer)
    {
        if(performer.Piety < 40f) return false;
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
            performer.ChangePiety(10f);
            performer.ChangeInfluence(-10f);

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
            performer.ChangeInfluence(10f);

            return FinishChoice(2, true);
        }
        else
        {  // 실패했을 때 로직
            

            return FinishChoice(2, false);
        }
    }
}
