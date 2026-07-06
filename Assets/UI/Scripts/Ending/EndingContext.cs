using System.Collections.Generic;
using UnityEngine;

public static class EndingContext
{
    private static readonly List<string> RankedCandidateNames = new List<string>();

    public static string TriggerEventId { get; private set; } = string.Empty;
    public static int SelectedOptionIndex { get; private set; }
    public static string PlayerName { get; private set; } = string.Empty;
    public static string ElectedNpcName { get; private set; } = string.Empty;
    public static float PlayerPiety { get; private set; }
    public static float PlayerInfluence { get; private set; }
    public static int ConclaveDay { get; private set; }

    public static void CaptureFromCurrentGame(Cardinal electedCandidate = null)
    {
        CaptureNames();
        CapturePlayerStats();
        CaptureRankings();
        CaptureConclaveDay();

        if (electedCandidate != null && !electedCandidate.CompareTag("Player"))
        {
            ElectedNpcName = electedCandidate.name;
        }
    }

    public static void SetEventTrigger(string eventId, int selectedOptionIndex)
    {
        TriggerEventId = string.IsNullOrWhiteSpace(eventId) ? string.Empty : eventId.Trim();
        SelectedOptionIndex = selectedOptionIndex;
    }

    public static string GetRankedCandidateName(int rankIndex)
    {
        if (rankIndex >= 0 && rankIndex < RankedCandidateNames.Count)
        {
            return RankedCandidateNames[rankIndex];
        }

        return $"후보 {rankIndex + 1}";
    }

    public static void Clear()
    {
        TriggerEventId = string.Empty;
        SelectedOptionIndex = 0;
        PlayerName = string.Empty;
        ElectedNpcName = string.Empty;
        PlayerPiety = 0f;
        PlayerInfluence = 0f;
        ConclaveDay = 0;
        RankedCandidateNames.Clear();
    }

    private static void CaptureNames()
    {
        if (SaveManager.Instance == null || SaveManager.Instance.CurrentGameNames == null)
        {
            return;
        }

        GameNameSaveData names = SaveManager.Instance.CurrentGameNames;
        PlayerName = names.playerName;

        if (string.IsNullOrWhiteSpace(ElectedNpcName) && names.npcNames != null && names.npcNames.Count > 0)
        {
            ElectedNpcName = names.npcNames[0];
        }
    }

    private static void CapturePlayerStats()
    {
        Cardinal player = FindPlayerCardinal();
        if (player == null)
        {
            return;
        }

        PlayerPiety = player.Piety;
        PlayerInfluence = player.Influence;
    }

    private static void CaptureRankings()
    {
        RankedCandidateNames.Clear();

        if (CardinalManager.Instance == null || CardinalManager.Instance.Cardinals == null)
        {
            return;
        }

        List<Cardinal> candidates = new List<Cardinal>();
        foreach (Cardinal cardinal in CardinalManager.Instance.Cardinals)
        {
            if (cardinal != null)
            {
                candidates.Add(cardinal);
            }
        }

        candidates.Sort((left, right) =>
        {
            float leftScore = left.Piety + left.Influence;
            float rightScore = right.Piety + right.Influence;
            return rightScore.CompareTo(leftScore);
        });

        foreach (Cardinal candidate in candidates)
        {
            RankedCandidateNames.Add(candidate.name);
        }
    }

    private static void CaptureConclaveDay()
    {
        if (InGameManager.Instance != null && InGameManager.Instance.Context != null)
        {
            ConclaveDay = InGameManager.Instance.Context.CurrentDay;
        }
    }

    private static Cardinal FindPlayerCardinal()
    {
        if (CardinalManager.Instance == null || CardinalManager.Instance.Cardinals == null)
        {
            return null;
        }

        foreach (Cardinal cardinal in CardinalManager.Instance.Cardinals)
        {
            if (cardinal != null && cardinal.CompareTag("Player"))
            {
                return cardinal;
            }
        }

        return null;
    }
}
