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
    }

    public override bool CanChoiceOption1(Cardinal performer)
    {
        if(performer.Piety < 40) return false;
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
            performer.ChangeHp(-20f);
            performer.ChangeInfluence(20f);

            Cardinal candidate3 = GetCandidate(3);
            if(candidate3 != null) candidate3.ChangeInfluence(-30f);

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
            if(candidate3 != null) candidate3.ChangeInfluence(-50f);

            var cardinals = CardinalManager.Instance.Cardinals;
            foreach(var c in cardinals)
            {
                c.ChangeHp(-20f);
            }

            return FinishChoice(2, true);
        }
        else
        {  // 실패했을 때 로직
            EliminateCandidate(2);
            Cardinal candidate3 = GetCandidate(3);
            if(candidate3 != null)
            {
                candidate3.ChangeHp(-40f);
                candidate3.ChangeInfluence(50f);
            }

            return FinishChoice(2, false);
        }
    }
}
