using UnityEngine;
using static GameContext;
using System.Collections;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "P021", menuName = "Plot/무릎 꿇기", order = 021)]

public class P021 : Plot
{
    [Header("해당 공작 설정")]
    [SerializeField] private int minInfluence;
    [SerializeField] private int pietyCost;
    [SerializeField] private int influenceDelta;
    [SerializeField] private int baseNextDayInfluenceDelta;
    [SerializeField] private int influenceGainPerConclave;

    public override int cost => pietyCost;

    void Reset()
    {
        // 설정 기본값
        plotID = "P021";
        plotGrade = PlotGrade.Rare;

        // 수치 기본값
        plotWeightBase = 15;
        plotWeightMultiplier = 0.05f;

        minInfluence = 6;
        pietyCost = 1;
        influenceDelta = -2;
        baseNextDayInfluenceDelta = 2;
        influenceGainPerConclave = 1;

        // 텍스트 기본값
        plotName = "무릎 꿇기";
        plotDescription = "추진력을 얻기 위함이었다!";
        plotEffect = "모든 상대 후보 정치력<sprite name=influence> 15 감소\n다음 날 첫 콘클라베 때 지난 콘클라베 수에 따라 모든 상대 후보 정치력<sprite name=influence> 20 ~ 50 증가 ";
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

        Debug.Log("P021 사용");

        var cm = CardinalManager.Instance;

        for (int i = 0; i < 3; i++)
        {
            cm.Cardinals[i].ChangeInfluence(influenceDelta);
        }

        /*
         * 다음 날 콘클라베가 되면 다시 정치력 증가하는 함수
         * 다음날쓰
         * 공작 시점 이브닝20, 애프터눈30, 모닝40, 던50
         */
        PlotManager.Instance.StartCoroutine(CountConclaveProcess(performer));
    }

    private IEnumerator CountConclaveProcess(Cardinal performer)
    {
        var gm = InGameManager.Instance;

        int currentDay = gm.GetCurrentDay();
        int conclaveCount = 0;

        bool isNextDayStarted = false;

        System.Action<GameContextEvent> eventHandler = (evt) => {
            if (evt == GameContextEvent.ConclaveEnd)
            {
                conclaveCount++;
            }

            if (evt == GameContextEvent.ConclaveStart)
            {
                if (currentDay < gm.GetCurrentDay())
                {
                    isNextDayStarted = true;
                }
            }
        };

        InGameManager.Instance.Context.OnGameContextEvent += eventHandler;

        yield return new WaitUntil(() => isNextDayStarted);

        InGameManager.Instance.Context.OnGameContextEvent -= eventHandler;

        var cm = CardinalManager.Instance;

        int totalInfluenceDelta = ((conclaveCount - 1) * influenceGainPerConclave) + baseNextDayInfluenceDelta;

        for (int i = 0; i < 3; i++)
        {
            cm.Cardinals[i].ChangeInfluence(totalInfluenceDelta);
        }

    }
}
