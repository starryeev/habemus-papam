using UnityEngine;

[CreateAssetMenu(fileName = "E31200", menuName = "Events/이단과 소문")]
public class E31200 : Event
{
    void Reset()
    {
        eventID = "E31200";
        eventName = "이단과 소문";
        maxAppear = 1;

        eventWeightBase = 40f;
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
            SetCandidateStats(2, 60f, 60f, 60f);

            // TODO(태양만세 엔딩): 후보 2가 최종 당선될 때 전용 엔딩으로 전환한다.
            // EndingType과 엔딩 컷신이 아직 없으므로 여기서는 부활/분기 기록까지만 수행한다.
            return FinishChoice(2, true);
        }
        else
        {  // 실패했을 때 로직
            

            return FinishChoice(2, false);
        }
    }
}
