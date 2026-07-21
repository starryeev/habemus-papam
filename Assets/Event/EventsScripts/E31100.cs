using UnityEngine;

[CreateAssetMenu(fileName = "E31100", menuName = "Events/반란 모의")]
public class E31100 : Event
{
    void Reset()
    {
        eventID = "E31100";
        eventName = "반란모의";
        maxAppear = 1;

        eventWeightBase = 20f;
        eventWeightMultiplier = 0f;

        option1Chance = 1f;
        option2Chance = 1f;
    }

    public override bool CanChoiceOption1(Cardinal performer)
    {
        if(performer.Piety < 60) return false;
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
            performer.ChangePiety(40f);
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


            return true;
        }
        else
        {  // 실패했을 때 로직
            

            return false;
        }
    }
}
