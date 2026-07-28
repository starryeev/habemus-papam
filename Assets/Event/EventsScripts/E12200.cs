using System.Collections;
using UnityEngine;

[CreateAssetMenu(fileName = "E12200", menuName = "Events/태양의 발")]
public class E12200 : Event
{
    void Reset()
    {
        eventID = "E12200";
        eventName = "태양의 발";
        maxAppear = 2;

        eventWeightBase = 30f;
        eventWeightMultiplier = 0f;

        option1Chance = 1f;
        option2Chance = 0.7f;

        // 선행 충돌 이벤트는 일단 인스펙터에서 드래그드롭으로처리
        //preEvents.Add(InGameManager.Instance.EventManager.GetEventById("E11100"));
    }

    public override bool CanChoiceOption1(Cardinal performer)
    {
        if(performer.Piety >= 40f) return true;
        else return false;
    }

    public override bool CanChoiceOption2(Cardinal performer)
    {
        return true;
    }

    public override bool OnChoiceOption1(Cardinal performer)
    {
        if(Random.value > option1Chance) return false;
        if(!CanChoiceOption1(performer)) return false;

        performer.ChangePiety(10);

        return true;
    }


    public override bool OnChoiceOption2(Cardinal performer)
    {
        if(!CanChoiceOption2(performer)) return false;

        if(Random.value <= option2Chance)
        {
            performer.ChangeHp(-10f);
            performer.StartCoroutine(Co_ApplySpeedUntilConclaveEnd(performer, 0.3f));

            return FinishChoice(2, true);
        }
        else
        {
            performer.ChangeHp(-10f);
            performer.StartCoroutine(Co_ApplySpeedUntilConclaveEnd(performer, -0.2f));
            return FinishChoice(2, false);
        }
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
