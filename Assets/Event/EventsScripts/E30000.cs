using UnityEngine;

[CreateAssetMenu(fileName = "E30000", menuName = "Events/새로운 전례")]
public class E30000 : Event
{
    void Reset()
    {
        eventID = "E30000";
        eventName = "새로운 전례";
        maxAppear = 1;

        eventWeightBase = 40f;
        eventWeightMultiplier = 0.1f;

        option1Chance = 1f;
        option2Chance = 0.5f;
        option1Requirement = "경건함 3 이상";
    }

    public override bool CanChoiceOption1(Cardinal performer)
    {
        if(performer.Piety < 3) return false;
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
            performer.ChangeHp(3);
            InGameManager.Instance.ChangeCurrentTurnActions(-1);

            return FinishChoice(2, true);
        }
        else
        {  // 실패했을 때 로직
            InGameManager.Instance.EndCurrentConclave();
            return FinishChoice(2, false);
        }
    }
}
