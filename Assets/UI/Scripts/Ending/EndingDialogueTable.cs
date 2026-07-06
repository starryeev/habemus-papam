using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "EndingDialogueTable", menuName = "Ending/Dialogue Table")]
public class EndingDialogueTable : ScriptableObject
{
    [SerializeField] private List<EndingDialogueEntry> entries = new List<EndingDialogueEntry>();

    public IReadOnlyList<EndingDialogueLine> GetLines(EndingType endingType)
    {
        foreach (EndingDialogueEntry entry in entries)
        {
            if (entry != null && entry.EndingType == endingType)
            {
                return entry.Lines;
            }
        }

        return Array.Empty<EndingDialogueLine>();
    }
}

[Serializable]
public class EndingDialogueEntry
{
    [SerializeField] private EndingType endingType;
    [SerializeField] private List<EndingDialogueLine> lines = new List<EndingDialogueLine>();

    public EndingType EndingType => endingType;
    public IReadOnlyList<EndingDialogueLine> Lines => lines;
}

[Serializable]
public class EndingDialogueLine
{
    [TextArea(1, 4)]
    [SerializeField] private string rawText;
    [SerializeField] private EndingLineDisplayMode displayMode;
    [SerializeField] private EndingLineCondition condition;
    [SerializeField] private string triggerEventId;
    [SerializeField] private int triggerOptionIndex;

    public string RawText => rawText;
    public EndingLineDisplayMode DisplayMode => displayMode;
    public EndingLineCondition Condition => condition;
    public string TriggerEventId => triggerEventId;
    public int TriggerOptionIndex => triggerOptionIndex;
}

public enum EndingLineDisplayMode
{
    Main,
    SubTextWithPrevious
}

public enum EndingLineCondition
{
    None,
    PlayerPietyGreaterThanInfluence,
    PlayerInfluenceGreaterThanPiety,
    TriggerEventOption
}
