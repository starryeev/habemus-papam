using UnityEngine;

[CreateAssetMenu(fileName = "E31101", menuName = "Events/북극곰의 반란")]
public class E31101 : Event
{
    void Reset()
    {
        eventID = "E31101";
        eventName = "북금곰의 반란";
        maxAppear = 1;

        eventWeightBase = 10f;
        eventWeightMultiplier = 0.05f;

        option1Chance = 1f;
        option2Chance = 1f;
    }

    public override bool CanChoiceOption1(Cardinal performer)
    {
        if(performer.Piety < 80) return false;
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
