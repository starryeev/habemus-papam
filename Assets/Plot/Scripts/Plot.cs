using UnityEngine;

public enum PlotGrade { Common, Rare, Legendary }
public enum PlotCostResource { Piety, Hp }

public abstract class Plot : ScriptableObject
{
    [SerializeField] public string plotID;
    [SerializeField] public string plotName;
    [TextArea] public string plotDescription;
    [TextArea] public string plotEffect;
    [TextArea] public string plotCondiText;
    [TextArea] public string plotCostText;
    [SerializeField] public Sprite plotImage;
    [SerializeField] public PlotGrade plotGrade;
    [SerializeField] public float plotWeightBase;
    [SerializeField] public float plotWeightMultiplier;

    public virtual int cost => 0;
    public virtual PlotCostResource CostResource => PlotCostResource.Piety;

    public float GetPlotWeight()
    {
        float progressWeight = plotWeightMultiplier * InGameManager.Instance.GetProgress();
        
        return plotWeightBase + progressWeight;
    }

    // 조건 확인 함수, 구현은 자식 클래스에서 직접
    public abstract bool CanExecute(Cardinal performer);

    // 비용 확인 함수, 구현은 자식 클래스에서 직접
    public abstract bool IsCostEnough(Cardinal performer);

    public bool IsEffectiveCostEnough(Cardinal performer)
    {
        if (CostResource == PlotCostResource.Piety && IsPietyCostWaived(performer)) return true;
        return IsCostEnough(performer);
    }

    protected void PayCost(Cardinal performer)
    {
        if (performer == null || cost <= 0) return;

        if (CostResource == PlotCostResource.Piety)
        {
            if (!IsPietyCostWaived(performer)) performer.ChangePiety(-cost);
            return;
        }

        performer.ChangeHp(-cost);
    }

    protected void ApplyHpDelta(Cardinal performer, Cardinal target, float delta)
    {
        if (target == null) return;

        EventManager eventManager = InGameManager.Instance != null
            ? InGameManager.Instance.EventManager
            : null;
        float effectiveDelta = eventManager != null
            ? eventManager.ModifyPlotHpDelta(performer, target, delta)
            : delta;
        target.ChangeHp(effectiveDelta);
    }

    private static bool IsPietyCostWaived(Cardinal performer)
    {
        return InGameManager.Instance != null && InGameManager.Instance.EventManager != null &&
            InGameManager.Instance.EventManager.IsPlotPietyCostWaived(performer);
    }

    // 실제 실행시 로직 함수, 구현은 자식 클래스에서 직접
    public abstract void Execute(Cardinal performer);
}
