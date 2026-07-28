using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class EventManager : MonoBehaviour
{
    [SerializeField] public List<Event> allEvents;

    private readonly HashSet<Event> appeared = new();
    private readonly Dictionary<Event, int> appearedCnt = new();
    private readonly Dictionary<string, ChoiceRecord> choiceRecords = new();
    private readonly Dictionary<int, float> plotDamageBonuses = new();

    private bool guaranteeNextPrayerOrSpeech;
    private bool freePlotPietyForCurrentConclave;

    private static readonly Dictionary<string, string> RequiredPreEventIds = new()
    {
        { "E11200", "E11100" },
        { "E11300", "E11200" },
        { "E21000", "E20000" },
        { "E21100", "E21000" },
        { "E21101", "E21100" },
        { "E31000", "E30000" },
        { "E31100", "E31000" },
        { "E31101", "E31100" },
        { "E31200", "E31000" },
        { "E31210", "E31200" },
        { "E31211", "E31210" },
        { "E31213", "E31212" },
        { "E32000", "E31213" },
        { "E32001", "E32000" },
        { "E32002", "E32000" }
    };

    private static readonly Dictionary<string, HashSet<string>> ConflictEventIds = new()
    {
        { "E21100", NewIdSet("E31100", "E31101", "E31200", "E31210", "E32000", "E32001") },
        { "E21101", NewIdSet("E31100", "E31101", "E31200", "E31210", "E32000", "E32001") },
        { "E31100", NewIdSet("E21000", "E21100", "E31200", "E31210", "E32000", "E32001") },
        { "E31101", NewIdSet("E21000", "E21100", "E31200", "E31210", "E32000", "E32001") },
        { "E31200", NewIdSet("E21000", "E21100", "E31100", "E31101", "E32000", "E32001") },
        { "E31210", NewIdSet("E21000", "E21100", "E31100", "E31101", "E32000", "E32001") },
        { "E31211", NewIdSet("E21000", "E21100", "E31100", "E31101", "E31200", "E32000", "E32001") },
        { "E31212", NewIdSet("E31213", "E32000", "E32001") },
        { "E32001", NewIdSet("E32002") },
        { "E32002", NewIdSet("E32001") }
    };

    private struct ChoiceRecord
    {
        public int optionIndex;
        public bool succeeded;
    }

    void Start()
    {
        if (InGameManager.Instance != null && InGameManager.Instance.Context != null)
        {
            InGameManager.Instance.Context.OnGameContextEvent += HandleGameContextEvent;
        }
    }

    void OnDestroy()
    {
        if (InGameManager.Instance != null && InGameManager.Instance.Context != null)
        {
            InGameManager.Instance.Context.OnGameContextEvent -= HandleGameContextEvent;
        }
    }

    public Event PickAnyEvent()
    {
        return PickWeightedEvent(allEvents == null ? new List<Event>() : allEvents.Where(e => e != null).ToList());
    }

    public Event GetNewEvent()
    {
        if (allEvents == null)
        {
            return null;
        }

        List<Event> eligibleEvents = allEvents
            .Where(e => e != null && HasRemaining(e) && PreEventSatisfied(e) &&
                        ConflictEventSatisfied(e) && NarrativeConditionSatisfied(e))
            .ToList();

        Event pickedEvent = PickWeightedEvent(eligibleEvents);
        if (pickedEvent == null)
        {
            Debug.LogWarning("[Event] 현재 조건을 만족하는 이벤트가 없습니다.");
            return null;
        }

        MarkEventAppeared(pickedEvent);
        Debug.Log($"이벤트 \"{pickedEvent.eventID}\" 선택");
        return pickedEvent;
    }

    public bool HasRemaining(Event e)
    {
        int count = 0;
        appearedCnt.TryGetValue(e, out count);

        return count < e.maxAppear;
    }

    public bool PreEventSatisfied(Event e)
    {
        if (RequiredPreEventIds.TryGetValue(e.eventID, out string requiredId))
        {
            return HasAppeared(requiredId);
        }

        var pres = e.preEvents;

        if(pres == null || pres.Count == 0) return true;

        foreach(var pre in pres)
        {
            if(!pre) continue;

            if (!appeared.Contains(pre))
            {
                return false;
            }
        }

        return true;
    }

    public void MarkEventAppeared(Event e)
    {
        appeared.Add(e);

        int count = 0;
        appearedCnt.TryGetValue(e, out count);

        appearedCnt[e] = count + 1;
    }

    public bool ConflictEventSatisfied(Event e)
    {
        if (ConflictEventIds.TryGetValue(e.eventID, out HashSet<string> conflictIds))
        {
            foreach (string conflictId in conflictIds)
            {
                if (HasAppeared(conflictId)) return false;
            }
            return true;
        }

        var conflicts = e.conflictEvents;

        if(conflicts == null || conflicts.Count == 0) return true;

        foreach(var conf in conflicts)
        {
            if(!conf) continue;

            if(appeared.Contains(conf)) return false;
        }

        return true;
    }

    public Event GetEventById(string eventId)
    {
        if (allEvents == null) return null;

        foreach(var e in allEvents)
        {
            if(!e) continue;

            if(e.eventID == eventId) return e;
        }

        return null;
    }

    public void InitEventManager()
    {
        appeared.Clear();
        appearedCnt.Clear();
        choiceRecords.Clear();
        plotDamageBonuses.Clear();
        ClearConclaveEffects();
    }

    public void RecordChoice(string eventId, int optionIndex, bool succeeded)
    {
        if (string.IsNullOrWhiteSpace(eventId)) return;
        choiceRecords[eventId] = new ChoiceRecord { optionIndex = optionIndex, succeeded = succeeded };

        Event chosenEvent = GetEventById(eventId);
        if (chosenEvent != null && !appeared.Contains(chosenEvent))
        {
            appeared.Add(chosenEvent);
            appearedCnt[chosenEvent] = Mathf.Max(1, appearedCnt.TryGetValue(chosenEvent, out int count) ? count : 0);
        }
    }

    public bool WasChoice(string eventId, int optionIndex, bool? succeeded = null)
    {
        if (!choiceRecords.TryGetValue(eventId, out ChoiceRecord record) || record.optionIndex != optionIndex)
        {
            return false;
        }

        return !succeeded.HasValue || record.succeeded == succeeded.Value;
    }

    public void SetPlotDamageBonus(int candidateNumber, float bonus)
    {
        plotDamageBonuses[candidateNumber] = bonus;
    }

    public float GetPlotDamageBonus(int candidateNumber)
    {
        return plotDamageBonuses.TryGetValue(candidateNumber, out float bonus) ? bonus : 0f;
    }

    public float ModifyPlotHpDelta(Cardinal performer, Cardinal target, float delta)
    {
        if (delta >= 0f || performer == null || target == null || !performer.CompareTag("Player"))
        {
            return delta;
        }

        int candidateNumber = GetCandidateNumber(target);
        return candidateNumber > 0 ? delta - GetPlotDamageBonus(candidateNumber) : delta;
    }

    public void GuaranteeNextPrayerOrSpeech()
    {
        guaranteeNextPrayerOrSpeech = true;
    }

    public bool TryConsumeGuaranteedPrayerOrSpeech(Cardinal performer)
    {
        if (performer == null || !performer.CompareTag("Player")) return false;
        if (!guaranteeNextPrayerOrSpeech) return false;
        guaranteeNextPrayerOrSpeech = false;
        return true;
    }

    public void SetFreePlotPietyForCurrentConclave()
    {
        freePlotPietyForCurrentConclave = true;
    }

    public bool FreePlotPietyForCurrentConclave => freePlotPietyForCurrentConclave;

    public bool IsPlotPietyCostWaived(Cardinal performer)
    {
        return freePlotPietyForCurrentConclave && performer != null && performer.CompareTag("Player");
    }

    public EventManagerSaveData CaptureSaveData()
    {
        EventManagerSaveData saveData = new EventManagerSaveData();

        foreach (var pair in appearedCnt)
        {
            if (pair.Key == null)
            {
                continue;
            }

            saveData.records.Add(new EventRecordSaveData
            {
                eventId = pair.Key.eventID,
                appearCount = pair.Value
            });
        }

        foreach (var pair in choiceRecords.OrderBy(pair => pair.Key))
        {
            saveData.choices.Add(new EventChoiceSaveData
            {
                eventId = pair.Key,
                optionIndex = pair.Value.optionIndex,
                succeeded = pair.Value.succeeded
            });
        }

        foreach (var pair in plotDamageBonuses.OrderBy(pair => pair.Key))
        {
            saveData.plotDamageBonuses.Add(new EventPlotDamageBonusSaveData
            {
                candidateNumber = pair.Key,
                bonus = pair.Value
            });
        }

        saveData.guaranteeNextPrayerOrSpeech = guaranteeNextPrayerOrSpeech;
        saveData.freePlotPietyForCurrentConclave = freePlotPietyForCurrentConclave;

        return saveData;
    }

    public void RestoreFromSave(EventManagerSaveData saveData)
    {
        appeared.Clear();
        appearedCnt.Clear();
        choiceRecords.Clear();
        plotDamageBonuses.Clear();
        ClearConclaveEffects();

        if (saveData == null)
        {
            return;
        }

        if (saveData.records != null)
        {
            foreach (var record in saveData.records)
            {
                if (record == null || string.IsNullOrWhiteSpace(record.eventId))
                {
                    continue;
                }

                Event restoredEvent = GetEventById(record.eventId);
                if (restoredEvent == null)
                {
                    Debug.LogWarning($"[Save] 이벤트 '{record.eventId}'를 찾지 못해 복원을 건너뜁니다.");
                    continue;
                }

                appeared.Add(restoredEvent);
                appearedCnt[restoredEvent] = Mathf.Max(0, record.appearCount);
            }
        }

        if (saveData.choices != null)
        {
            foreach (var choice in saveData.choices)
            {
                if (choice == null || string.IsNullOrWhiteSpace(choice.eventId) ||
                    choice.optionIndex < 1 || choice.optionIndex > 2)
                {
                    continue;
                }

                Event restoredEvent = GetEventById(choice.eventId);
                if (restoredEvent == null)
                {
                    Debug.LogWarning($"[Save] 선택 결과 이벤트 '{choice.eventId}'를 찾지 못해 복원을 건너뜁니다.");
                    continue;
                }

                choiceRecords[choice.eventId] = new ChoiceRecord
                {
                    optionIndex = choice.optionIndex,
                    succeeded = choice.succeeded
                };

                appeared.Add(restoredEvent);
                if (!appearedCnt.ContainsKey(restoredEvent))
                {
                    appearedCnt[restoredEvent] = 1;
                }
            }
        }

        if (saveData.plotDamageBonuses != null)
        {
            foreach (var plotBonus in saveData.plotDamageBonuses)
            {
                if (plotBonus == null || plotBonus.candidateNumber < 1 || plotBonus.candidateNumber > 3 ||
                    float.IsNaN(plotBonus.bonus) || float.IsInfinity(plotBonus.bonus))
                {
                    continue;
                }

                plotDamageBonuses[plotBonus.candidateNumber] = Mathf.Max(0f, plotBonus.bonus);
            }
        }

        guaranteeNextPrayerOrSpeech = saveData.guaranteeNextPrayerOrSpeech;
        freePlotPietyForCurrentConclave = saveData.freePlotPietyForCurrentConclave;
    }

    private Event PickWeightedEvent(List<Event> candidates)
    {
        if (candidates == null || candidates.Count == 0) return null;

        List<Event> forced = candidates.Where(e => float.IsPositiveInfinity(GetSelectionWeight(e))).ToList();
        if (forced.Count > 0)
        {
            return forced[Random.Range(0, forced.Count)];
        }

        float totalWeight = 0f;
        foreach (Event candidate in candidates)
        {
            totalWeight += Mathf.Max(0f, GetSelectionWeight(candidate));
        }

        if (totalWeight <= 0f) return null;

        float roll = Random.value * totalWeight;
        float accumulated = 0f;
        foreach (Event candidate in candidates)
        {
            accumulated += Mathf.Max(0f, GetSelectionWeight(candidate));
            if (roll <= accumulated) return candidate;
        }

        return candidates[candidates.Count - 1];
    }

    private float GetSelectionWeight(Event e)
    {
        switch (e.eventID)
        {
            case "E11200":
            case "E11300":
            case "E21101":
                return float.PositiveInfinity;
            default:
                return e.GetEventWeight();
        }
    }

    private bool NarrativeConditionSatisfied(Event e)
    {
        switch (e.eventID)
        {
            case "E21100": return WasChoice("E21000", 1, true);
            case "E31100": return IsCandidateEliminated(3);
            case "E31101": return WasChoice("E31100", 1, true);
            case "E31200": return IsCandidateEliminated(2) && !IsCandidateEliminated(1);
            // 기획 행의 선행(E31200)과 서술을 함께 만족시키는 분기다.
            case "E31210": return WasChoice("E31200", 1, true);
            case "E31211": return !IsCandidateEliminated(1) && WasChoice("E31210", 2, false);
            case "E32000": return WasChoice("E31213", 2, true);
            case "E32001": return WasChoice("E32000", 1, false);
            case "E32002": return !WasChoice("E32000", 1, false);
            default: return true;
        }
    }

    private bool HasAppeared(string eventId)
    {
        Event evt = GetEventById(eventId);
        return evt != null && appeared.Contains(evt);
    }

    private bool IsCandidateEliminated(int candidateNumber)
    {
        if (CardinalManager.Instance == null) return false;

        StatsUI statsUI = CardinalManager.Instance.StatsUI;
        Cardinal[] linked = statsUI != null ? statsUI.LinkedCardinals : null;
        Cardinal candidate = linked != null && linked.Length > candidateNumber
            ? linked[candidateNumber]
            : null;

        if (candidate == null)
        {
            List<Cardinal> ai = CardinalManager.Instance.GetAICardinlas();
            int index = candidateNumber - 1;
            candidate = index < ai.Count ? ai[index] : null;
        }

        return candidate != null && (candidate.Hp <= 0f || candidate.IsKnockedOut);
    }

    private int GetCandidateNumber(Cardinal target)
    {
        if (target == null || CardinalManager.Instance == null) return 0;

        StatsUI statsUI = CardinalManager.Instance.StatsUI;
        Cardinal[] linked = statsUI != null ? statsUI.LinkedCardinals : null;
        if (linked != null)
        {
            for (int candidateNumber = 1; candidateNumber <= 3 && candidateNumber < linked.Length; candidateNumber++)
            {
                if (linked[candidateNumber] == target) return candidateNumber;
            }
        }

        List<Cardinal> ai = CardinalManager.Instance.GetAICardinlas();
        for (int index = 0; index < 3 && index < ai.Count; index++)
        {
            if (ai[index] == target) return index + 1;
        }

        return 0;
    }

    private void HandleGameContextEvent(GameContext.GameContextEvent eventType)
    {
        if (eventType == GameContext.GameContextEvent.ConclaveEnd)
        {
            ClearConclaveEffects();
        }
    }

    private void ClearConclaveEffects()
    {
        plotDamageBonuses.Clear();
        guaranteeNextPrayerOrSpeech = false;
        freePlotPietyForCurrentConclave = false;
    }

    private static HashSet<string> NewIdSet(params string[] ids)
    {
        return new HashSet<string>(ids);
    }
}
