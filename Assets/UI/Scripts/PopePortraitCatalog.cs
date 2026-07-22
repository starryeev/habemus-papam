using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class PopePortraitEntry
{
    public CandidateSlot slot;
    public Sprite portrait;
}

[CreateAssetMenu(fileName = "PopePortraitCatalog", menuName = "UI/Pope Portrait Catalog")]
public class PopePortraitCatalog : ScriptableObject
{
    [SerializeField] private List<PopePortraitEntry> entries = new List<PopePortraitEntry>();

    public Sprite GetPortrait(CandidateSlot slot)
    {
        foreach (PopePortraitEntry entry in entries)
        {
            if (entry != null && entry.slot == slot)
            {
                return entry.portrait;
            }
        }

        return null;
    }
}
