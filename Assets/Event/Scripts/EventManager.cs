using System.Collections.Generic;
using System.Linq;
using Habemus.Events;
using UnityEngine;

public class EventManager : MonoBehaviour
{
    private const int CurrentScheduleVersion = 3;

    [SerializeField] public List<Event> allEvents;

    private readonly HashSet<Event> appeared = new();
    private readonly Dictionary<Event, int> appearedCnt = new();
    private readonly Dictionary<string, ChoiceRecord> choiceRecords = new();
    private readonly Dictionary<int, float> plotDamageBonuses = new();
    private readonly List<PendingGuaranteedEventSaveData> pendingGuaranteedEvents = new();

    private bool subEventOccurredThisTurn;
    private bool guaranteeNextPrayerOrSpeech;
    private bool freePlotPietyForCurrentConclave;

    private struct ChoiceRecord
    {
        public int optionIndex;
        public bool succeeded;
    }

    void Start()
    {
        EventTrigger.ValidateRules();
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
        return PickUniformEvent(allEvents == null ? new List<Event>() : allEvents.Where(e => e != null).ToList());
    }

    public Event GetNewEvent()
    {
        return InGameManager.Instance == null ? null : GetEventForPosition(new EventTriggerContext(
            InGameManager.Instance.GetCurrentDay(),
            InGameManager.Instance.GetCurrentConclave(),
            InGameManager.Instance.GetCurrentTurn(),
            InGameManager.Instance.GetCurrentTurnPhase()));
    }

    public Event GetEventForPosition(EventTriggerContext context)
    {
        Event pending = TakePendingEvent(PendingEventTiming.NextEnteredPosition);
        if (pending != null) return pending;

        Event branch = PickCandidateBranch(context);
        if (branch != null) return branch;

        List<TriggerCandidate> candidates = GetEligibleCandidates(context);
        Event guaranteed = candidates
            .Where(candidate => candidate.trigger.Tier == EventTier.GuaranteedStory)
            .OrderBy(candidate => candidate.evt.eventID)
            .Select(candidate => candidate.evt)
            .FirstOrDefault();
        if (guaranteed != null) return SelectEvent(guaranteed);

        List<TriggerCandidate> stories = candidates
            .Where(candidate => candidate.trigger.Tier == EventTier.Story)
            .OrderBy(candidate => candidate.evt.eventID)
            .ToList();
        List<Event> subEvents = subEventOccurredThisTurn
            ? new List<Event>()
            : candidates.Where(candidate => candidate.trigger.Tier == EventTier.Sub)
                .Select(candidate => candidate.evt)
                .ToList();

        float storyTotal = stories.Sum(candidate => candidate.trigger.GetChance());
        float roll = Random.value;
        float accumulated = 0f;

        if (storyTotal > 1f)
        {
            foreach (TriggerCandidate candidate in stories)
            {
                accumulated += candidate.trigger.GetChance() / storyTotal;
                if (roll <= accumulated) return SelectEvent(candidate.evt);
            }

            return stories.Count > 0 ? SelectEvent(stories[stories.Count - 1].evt) : null;
        }

        foreach (TriggerCandidate candidate in stories)
        {
            accumulated += candidate.trigger.GetChance();
            if (roll <= accumulated) return SelectEvent(candidate.evt);
        }

        if (subEvents.Count == 0) return null;
        Event subEvent = PickUniformEvent(subEvents);
        subEventOccurredThisTurn = subEvent != null;
        return SelectEvent(subEvent);
    }

    public Event GetChainedEvent()
    {
        return TakePendingEvent(PendingEventTiming.AfterCurrentEvent);
    }

    public Event GetStartOfDayEvent()
    {
        return GetNewEvent();
    }

    public Event GetFirstPlotTutorialEvent()
    {
        if (!HasAppeared("E11100")) return null;
        Event tutorial = GetEventById("E11200");
        return tutorial != null && HasRemaining(tutorial) ? SelectEvent(tutorial) : null;
    }

    public bool HasRemaining(Event evt)
    {
        return evt != null && (!appearedCnt.TryGetValue(evt, out int count) || count < 1);
    }

    public bool HasAppeared(string eventId)
    {
        Event evt = GetEventById(eventId);
        return evt != null && appeared.Contains(evt);
    }

    public bool HasPendingEvent(string eventId)
    {
        return pendingGuaranteedEvents.Any(pending => pending != null && pending.eventId == eventId);
    }

    public bool IsCandidateBranchAvailable()
    {
        return HasAppeared("E31000") && !HasAppeared("E31100") && !HasAppeared("E31200") &&
            !HasPendingEvent("E31100") && !HasPendingEvent("E31200");
    }

    public Event GetEventById(string eventId)
    {
        if (allEvents == null) return null;
        return allEvents.FirstOrDefault(evt => evt != null && evt.eventID == eventId);
    }

    public void InitEventManager()
    {
        appeared.Clear();
        appearedCnt.Clear();
        choiceRecords.Clear();
        plotDamageBonuses.Clear();
        pendingGuaranteedEvents.Clear();
        subEventOccurredThisTurn = false;
        ClearConclaveEffects();
    }

    public void RecordChoice(string eventId, int optionIndex, bool succeeded)
    {
        if (string.IsNullOrWhiteSpace(eventId)) return;

        Event chosenEvent = GetEventById(eventId);
        if (chosenEvent != null && !appeared.Contains(chosenEvent)) MarkEventAppeared(chosenEvent);

        if (choiceRecords.TryGetValue(eventId, out ChoiceRecord existing) &&
            existing.optionIndex == optionIndex && existing.succeeded == succeeded)
        {
            return;
        }

        choiceRecords[eventId] = new ChoiceRecord { optionIndex = optionIndex, succeeded = succeeded };

        switch (eventId)
        {
            case "E11100":
                QueueGuaranteedEvent("E11200", PendingEventTiming.AfterCurrentEvent);
                break;
            case "E31000":
                QueueCandidateBranchIfReady();
                break;
            case "E31210" when optionIndex == 2 && !succeeded && !IsCandidateEliminated(1):
                QueueGuaranteedEvent("E31211", PendingEventTiming.NextEnteredPosition);
                break;
            case "E32000" when optionIndex == 1 && !succeeded:
                QueueGuaranteedEvent("E32001", PendingEventTiming.NextEnteredPosition);
                break;
            case "E32001" when optionIndex == 1:
                QueueGuaranteedEvent("E32002", PendingEventTiming.NextEnteredPosition);
                break;
            case "E32001" when optionIndex == 2:
                CancelPendingEvent("E32002");
                break;
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

    public bool TryGetTemporaryFlagValue(string flagId, out int value)
    {
        value = 0;
        switch (flagId)
        {
            case "F10000":
                value = WasChoice("E20000", 1, true) ? 1 : 0;
                return value != 0;
            case "F10001":
                value = WasChoice("E21101", 1) || WasChoice("E31100", 2) ||
                    WasChoice("E31211", 1) || WasChoice("E32001", 1) ? 1 : 0;
                return value != 0;
            case "F10011": return TryGetRecordedOption("E21100", out value);
            case "F20000": return TryGetBooleanChoice("E30000", 1, out value);
            case "F21000": return TryGetRecordedOption("E31000", out value);
            case "F21100": return TryGetBooleanChoice("E31100", 1, out value);
            case "F21110": return TryGetRecordedOption("E31101", out value);
            case "F21200": return TryGetBooleanChoice("E31200", 1, out value);
            case "F21210":
                value = WasChoice("E31210", 2, false) ? 1 : 0;
                return value != 0;
            case "F30000":
                if (!choiceRecords.TryGetValue("E31212", out ChoiceRecord ascension) || ascension.optionIndex != 1)
                    return false;
                value = ascension.succeeded ? 1 : 0;
                return true;
            case "F31000": return TryGetBooleanChoice("E31213", 2, out value);
            case "F31100": return TryGetBooleanChoice("E32000", 1, out value);
            case "F31110": return TryGetBooleanChoice("E32001", 2, out value);
            default: return false;
        }
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
        if (delta >= 0f || performer == null || target == null || !performer.CompareTag("Player")) return delta;
        int candidateNumber = GetCandidateNumber(target);
        return candidateNumber > 0 ? delta - GetPlotDamageBonus(candidateNumber) : delta;
    }

    public void GuaranteeNextPrayerOrSpeech()
    {
        guaranteeNextPrayerOrSpeech = true;
    }

    public bool TryConsumeGuaranteedPrayerOrSpeech(Cardinal performer)
    {
        if (performer == null || !performer.CompareTag("Player") || !guaranteeNextPrayerOrSpeech) return false;
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
        EventManagerSaveData saveData = new EventManagerSaveData
        {
            scheduleVersion = CurrentScheduleVersion,
            subEventOccurredThisTurn = subEventOccurredThisTurn,
            guaranteeNextPrayerOrSpeech = guaranteeNextPrayerOrSpeech,
            freePlotPietyForCurrentConclave = freePlotPietyForCurrentConclave
        };

        foreach (var pair in appearedCnt)
        {
            if (pair.Key == null) continue;
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

        foreach (PendingGuaranteedEventSaveData pending in pendingGuaranteedEvents)
        {
            if (pending == null || string.IsNullOrWhiteSpace(pending.eventId)) continue;
            saveData.pendingGuaranteedEvents.Add(new PendingGuaranteedEventSaveData
            {
                eventId = pending.eventId,
                timing = pending.timing
            });
        }

        return saveData;
    }

    public void RestoreFromSave(EventManagerSaveData saveData)
    {
        appeared.Clear();
        appearedCnt.Clear();
        choiceRecords.Clear();
        plotDamageBonuses.Clear();
        pendingGuaranteedEvents.Clear();
        subEventOccurredThisTurn = false;
        ClearConclaveEffects();

        if (saveData == null) return;

        RestoreEventRecords(saveData.records);
        RestoreChoiceRecords(saveData.choices);
        RestorePlotBonuses(saveData.plotDamageBonuses);

        if (saveData.scheduleVersion >= CurrentScheduleVersion && saveData.pendingGuaranteedEvents != null)
        {
            foreach (PendingGuaranteedEventSaveData pending in saveData.pendingGuaranteedEvents)
            {
                if (pending == null || string.IsNullOrWhiteSpace(pending.eventId) ||
                    GetEventById(pending.eventId) == null || HasAppeared(pending.eventId)) continue;

                pendingGuaranteedEvents.Add(new PendingGuaranteedEventSaveData
                {
                    eventId = pending.eventId,
                    timing = pending.timing
                });
            }

            subEventOccurredThisTurn = saveData.subEventOccurredThisTurn;
        }

        guaranteeNextPrayerOrSpeech = saveData.guaranteeNextPrayerOrSpeech;
        freePlotPietyForCurrentConclave = saveData.freePlotPietyForCurrentConclave;
    }

    public bool IsCandidateEliminated(int candidateNumber)
    {
        if (CardinalManager.Instance == null) return false;

        StatsUI statsUI = CardinalManager.Instance.StatsUI;
        Cardinal[] linked = statsUI != null ? statsUI.LinkedCardinals : null;
        Cardinal candidate = linked != null && linked.Length > candidateNumber ? linked[candidateNumber] : null;

        if (candidate == null)
        {
            List<Cardinal> ai = CardinalManager.Instance.GetAICardinlas();
            int index = candidateNumber - 1;
            candidate = index >= 0 && index < ai.Count ? ai[index] : null;
        }

        return candidate != null && (candidate.Hp <= 0f || candidate.IsKnockedOut);
    }

    private List<TriggerCandidate> GetEligibleCandidates(EventTriggerContext context)
    {
        List<TriggerCandidate> candidates = new();
        if (allEvents == null) return candidates;

        foreach (Event evt in allEvents)
        {
            if (evt == null || !HasRemaining(evt)) continue;
            EventTrigger trigger = new EventTrigger(evt.eventID);
            if (trigger.Tier == EventTier.GuaranteedChain || !trigger.IsEligible(context, this)) continue;
            candidates.Add(new TriggerCandidate(evt, trigger));
        }

        return candidates;
    }

    private Event PickCandidateBranch(EventTriggerContext context)
    {
        Event event311 = GetEventById("E31100");
        Event event312 = GetEventById("E31200");
        bool can311 = event311 != null && new EventTrigger("E31100").IsEligible(context, this);
        bool can312 = event312 != null && new EventTrigger("E31200").IsEligible(context, this);
        if (!can311 && !can312) return null;

        Event picked = can311 && can312 ? (Random.value < 0.5f ? event311 : event312) : can311 ? event311 : event312;
        return SelectEvent(picked);
    }

    private void QueueCandidateBranchIfReady()
    {
        if (!IsCandidateBranchAvailable()) return;
        bool can311 = IsCandidateEliminated(3);
        bool can312 = IsCandidateEliminated(2) && !IsCandidateEliminated(1);
        if (!can311 && !can312) return;

        string eventId = can311 && can312 ? (Random.value < 0.5f ? "E31100" : "E31200")
            : can311 ? "E31100" : "E31200";
        QueueGuaranteedEvent(eventId, PendingEventTiming.NextEnteredPosition);
    }

    private void QueueGuaranteedEvent(string eventId, PendingEventTiming timing)
    {
        if (string.IsNullOrWhiteSpace(eventId) || HasAppeared(eventId) || HasPendingEvent(eventId)) return;
        pendingGuaranteedEvents.Add(new PendingGuaranteedEventSaveData { eventId = eventId, timing = timing });
    }

    private void CancelPendingEvent(string eventId)
    {
        pendingGuaranteedEvents.RemoveAll(pending => pending != null && pending.eventId == eventId);
    }

    private Event TakePendingEvent(PendingEventTiming timing)
    {
        for (int index = 0; index < pendingGuaranteedEvents.Count; index++)
        {
            PendingGuaranteedEventSaveData pending = pendingGuaranteedEvents[index];
            if (pending == null || pending.timing != timing) continue;
            pendingGuaranteedEvents.RemoveAt(index--);
            if (string.IsNullOrWhiteSpace(pending.eventId) || HasAppeared(pending.eventId)) continue;

            Event evt = GetEventById(pending.eventId);
            if (evt != null) return SelectEvent(evt);
            Debug.LogWarning($"[Event] 대기 이벤트 '{pending.eventId}'가 등록되지 않았습니다.");
        }

        return null;
    }

    private Event SelectEvent(Event evt)
    {
        if (evt == null) return null;
        MarkEventAppeared(evt);
        Debug.Log($"[Event] 이벤트 '{evt.eventID}' 선택");
        return evt;
    }

    private void MarkEventAppeared(Event evt)
    {
        if (evt == null) return;
        appeared.Add(evt);
        appearedCnt[evt] = appearedCnt.TryGetValue(evt, out int count) ? count + 1 : 1;
    }

    private static Event PickUniformEvent(List<Event> candidates)
    {
        if (candidates == null || candidates.Count == 0) return null;
        return candidates[Random.Range(0, candidates.Count)];
    }

    private bool TryGetRecordedOption(string eventId, out int optionIndex)
    {
        optionIndex = 0;
        if (!choiceRecords.TryGetValue(eventId, out ChoiceRecord record)) return false;
        optionIndex = record.optionIndex;
        return true;
    }

    private bool TryGetBooleanChoice(string eventId, int expectedOption, out int value)
    {
        value = WasChoice(eventId, expectedOption) ? 1 : 0;
        return value != 0;
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

    private void RestoreEventRecords(List<EventRecordSaveData> records)
    {
        if (records == null) return;
        foreach (EventRecordSaveData record in records)
        {
            if (record == null || string.IsNullOrWhiteSpace(record.eventId)) continue;
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

    private void RestoreChoiceRecords(List<EventChoiceSaveData> choices)
    {
        if (choices == null) return;
        foreach (EventChoiceSaveData choice in choices)
        {
            if (choice == null || string.IsNullOrWhiteSpace(choice.eventId) ||
                choice.optionIndex < 1 || choice.optionIndex > 2) continue;

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
            if (!appearedCnt.ContainsKey(restoredEvent)) appearedCnt[restoredEvent] = 1;
        }
    }

    private void RestorePlotBonuses(List<EventPlotDamageBonusSaveData> bonuses)
    {
        if (bonuses == null) return;
        foreach (EventPlotDamageBonusSaveData bonus in bonuses)
        {
            if (bonus == null || bonus.candidateNumber < 1 || bonus.candidateNumber > 3 ||
                float.IsNaN(bonus.bonus) || float.IsInfinity(bonus.bonus)) continue;
            plotDamageBonuses[bonus.candidateNumber] = Mathf.Max(0f, bonus.bonus);
        }
    }

    private void HandleGameContextEvent(GameContext.GameContextEvent eventType)
    {
        if (eventType == GameContext.GameContextEvent.TurnStart) subEventOccurredThisTurn = false;
        if (eventType == GameContext.GameContextEvent.ConclaveEnd) ClearConclaveEffects();
    }

    private void ClearConclaveEffects()
    {
        plotDamageBonuses.Clear();
        guaranteeNextPrayerOrSpeech = false;
        freePlotPietyForCurrentConclave = false;

        if (CardinalManager.Instance == null) return;
        foreach (Cardinal cardinal in CardinalManager.Instance.Cardinals)
        {
            if (cardinal == null) continue;
            cardinal.SetMinHpOneEffect("E50600", false);
            cardinal.SetMinHpOneEffect("P030", false);
        }
    }

    private readonly struct TriggerCandidate
    {
        public Event evt { get; }
        public EventTrigger trigger { get; }

        public TriggerCandidate(Event evt, EventTrigger trigger)
        {
            this.evt = evt;
            this.trigger = trigger;
        }
    }
}
