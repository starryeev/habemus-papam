using UnityEngine;

[CreateAssetMenu(fileName = "E40100", menuName = "Events/소똥구리 토론")]
public class E40100 : Event
{
    void Reset()
    {
        eventID = "E40100";
        eventName = "소똥구리 토론";
        eventDescription = "추기경들이 옹기종기 앉아 소똥구리 한마리를 관찰하고 있다.\n\"그러고 보니, 이 벌레가 똥을 굴리는 모양새가 태양의 운행과 퍽 닮지 않았소이까?\"\n추기경들은 돌아가며 자신의 의견을 말했고, 이제 당신의 차례다!";
        maxAppear = 1;

        eventWeightBase = 20f;
        eventWeightMultiplier = 0f;

        option1 = "신님이 당신의 모습을 본 떠 인간을 빚었듯 태양의 불변함을 이 미물에 담은 것이다.";
        option1Chance = 1f;
        option1Requirement = "경건함 4 이상";
        option1SuccessDescription = "당신의 주장은 큰 지지를 받았다. 추기경들은 이 신성한 벌레를 태양충이라 부르기로 했다.\n\n체력 1 증가!\n정치력 1 증가!";
        option1SuccessResult = "체력 +1\n정치력 +1";

        option2 = "으디서 그런 수천 년 전 이교도가 할 법한 소리를!";
        option2Chance = 1f;
        option2SuccessDescription = "이교! 척결! 오늘도 당신의 손으로 신앙의 순수함을 지켜냈다.\n\n경건함 3 증가!";
        option2SuccessResult = "경건함 +3";
    }

    public override bool CanChoiceOption1(Cardinal performer)
    {
        if(performer.Piety < 4f) return false;
        return true;
    }

    public override bool CanChoiceOption2(Cardinal performer)
    {
        return true;
    }

    public override bool OnChoiceOption1(Cardinal performer)
    {
        if(!CanChoiceOption1(performer)) return false;
        performer.ChangeHp(1f);
        performer.ChangeInfluence(1f);
        return true;
    }

    public override bool OnChoiceOption2(Cardinal performer)
    {
        if(!CanChoiceOption2(performer)) return false;
        performer.ChangePiety(3f);
        return true;
    }
}
