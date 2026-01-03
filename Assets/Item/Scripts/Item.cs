using UnityEngine;

public enum ItemGrade { Common, Rare }

public abstract class Item : ScriptableObject
{
    public string itemID;
    public string itemName;
    public ItemGrade itemGrade;

    // 공통
    public virtual void OnAcquire() { }
    public virtual void OnRemove() { }
}
