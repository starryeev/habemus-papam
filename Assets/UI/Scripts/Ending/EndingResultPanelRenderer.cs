using System.Collections.Generic;
using TMPro;
using UnityEngine;

public static class EndingResultPanelRenderer
{
    private const string ResultPanelPath = "Canvas/ResultPanel";
    private const string PrayCountPath = ResultPanelPath + "/PrayCount";
    private const string SpeechCountPath = ResultPanelPath + "/SpeechCount";
    private const string ItemAchieveCountPath = ResultPanelPath + "/ItemAchiveCount";
    private const string MostItemAchieveCountPath = ResultPanelPath + "/MostItemAchiveCount";
    private const string HighPietyTimePath = ResultPanelPath + "/Living1Count";
    private const string HighInfluenceTimePath = ResultPanelPath + "/Living1-1Count";
    private const string LowPietyTimePath = ResultPanelPath + "/Living2Count";
    private const string LowInfluenceTimePath = ResultPanelPath + "/Living2-2Count";
    private const string StunCountPath = ResultPanelPath + "/StunCount";
    private const string BadEndingCountPath = ResultPanelPath + "/BadEndCount";
    private const string HappyEndingCountPath = ResultPanelPath + "/HappyEndCount";
    private const string PapalElectionCountPath = ResultPanelPath + "/ElectionCount";
    private const string PapalElectionFailedCountPath = ResultPanelPath + "/NotElectionCount";
    private const string PopeGenerationPath = ResultPanelPath + "/HowManyCount";
    private const string ConclaveCountPath = ResultPanelPath + "/ConClaveCount";

    private static readonly Dictionary<string, string> OriginalTextsByPath = new Dictionary<string, string>();

    public static void PopulateCurrentRunStats()
    {
        ActionRecordManager records = ActionRecordManager.Instance;

        if (records == null)
        {
            Debug.LogWarning("[EndingResult] ActionRecordManager was not found.");
            return;
        }

        SetInt(PrayCountPath, records.GetCurrentPrayCount());
        SetInt(SpeechCountPath, records.GetCurrentSpeechCount());
        SetInt(ItemAchieveCountPath, records.GetCurrentItemAcquireTotalCount());
        SetText(MostItemAchieveCountPath, records.GetCurrentMostAcquiredItemName());
        SetSeconds(HighPietyTimePath, records.GetCurrentHighPietyTime());
        SetSeconds(HighInfluenceTimePath, records.GetCurrentHighInfluenceTime());
        SetSeconds(LowPietyTimePath, records.GetCurrentLowPietyTime());
        SetSeconds(LowInfluenceTimePath, records.GetCurrentLowInfluenceTime());
        SetInt(StunCountPath, records.GetCurrentStunCount());
        SetInt(BadEndingCountPath, records.GetCurrentBadEndingCount());
        SetInt(HappyEndingCountPath, records.GetCurrentHappyEndingCount());
        SetInt(PapalElectionCountPath, records.GetCurrentPapalElectionCount());
        SetInt(PapalElectionFailedCountPath, records.GetCurrentPapalElectionFailedCount());
        SetInt(PopeGenerationPath, records.GetCurrentPopeGeneration());
        SetInt(ConclaveCountPath, records.GetCurrentConclaveCount());
    }

    private static void SetInt(string path, int value)
    {
        SetText(path, value.ToString());
    }

    private static void SetSeconds(string path, float seconds)
    {
        SetText(path, $"{seconds:F1}초");
    }

    private static void SetText(string path, string value)
    {
        TextMeshProUGUI text = FindText(path);

        if (text == null)
        {
            Debug.LogWarning($"[EndingResult] Text path was not found: {path}");
            return;
        }

        if (!OriginalTextsByPath.ContainsKey(path))
        {
            OriginalTextsByPath[path] = text.text;
        }

        text.text = $"{OriginalTextsByPath[path]} {value}";
    }

    private static TextMeshProUGUI FindText(string path)
    {
        GameObject target = GameObject.Find(path);
        return target != null ? target.GetComponent<TextMeshProUGUI>() : null;
    }
}
