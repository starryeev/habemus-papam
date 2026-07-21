using System.Collections;
using UnityEngine;

[CreateAssetMenu(fileName = "E40400", menuName = "Events/호우주의보")]
public class E40400 : Event
{
    void Reset()
    {
        eventID = "E40400";
        eventName = "호우주의보";
        eventDescription = "추적추적.... 창밖을 보니 비가 내리고 있다.\n빗소리가 은은하게 울려 퍼지는 대신전은 어느 때보다 고요했다.";
        maxAppear = 1;

        eventWeightBase = 20f;
        eventWeightMultiplier = 0f;

        option1 = "눈을 감고 빗소리를 듣자.";
        option1Chance = 1f;
        option1Requirement = "-";
        option1SuccessDescription = "몸과 마음이 치유되는 느낌이다.\n\n모든 후보의 체력 20 증가!\n이번 썬ㅡ클라베 종료까지 이동 속도 10% 증가!";
        option1SuccessResult = "모든 후보의 체력 + 20\n콘클라베 종료까지 이동 속도 + 10%";
        option2 = "";
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
            cardinal.ChangeHp(20f);
            performer.StartCoroutine(Co_ApplySpeedUntilConclaveEnd(cardinal, 0.1f));
        }

        return true;
    }

    public override bool OnChoiceOption2(Cardinal performer)
    {
        return true;
    }

    private IEnumerator Co_ApplySpeedUntilConclaveEnd(Cardinal target, float delta)
    {
        if(target == null || InGameManager.Instance == null || InGameManager.Instance.Context == null) yield break;

        bool isEnded = false;
        GameContext context = InGameManager.Instance.Context;

        void OnContextEvent(GameContext.GameContextEvent eventType)
        {
            if(eventType == GameContext.GameContextEvent.ConclaveEnd) isEnded = true;
        }

        target.ChangeSpeed(delta);
        context.OnGameContextEvent += OnContextEvent;
        yield return new WaitUntil(() => isEnded || target == null);
        if(target != null) target.ChangeSpeed(-delta);
        context.OnGameContextEvent -= OnContextEvent;
    }
}
