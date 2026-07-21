using System.Collections;
using UnityEngine;

[CreateAssetMenu(fileName = "E40700", menuName = "Events/묵주닦이")]
public class E40700 : Event
{
    void Reset()
    {
        eventID = "E40700";
        eventName = "묵주닦이";
        eventDescription = "진흙같은 어두운 밤, 당신의 귓가에서 괴이한 소리가 울린다.\n아늑하고 달콤한 소리지만 당신은 그것의 출처가 악마임을 어렴풋이 느낀다.\n악마의 꼬드김에 넘어간 사람들의 결말은 뻔하다. 저항하자!";
        maxAppear = 1;

        eventWeightBase = 20f;
        eventWeightMultiplier = 0f;

        option1 = "아니다 이 악마야!";
        option1Chance = 1f;
        option1Requirement = "경건함 60 이상";
        option1SuccessDescription = "나의 능력을 조심하라, 솔라 랜턴 빛!\n머리에서 나온 찬란한 광휘에 악마는 저항도 못하고 스러졌다.\n\n체력 100 증가!";
        option1SuccessResult = "체력 + 100";

        option2 = "흠, 듣고 보니 맞는 말이군. 반박할 수가 없다.";
        option2Chance = 1f;
        option2SuccessDescription = "역사는 반복되고 사람은 끊임없이 멍청한 짓을 한다.\n\n체력 20 감소!\n이번 썬ㅡ클라베 동안 이동 속도 30% 감소!";
        option2SuccessResult = "체력 - 20\n콘클라베 종료까지 이동 속도 - 30%";
    }

    public override bool CanChoiceOption1(Cardinal performer)
    {
        if(performer.Piety < 60f) return false;
        return true;
    }

    public override bool CanChoiceOption2(Cardinal performer)
    {
        return true;
    }

    public override bool OnChoiceOption1(Cardinal performer)
    {
        if(!CanChoiceOption1(performer)) return false;
        performer.ChangeHp(100f);
        return true;
    }

    public override bool OnChoiceOption2(Cardinal performer)
    {
        if(!CanChoiceOption2(performer)) return false;
        performer.ChangeHp(-20f);
        performer.StartCoroutine(Co_ApplySpeedUntilConclaveEnd(performer, -0.3f));
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
