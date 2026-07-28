using UnityEngine;

[CreateAssetMenu(fileName = "E11300", menuName = "Events/연설")]
public class E11300 : Event
{
    void Reset()
    {
        eventID = "E11300";
        eventName = "연설";
        maxAppear = 1;

        eventWeightBase = 0f;
        eventWeightMultiplier = 0f;

        option1Chance = 1f;
        option2Chance = 1f;

        // 선행 충돌 이벤트는 일단 인스펙터에서 드래그드롭으로처리
        //preEvents.Add(InGameManager.Instance.EventManager.GetEventById("E11100"));
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

        

        return FinishChoice(1, true);
    }


    public override bool OnChoiceOption2(Cardinal performer)
    {
        return FinishChoice(2, true);
    }
}
