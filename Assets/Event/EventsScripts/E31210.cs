using UnityEngine;

[CreateAssetMenu(fileName = "E31210", menuName = "Events/태양 만세!")]
public class E31210 : Event
{
    void Reset()
    {
        eventID = "E31210";
        eventName = "태양 만세!";
        maxAppear = 1;

        eventWeightBase = 40f;
        eventWeightMultiplier = 0f;

        option1Chance = 1f;
        option2Chance = 0.5f;
        option1Requirement = "경건함 4 이상";
    }

    public override bool CanChoiceOption1(Cardinal performer)
    {
        if(performer.Piety < 4) return false;
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
            performer.ChangeHp(-2f);
            performer.ChangeInfluence(2f);

            Cardinal candidate3 = GetCandidate(3);
            if(candidate3 != null) candidate3.ChangeInfluence(-3f);

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
            Cardinal candidate3 = GetCandidate(3);
            if(candidate3 != null) candidate3.ChangeInfluence(-5f);

            var cardinals = CardinalManager.Instance.Cardinals;
            foreach(var c in cardinals)
            {
                c.ChangeHp(-2f);
            }

            return FinishChoice(2, true);
        }
        else
        {  // 실패했을 때 로직
            EliminateCandidate(2);
            Cardinal candidate3 = GetCandidate(3);
            if(candidate3 != null)
            {
                candidate3.ChangeHp(-4f);
                candidate3.ChangeInfluence(5f);
            }

            return FinishChoice(2, false);
        }
    }
}
