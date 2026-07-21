using JetBrains.Annotations;
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

            var aiCardinals = CardinalManager.Instance.GetAICardinlas();
            aiCardinals[2].ChangeInfluence(-30f);

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
            var aiCardinals = CardinalManager.Instance.GetAICardinlas();
            aiCardinals[2].ChangeInfluence(-50f);

            var cardinals = CardinalManager.Instance.Cardinals;
            foreach(var c in cardinals)
            {
                c.ChangeHp(-20f);
            }

            return true;
        }
        else
        {  // 실패했을 때 로직
            // 후보2 탈락로직필요
            var aiCardinals = CardinalManager.Instance.GetAICardinlas();
            aiCardinals[2].ChangeHp(-40f);
            aiCardinals[2].ChangeInfluence(50f);

            return false;
        }
    }
}
