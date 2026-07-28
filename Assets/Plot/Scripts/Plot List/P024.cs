using UnityEngine;

[CreateAssetMenu(fileName = "P024", menuName = "Plot/가지치기", order = 024)]

public class P024 : Plot
{
    [Header("해당 공작 설정")]
    [SerializeField] private int minHp;
    [SerializeField] private int pietyCost;
    [SerializeField] private int statsDelta;

    public override int cost => pietyCost;

    void Reset()
    {
        // 설정 기본값
        plotID = "P024";
        plotGrade = PlotGrade.Rare;

        // 수치 기본값
        plotWeightBase = 15;
        plotWeightMultiplier = 0f;

        minHp = 4;
        pietyCost = 0;
        statsDelta = 2;

        // 텍스트 기본값
        plotName = "가지치기";
        plotDescription = "줄건 줘";
        plotEffect = "체력<sprite name=hp>, 정치력<sprite name=influence>, 경건함<sprite name=piety> 중 가장 적은 수치 절반으로 감소\n나머지 15 증가";
        plotCondiText = $"<sprite name=hp>{minHp}<sprite name=up>";
        plotCostText = $"<sprite name=piety>  {cost}";
    }

    public override bool CanExecute(Cardinal performer)
    {
        return performer.Hp >= minHp;
    }

    public override bool IsCostEnough(Cardinal performer)
    {
        return performer.Piety >= cost;
    }

    public override void Execute(Cardinal performer)
    {
        if (!CanExecute(performer)) return;

        PayCost(performer);

        float hp = performer.Hp;
        float pol = performer.Influence;
        float piety = performer.Piety;

        float minVal = Mathf.Min(hp, Mathf.Min(pol, piety));

        // 3. 누가 깎일 대상인지 판별 (동률 포함)
        bool isHpMin = Mathf.Approximately(hp, minVal);
        bool isPolMin = Mathf.Approximately(pol, minVal);
        bool isPieMin = Mathf.Approximately(piety, minVal);

        // 4. [페널티] 최솟값인 스탯들은 모두 절반으로 감소
        if (isHpMin) performer.ChangeHp(-(hp / 2f));
        if (isPolMin) performer.ChangeInfluence(-(pol / 2f));
        if (isPieMin) performer.ChangePiety(-(piety / 2f));

        // 5. [보너스] 깎이지 않은 "나머지" 스탯들만 15 증가
        if (!isHpMin) performer.ChangeHp(statsDelta);
        if (!isPolMin) performer.ChangeInfluence(statsDelta);
        if (!isPieMin) performer.ChangePiety(statsDelta);
    }

}
