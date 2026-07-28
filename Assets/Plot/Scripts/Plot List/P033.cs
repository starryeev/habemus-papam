using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static GameContext;
using static UnityEngine.GraphicsBuffer;

[CreateAssetMenu(fileName = "P033", menuName = "Plot/나는 용서하마", order = 033)]

public class P033 : Plot
{
    [Header("해당 공작 설정")]
    [SerializeField] private int minInfluence;
    [SerializeField] private int pietyCost;

    public override int cost => pietyCost;

    void Reset()
    {
        // 설정 기본값
        plotID = "P033";
        plotGrade = PlotGrade.Legendary;

        // 수치 기본값
        plotWeightBase = 15;
        plotWeightMultiplier = 0f;

        minInfluence = 6;
        pietyCost = 6;

        // 텍스트 기본값
        plotName = "나는 용서하마";
        plotDescription = "하지만 이 녀석이 용서할까?";
        plotEffect = "다음 콘클라베 때, 이번 콘클라베 동안 감소한 체력<sprite name=hp>만큼 모든 상대 후보의 체력<sprite name=hp> 감소";
        plotCondiText = $"<sprite name=influence>{minInfluence}<sprite name=up>";
        plotCostText = $"<sprite name=piety>  {cost}";
    }

    public override bool CanExecute(Cardinal performer)
    {
        return performer.Influence >= minInfluence;
    }

    public override bool IsCostEnough(Cardinal performer)
    {
        return performer.Piety >= cost;
    }

    public override void Execute(Cardinal performer)
    {
        if (!CanExecute(performer)) return;

        PayCost(performer);

        PlotManager.Instance.StartCoroutine(RevengeRoutine(performer));
    }

    private IEnumerator RevengeRoutine(Cardinal performer)
    {
        float totalDamageTaken = 0f;
        float lastCheckHp = performer.Hp;

        // 상태 제어를 위한 변수들
        bool isConclaveEnded = false;
        bool isNextConclaveStarted = false;

        System.Action<GameContextEvent> eventHandler = (evt) => {
            if (evt == GameContextEvent.ConclaveEnd)
            {
                isConclaveEnded = true;
            }

            if (evt == GameContextEvent.ConclaveStart)
            {
                isNextConclaveStarted = true;
            }
        };

        InGameManager.Instance.Context.OnGameContextEvent += eventHandler;

        while (!isConclaveEnded)
        {
            if (performer == null)
            {
                InGameManager.Instance.Context.OnGameContextEvent -= eventHandler;
                yield break;
            }

            if (performer.Hp < lastCheckHp)
            {
                totalDamageTaken += (lastCheckHp - performer.Hp);
                lastCheckHp = performer.Hp;
            }
            else if (performer.Hp > lastCheckHp)
            {
                lastCheckHp = performer.Hp;
            }

            yield return null;
        }

        yield return new WaitUntil(() => isNextConclaveStarted);

        InGameManager.Instance.Context.OnGameContextEvent -= eventHandler;

        if (totalDamageTaken > 0)
        {
            ExecuteMassiveDamage(performer, totalDamageTaken);
        }
    }

    private void ExecuteMassiveDamage(Cardinal performer, float damage)
    {
        if (damage <= 0) return;

        var cm = CardinalManager.Instance;

        for (int i = 0; i < 3; i++)
        {
            var target = cm.Cardinals[i];
            ApplyHpDelta(performer, target, -damage);
        }
    }
}
