using System.Collections;
using UnityEngine;

[CreateAssetMenu(fileName = "E50600", menuName = "Events/결코 다시 전쟁!")]
public class E50600 : Event
{
    void Reset()
    {
        eventID = "E50600";
        eventName = "결코 다시 전쟁!";
        eventDescription = "연단에서 누군가가 오늘도! 또! 어김없이!\n전쟁을 들먹이며 과격한 정치구호를 외치고 있다!\n이번에는 또 어떤 이단하고 싸우자는 걸까?\n\n뭐? 나라고?\n도저히 참을 수 없다!";
        maxAppear = 1;

        eventWeightBase = 0f;
        eventWeightMultiplier = 0.1f;

        option1 = "성전이다!";
        option1Chance = 1f;
        option1Requirement = "-";
        option1SuccessDescription = "너 죽고 나 죽자!\n내가 오늘 성전의 맛을 똑똑히 보여주겠다.\n\n모든 후보의 정치력 40 증가!\n이번 썬ㅡ클라베 동안 체력이 0으로 떨어지지 않음!";
        option1SuccessResult = "모든 후보의 정치력 +40\n이번 콘클라베 동안 최소 체력 1 유지";

        option2 = "우매한 이단들아!";
        option2Chance = 1f;
        option2SuccessDescription = "어디다 대고 신성한 대신전에서 삿대질이야!\n아예 뼈다귀도 못 추리게 해 주마.\n\n이번 썬ㅡ클라베 동안 공작이 경건함을 소모하지 않음!\n이번 썬ㅡ클라베 동안 체력이 0으로 떨어지지 않음!";
        option2SuccessResult = "이번 콘클라베 동안 공작 경건함 소모량 0으로 고정\n이번 콘클라베 동안 최소 체력 1 유지";
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

        foreach(var cardinal in CardinalManager.Instance.Cardinals)
        {
            cardinal.ChangeInfluence(40f);
            performer.StartCoroutine(Co_ApplyMinHpOneUntilConclaveEnd(cardinal));
        }

        return true;
    }

    public override bool OnChoiceOption2(Cardinal performer)
    {
        if(!CanChoiceOption2(performer)) return false;

        foreach(var cardinal in CardinalManager.Instance.Cardinals)
        {
            performer.StartCoroutine(Co_ApplyMinHpOneUntilConclaveEnd(cardinal));
        }

        // 이번 썬ㅡ클라베 동안 공작 경건함 소모량 0 고정 처리 필요
        return true;
    }

    private IEnumerator Co_ApplyMinHpOneUntilConclaveEnd(Cardinal target)
    {
        if(target == null || InGameManager.Instance == null || InGameManager.Instance.Context == null) yield break;

        bool isEnded = false;
        GameContext context = InGameManager.Instance.Context;

        void OnContextEvent(GameContext.GameContextEvent eventType)
        {
            if(eventType == GameContext.GameContextEvent.ConclaveEnd) isEnded = true;
        }

        target.SetMinHpOneEffect(true);
        context.OnGameContextEvent += OnContextEvent;
        yield return new WaitUntil(() => isEnded || target == null);
        if(target != null) target.SetMinHpOneEffect(false);
        context.OnGameContextEvent -= OnContextEvent;
    }
}
