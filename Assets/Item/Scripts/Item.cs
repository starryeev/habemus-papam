using UnityEngine;

public enum ItemGrade { Common, Rare }
public enum ItemExpirationType { Conclave, Day, Permanent, Special }
public enum ItemUsageType
{
    Passive,
    Active
}

public abstract class Item : ScriptableObject
{
    [SerializeField] public string itemID;
    [SerializeField] public string itemName;
    [TextArea] public string itemDescription;
    [TextArea] public string itemEffectDescription;
    [SerializeField] public Sprite itemImage;

    [Header("Usage")]
    [SerializeField] public ItemGrade itemGrade;
    [SerializeField] public ItemExpirationType itemExpirationType;
    [SerializeField] public ItemUsageType usageType;

    public virtual bool IsDurationBuff => false;
    public virtual bool ConsumeOnUse => true;

    public virtual void OnAcquire() { }
    public virtual void OnRemove() { }
    public virtual void OnUse() { }
    public virtual void OnPray(Cardinal owner) { }
    public virtual void OnSpeech(Cardinal owner) { }
    public virtual void OnPlot(Cardinal owner) { }
    public virtual void OnReapply(Cardinal owner) { }
    public virtual bool OnHpReachedZero(Cardinal owner) { return false; }
    public virtual void ResetRuntimeState() { }
    public virtual string CaptureRuntimeState() { return string.Empty; }
    public virtual void RestoreRuntimeState(string runtimeStateJson) { }

    public string GetColoredGrade()
    {
        string colorHex = "";
        string gradeText = "";

        switch (itemGrade)
        {
            case ItemGrade.Common:
                colorHex = "#4488FF";
                gradeText = "일반";
                break;
            case ItemGrade.Rare:
                colorHex = "#9B30FF";
                gradeText = "레어";
                break;
            default:
                colorHex = "#FFFFFF";
                gradeText = "기타";
                break;
        }

        return $"<color={colorHex}>{gradeText}</color>";
    }

    public virtual float ModifySpeechInfluence(float originalDelta, GameBalance balance, bool isSuccess)
    {
        return originalDelta;
    }

    public virtual float PreviewPrayerPiety(float originalDelta, GameBalance balance, bool isSuccess)
    {
        return originalDelta;
    }

    public virtual float PreviewPrayerHp(float originalDelta, GameBalance balance, bool isSuccess)
    {
        return originalDelta;
    }

    public virtual float PreviewSpeechInfluence(float originalDelta, GameBalance balance, bool isSuccess)
    {
        return originalDelta;
    }

    public virtual float PreviewSpeechInfluenceAfterAction(float originalDelta, GameBalance balance, bool isSuccess)
    {
        return originalDelta;
    }

    public virtual float PreviewSpeechHp(float originalDelta, GameBalance balance, bool isSuccess)
    {
        return originalDelta;
    }
}
