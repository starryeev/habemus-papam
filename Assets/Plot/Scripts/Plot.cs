using UnityEngine;

public enum PlotGrade { Common, Rare, Legendary }
public enum PlotCostResource { Piety, Hp }

[System.Serializable]
public class PlotIconImageIndexes
{
    public int icon1 = -1;
    public int icon2 = -1;
    public int icon3 = -1;
    public int icon1S = -1;
    public int icon2S = -1;
    public int icon3S = -1;

    public int GetImageIndex(int index, bool isSelectedIcon)
    {
        switch (index)
        {
            case 0:
                return isSelectedIcon ? icon1S : icon1;
            case 1:
                return isSelectedIcon ? icon2S : icon2;
            case 2:
                return isSelectedIcon ? icon3S : icon3;
            default:
                return -1;
        }
    }

    public PlotIconImageIndexes Clone()
    {
        return new PlotIconImageIndexes
        {
            icon1 = icon1,
            icon2 = icon2,
            icon3 = icon3,
            icon1S = icon1S,
            icon2S = icon2S,
            icon3S = icon3S
        };
    }
}

public abstract class Plot : ScriptableObject
{
    [SerializeField] public string plotID;
    [SerializeField] public string plotName;
    [TextArea] public string plotDescription;
    [TextArea] public string plotEffect;
    [TextArea] public string plotCondiText;
    [TextArea] public string plotCostText;
    [HideInInspector]
    [SerializeField] public Sprite plotImage;
    [HideInInspector]
    [SerializeField] public Sprite icon1;
    [HideInInspector]
    [SerializeField] public Sprite icon2;
    [HideInInspector]
    [SerializeField] public Sprite icon3;
    [HideInInspector]
    [SerializeField] public Sprite icon1S;
    [HideInInspector]
    [SerializeField] public Sprite icon2S;
    [HideInInspector]
    [SerializeField] public Sprite icon3S;
    [Header("공작 이미지 번호")]
    [SerializeField] public PlotIconImageIndexes plotIconImageIndexes = new PlotIconImageIndexes();
    [SerializeField] public PlotGrade plotGrade;
    [SerializeField] public float plotWeightBase;
    [SerializeField] public float plotWeightMultiplier;

    public virtual int cost => 0;
    public virtual PlotCostResource CostResource => PlotCostResource.Piety;

    public Sprite GetIconSprite(int index, bool isSelectedIcon)
    {
        switch (index)
        {
            case 0:
                return isSelectedIcon ? icon1S : icon1;
            case 1:
                return isSelectedIcon ? icon2S : icon2;
            case 2:
                return isSelectedIcon ? icon3S : icon3;
            default:
                return null;
        }
    }

    public float GetPlotWeight()
    {
        int day = InGameManager.Instance != null ? InGameManager.Instance.GetCurrentDay() : 1;
        return plotWeightBase + plotWeightMultiplier * Mathf.Max(1, day);
    }

    public int GetInfluenceRequirement()
    {
        if (string.IsNullOrWhiteSpace(plotCondiText)) return 0;
        System.Text.RegularExpressions.Match match = System.Text.RegularExpressions.Regex.Match(
            plotCondiText, @"name=influence>(-?\d+)");
        if (!match.Success || !int.TryParse(match.Groups[1].Value, out int value)) return 0;
        return Mathf.Clamp(value, 0, 10);
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
