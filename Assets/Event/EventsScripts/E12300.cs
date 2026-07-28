using UnityEngine;

[CreateAssetMenu(fileName = "E12300", menuName = "Events/태양의 입")]
public class E12300 : Event
{
    void Reset()
    {
        eventID = "E12300";
        eventName = "태양의 입";
        maxAppear = 2;

        eventWeightBase = 30f;
        eventWeightMultiplier = 0f;

        option1Chance = 0.5f;
        option2Chance = 0.5f;
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
            performer.ChangeHp(10f);

            return FinishChoice(1, true);
        }
        else
        {  // 실패했을 때 로직
            performer.ChangeHp(-20f);
            performer.ChangePiety(20f);

            return FinishChoice(1, false);
        }
    }


    public override bool OnChoiceOption2(Cardinal performer)
    {
        if(!CanChoiceOption2(performer)) return false;

        if(Random.value <= option2Chance)
        {  // 성공했을 때 로직
            performer.ChangeHp(20f);

            return FinishChoice(2, true);
        }
        else
        {  // 실패했을 때 로직
            performer.ChangeHp(-20f);
            performer.ChangePiety(20f);

            return FinishChoice(2, false);
        }
    }
}
