using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class PlotSet
{
    public Plot[] plots = new Plot[3];
    public bool[] isUsed = new bool[3];

    public PlotSet(Plot[] plots)
    {
        for(int i = 0; i < 3; i++)
        {
            this.plots[i] = plots[i];
            this.isUsed[i] = plots[i] == null;
        }
    }

    public void use(int slot)
    {
        isUsed[slot] = true;
    }

    public bool isAllUsed()
    {
        return isUsed[0] && isUsed[1] && isUsed[2];
    }
}


public class PlotManager : MonoBehaviour
{
    public static PlotManager Instance { get; private set; }

    [SerializeField] private PlotUI plotUI;

    [Header("공작 SO 리스트")]
    [SerializeField] private List<Plot> plots;

    private PlotSet[] availPlotSets = new PlotSet[2];
    private int activePlotDay;

    private List<Plot> usedPlots;

    private Cardinal performer;

    public PlotSet[] AvailPlotSets => availPlotSets;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        usedPlots = new List<Plot>();
    }
    void Start()
    {
        if (InGameManager.Instance != null && InGameManager.Instance.Context != null)
        {
            InGameManager.Instance.Context.OnGameContextEvent += OnGameContextChanged;
        }
    }

    void OnDestroy()
    {
        if (InGameManager.Instance != null && InGameManager.Instance.Context != null)
        {
            InGameManager.Instance.Context.OnGameContextEvent -= OnGameContextChanged;
        }
    }

    public PlotSet GeneratePlotSet()
    {
        return GeneratePlotSet(null);
    }

    private PlotSet GeneratePlotSet(ICollection<Plot> excludedPlots)
    {
        float playerInfluence = GetPlayerInfluence();
        Plot[] selectedPlots = new Plot[3];

        for (int slot = 0; slot < selectedPlots.Length; slot++)
        {
            selectedPlots[slot] = PickSlotPlot(
                RollPreferredGrade(slot), playerInfluence, selectedPlots, excludedPlots);
        }

        return new PlotSet(selectedPlots);
    }

    private static PlotGrade RollPreferredGrade(int slot)
    {
        return slot == 1
            ? (Random.value < 0.9f ? PlotGrade.Rare : PlotGrade.Legendary)
            : (Random.value < 0.6f ? PlotGrade.Common : PlotGrade.Rare);
    }

    private float GetPlayerInfluence()
    {
        if (CardinalManager.Instance == null) return 0f;
        Cardinal player = CardinalManager.Instance.Cardinals.FirstOrDefault(
            candidate => candidate != null && candidate.CompareTag("Player"));
        return player != null ? player.Influence : 0f;
    }

    private Plot PickSlotPlot(PlotGrade preferredGrade, float playerInfluence, Plot[] alreadySelected,
        ICollection<Plot> excludedPlots)
    {
        int conditionPenalty = InGameManager.Instance != null && InGameManager.Instance.IsNpcCandidateLeading(2) ? 1 : 0;
        List<Plot> available = GetAvailablePlots(alreadySelected, excludedPlots);
        List<Plot> reachable = available.Where(plot =>
            plot.GetInfluenceRequirement() + conditionPenalty <= playerInfluence + 2f).ToList();
        if (reachable.Count > 0) available = reachable;

        if (available.Count == 0 && excludedPlots != null && excludedPlots.Count > 0)
        {
            available = GetAvailablePlots(alreadySelected, null);
        }

        return PickWeightedPlot(available, preferredGrade, playerInfluence, conditionPenalty);
    }

    private List<Plot> GetAvailablePlots(Plot[] alreadySelected, ICollection<Plot> excludedPlots)
    {
        return plots.Where(plot => plot != null && IsSupportedByTurnSystem(plot) &&
            !usedPlots.Contains(plot) && !alreadySelected.Contains(plot) &&
            (excludedPlots == null || !excludedPlots.Contains(plot))).ToList();
    }

    private static Plot PickWeightedPlot(List<Plot> candidates, PlotGrade preferredGrade,
        float playerInfluence, int conditionPenalty)
    {
        if (candidates == null || candidates.Count == 0) return null;

        float weightSum = candidates.Sum(plot => GetSelectionWeight(
            plot, preferredGrade, playerInfluence, conditionPenalty));
        float roll = Random.Range(0f, weightSum);

        foreach (Plot candidate in candidates)
        {
            roll -= GetSelectionWeight(candidate, preferredGrade, playerInfluence, conditionPenalty);
            if (roll <= 0f) return candidate;
        }

        return candidates[candidates.Count - 1];
    }

    private static float GetSelectionWeight(Plot plot, PlotGrade preferredGrade,
        float playerInfluence, int conditionPenalty)
    {
        int requirement = plot.GetInfluenceRequirement() + conditionPenalty;
        float gradeWeight = plot.plotGrade == preferredGrade ? 3f : 1f;
        float requirementWeight = requirement <= playerInfluence ? 4f :
            requirement <= playerInfluence + 2f ? 1f : 0.25f;
        return Mathf.Max(0.01f, plot.GetPlotWeight()) * gradeWeight * requirementWeight;
    }

    private static bool IsSupportedByTurnSystem(Plot plot)
    {
        // P019는 기획 결정만으로 대상과 적용 시점을 확정할 수 없어 부분 소비를 막는다.
        return plot.plotID != "P019";
    }

    public bool MeetsEffectiveInfluenceCondition(Plot plot, Cardinal candidate)
    {
        if (plot == null || candidate == null) return false;
        return candidate.Influence >= GetEffectiveInfluenceRequirement(plot, candidate);
    }

    public int GetEffectiveInfluenceRequirement(Plot plot, Cardinal candidate)
    {
        if (plot == null) return 0;
        int penalty = candidate != null && candidate.CompareTag("Player") && InGameManager.Instance != null &&
            InGameManager.Instance.IsNpcCandidateLeading(2) ? 1 : 0;
        return plot.GetInfluenceRequirement() + penalty;
    }

    public string GetEffectiveConditionText(Plot plot, Cardinal candidate)
    {
        if (plot == null) return string.Empty;
        int baseRequirement = plot.GetInfluenceRequirement();
        int effectiveRequirement = GetEffectiveInfluenceRequirement(plot, candidate);
        return effectiveRequirement == baseRequirement
            ? plot.plotCondiText
            : $"<sprite name=influence>{effectiveRequirement}<sprite name=up>";
    }

    public void RefreshPlotManager()
    {
        usedPlots.Clear();
    }

    // 콘클라베 시작 시 새로운 공작 Set 생성
    private void OnGameContextChanged(GameContext.GameContextEvent eventType)
    {
        if (eventType == GameContext.GameContextEvent.ConclaveStart)
        {
            int currentDay = InGameManager.Instance.Context.CurrentDay;
            if (activePlotDay == currentDay && availPlotSets.All(plotSet => plotSet != null)) return;

            HashSet<Plot> previousDayPlots = new HashSet<Plot>(availPlotSets
                .Where(plotSet => plotSet != null)
                .SelectMany(plotSet => plotSet.plots)
                .Where(plot => plot != null));
            activePlotDay = currentDay;
            availPlotSets[0] = GeneratePlotSet(previousDayPlots);
            availPlotSets[1] = GeneratePlotSet(previousDayPlots);
        }
    }

    public void InitializePlotSession(Cardinal performer, StateController schemerState)
    {
        if (InGameManager.Instance != null && !InGameManager.Instance.CanPerformPlayerAction(performer)) return;
        this.performer = performer;

        plotUI.ShowPlotUI(performer, schemerState);
    }

    public void UsePlot(int plotSet, int index)
    {
        if (InGameManager.Instance != null && !InGameManager.Instance.CanPerformPlayerAction(performer)) return;
        Plot selectedPlot = AvailPlotSets[plotSet].plots[index];
        if (!MeetsEffectiveInfluenceCondition(selectedPlot, performer) || !selectedPlot.IsEffectiveCostEnough(performer)) return;
        if (InGameManager.Instance != null) InGameManager.Instance.ExecuteNpcActionsBeforePlayerAction(performer);
        selectedPlot.Execute(performer);
        performer?.OnPlotExecuted();
        AvailPlotSets[plotSet].use(index);

        if (ActionRecordManager.Instance != null)
        {
            ActionRecordManager.Instance.RecordPlot(performer);
        }
        if (InGameManager.Instance != null) InGameManager.Instance.CompletePlayerAction(performer);
    }

    public void RerollPlotSet(int plotSet = 0)
    {
        if (InGameManager.Instance != null)
        {
            activePlotDay = InGameManager.Instance.Context.CurrentDay;
        }

        HashSet<Plot> previousPlots = availPlotSets[plotSet] != null
            ? new HashSet<Plot>(availPlotSets[plotSet].plots.Where(plot => plot != null))
            : null;
        availPlotSets[plotSet] = GeneratePlotSet(previousPlots);
    }

    public Plot GetPlotById(string plotId)
    {
        if (string.IsNullOrWhiteSpace(plotId))
        {
            return null;
        }

        return plots.Find(plot => plot != null && plot.plotID == plotId);
    }

    public PlotManagerSaveData CaptureSaveData()
    {
        PlotManagerSaveData saveData = new PlotManagerSaveData
        {
            activeDay = activePlotDay
        };

        for (int i = 0; i < availPlotSets.Length; i++)
        {
            PlotSetSaveData setSave = new PlotSetSaveData();
            PlotSet currentSet = availPlotSets[i];

            for (int slot = 0; slot < 3; slot++)
            {
                string plotId = string.Empty;
                bool used = false;

                if (currentSet != null)
                {
                    Plot currentPlot = currentSet.plots[slot];
                    plotId = currentPlot != null ? currentPlot.plotID : string.Empty;
                    used = currentSet.isUsed[slot];
                }

                setSave.plotIds.Add(plotId);
                setSave.usedSlots.Add(used);
            }

            saveData.plotSets.Add(setSave);
        }

        return saveData;
    }

    public void RestoreFromSave(PlotManagerSaveData saveData)
    {
        usedPlots.Clear();
        availPlotSets = new PlotSet[2];
        activePlotDay = saveData != null && saveData.activeDay > 0
            ? saveData.activeDay
            : InGameManager.Instance.Context.CurrentDay;

        if (saveData == null || saveData.plotSets == null)
        {
            return;
        }

        for (int i = 0; i < availPlotSets.Length && i < saveData.plotSets.Count; i++)
        {
            PlotSetSaveData setSave = saveData.plotSets[i];
            if (setSave == null || setSave.plotIds == null || setSave.plotIds.Count < 3)
            {
                continue;
            }

            Plot[] restoredPlots = new Plot[3];
            bool[] restoredFromSave = new bool[3];

            for (int slot = 0; slot < 3; slot++)
            {
                string plotId = setSave.plotIds[slot];
                restoredPlots[slot] = GetPlotById(plotId);
                restoredFromSave[slot] = restoredPlots[slot] != null;
                if (!string.IsNullOrWhiteSpace(plotId) && restoredPlots[slot] == null)
                {
                    Debug.LogWarning($"[Save] 공작 '{plotId}'를 찾지 못해 빈 슬롯으로 복원합니다.");
                }
            }

            float playerInfluence = GetPlayerInfluence();
            for (int slot = 0; slot < restoredPlots.Length; slot++)
            {
                if (restoredPlots[slot] == null)
                {
                    restoredPlots[slot] = PickSlotPlot(
                        RollPreferredGrade(slot), playerInfluence, restoredPlots, null);
                }
            }

            availPlotSets[i] = new PlotSet(restoredPlots);

            if (setSave.usedSlots != null)
            {
                for (int slot = 0; slot < 3 && slot < setSave.usedSlots.Count; slot++)
                {
                    if (restoredFromSave[slot] && setSave.usedSlots[slot])
                    {
                        availPlotSets[i].use(slot);
                    }
                }
            }
        }

        for (int i = 0; i < availPlotSets.Length; i++)
        {
            if (availPlotSets[i] == null)
            {
                availPlotSets[i] = GeneratePlotSet();
            }
        }
    }
}
