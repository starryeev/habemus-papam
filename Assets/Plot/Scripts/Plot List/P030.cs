using System;
using System.Collections;
using UnityEngine;
using static UnityEngine.GraphicsBuffer;

[CreateAssetMenu(fileName = "P030", menuName = "Plot/결코 다시 전쟁!", order = 030)]

public class P030 : Plot
{

    private const string MinHpEffectSource = "P030";

    [Header("해당 공작 설정")]
    [SerializeField] private int minInfluence;
    [SerializeField] private int pietyCost;
    [SerializeField] private int influenceDelta;

    public override int cost => pietyCost;

    void Reset()
    {
        // 설정 기본값
        plotID = "P030";
        plotGrade = PlotGrade.Legendary;

        // 수치 기본값
        plotWeightBase = 10;
        plotWeightMultiplier = 0f;

        minInfluence = 6;
        pietyCost = 0;
        influenceDelta = 4;

        // 아이콘 기본값
        PlotIconImageIndexes iconIndexes = new PlotIconImageIndexes();
        iconIndexes.icon1 = 6;
        iconIndexes.icon2 = 2;
        iconIndexes.icon2S = 3;
        iconIndexes.icon3 = 0;
        iconIndexes.icon3S = 11;
        plotIconImageIndexes = iconIndexes;

        // 텍스트 기본값
        plotName = "결코 다시 전쟁!";
        plotDescription = "성전이다, 우매한 이단들아!";
        plotEffect = "모든 후보 정치력<sprite name=influence> 4 증가\n이번 콘클라베 동안 자신의 체력<sprite name=hp> 최소 1 유지";
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

        var cm = CardinalManager.Instance;

        performer.ChangeInfluence(influenceDelta);

        for (int i = 0; i < 3; i++)
        {
            cm.Cardinals[i].ChangeInfluence(influenceDelta);
        }

        performer.StartCoroutine(Co_ApplyMinHpOneUntilConclaveEnd(performer));
    }

    private IEnumerator Co_ApplyMinHpOneUntilConclaveEnd(Cardinal performer)
    {
        if (performer == null || InGameManager.Instance == null || InGameManager.Instance.Context == null)
        {
            yield break;
        }

        GameContext context = InGameManager.Instance.Context;
        bool isEnded = false;

        void OnContextEvent(GameContext.GameContextEvent eventType)
        {
            if (eventType == GameContext.GameContextEvent.ConclaveEnd)
            {
                isEnded = true;
            }
        }

        performer.SetMinHpOneEffect(MinHpEffectSource, true);
        context.OnGameContextEvent += OnContextEvent;

        yield return new WaitUntil(() => isEnded || performer == null);

        if (performer != null)
        {
            performer.SetMinHpOneEffect(MinHpEffectSource, false);
        }

        if (context != null)
        {
            context.OnGameContextEvent -= OnContextEvent;
        }
    }
}
