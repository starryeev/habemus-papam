using UnityEngine;

[CreateAssetMenu(fileName = "E11100", menuName = "Events/큰일났다!")]
public class E11100 : Event
{
    void OnEnable()
    {
        if (!string.IsNullOrWhiteSpace(eventID)) return;

        EventManager manager = InGameManager.Instance != null ? InGameManager.Instance.EventManager : null;
        Event definition = manager != null ? manager.GetEventById("E11100") : null;
        if (definition != null && definition != this)
        {
            CopyDefinitionFrom(definition);
        }
        else
        {
            Reset();
        }
    }

    void Reset()
    {
        eventID = "E11100";
        eventName = "큰일났다!";
        maxAppear = 1;

        eventWeightBase = 0f;
        eventWeightMultiplier = 0f;

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
        if(Random.value > option1Chance) return false;

        performer.ChangePiety(8f);
        performer.ChangeInfluence(4f);
        return FinishChoice(1, true);
    }


    public override bool OnChoiceOption2(Cardinal performer)
    {
        return FinishChoice(2, true);
    }
}
