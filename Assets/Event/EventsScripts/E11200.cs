using UnityEngine;

[CreateAssetMenu(fileName = "E11200", menuName = "Events/기도")]
public class E11200 : Event
{
    void Reset()
    {
        eventID = "E11200";
        eventName = "기도";
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
        if(!CanChoiceOption1(performer)) return false;

        

        return FinishChoice(1, true);
    }


    public override bool OnChoiceOption2(Cardinal performer)
    {
        if(!CanChoiceOption2(performer)) return false;
        return FinishChoice(2, true);
    }
}
