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
        float playerInfluence = GetPlayerInfluence();
        Plot[] selectedPlots = new Plot[3];

        selectedPlots[0] = PickSlotPlot(Random.value < 0.6f ? PlotGrade.Common : PlotGrade.Rare, playerInfluence, selectedPlots);
        selectedPlots[1] = PickSlotPlot(Random.value < 0.9f ? PlotGrade.Rare : PlotGrade.Legendary, playerInfluence, selectedPlots);
        selectedPlots[2] = PickSlotPlot(Random.value < 0.6f ? PlotGrade.Common : PlotGrade.Rare, playerInfluence, selectedPlots);

        return new PlotSet(selectedPlots);
    }

    private float GetPlayerInfluence()
    {
        if (CardinalManager.Instance == null) return 0f;
        Cardinal player = CardinalManager.Instance.Cardinals.FirstOrDefault(
            candidate => candidate != null && candidate.CompareTag("Player"));
        return player != null ? player.Influence : 0f;
    }

    private Plot PickSlotPlot(PlotGrade grade, float playerInfluence, Plot[] alreadySelected)
    {
        List<Plot> available = GetAvailablePlots(grade, alreadySelected);
        if (available.Count == 0 && grade == PlotGrade.Legendary)
            return PickSlotPlot(PlotGrade.Rare, playerInfluence, alreadySelected);
        if (available.Count == 0) return null;

        int conditionPenalty = InGameManager.Instance != null && InGameManager.Instance.IsNpcCandidateLeading(2) ? 1 : 0;
        List<Plot> lower = available.Where(plot => plot.GetInfluenceRequirement() + conditionPenalty < playerInfluence).ToList();
        List<Plot> higher = available.Where(plot => playerInfluence < plot.GetInfluenceRequirement() + conditionPenalty &&
            plot.GetInfluenceRequirement() + conditionPenalty < playerInfluence + 2f).ToList();

        bool chooseLower = Random.value < 0.8f;
        List<Plot> selectedBand = chooseLower ? lower : higher;
        if (selectedBand.Count == 0) selectedBand = chooseLower ? higher : lower;
        if (selectedBand.Count == 0 && grade == PlotGrade.Legendary)
            return PickSlotPlot(PlotGrade.Rare, playerInfluence, alreadySelected);
        if (selectedBand.Count == 0) return null;
        return selectedBand[Random.Range(0, selectedBand.Count)];
    }

    private List<Plot> GetAvailablePlots(PlotGrade grade, Plot[] alreadySelected)
    {
        return plots.Where(plot => plot != null && plot.plotGrade == grade && IsSupportedByTurnSystem(plot) &&
            !usedPlots.Contains(plot) && !alreadySelected.Contains(plot)).ToList();
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
            availPlotSets[0] = GeneratePlotSet();
            availPlotSets[1] = GeneratePlotSet();
        }
    }

    public void InitializePlotSession(Cardinal performer)
    {
        if (InGameManager.Instance != null && !InGameManager.Instance.CanPerformPlayerAction(performer)) return;
        this.performer = performer;

        plotUI.ShowPlotUI(performer);
    }

    public void UsePlot(int plotSet, int index)
    {
        if (InGameManager.Instance != null && !InGameManager.Instance.CanPerformPlayerAction(performer)) return;
        Plot selectedPlot = AvailPlotSets[plotSet].plots[index];
        if (!MeetsEffectiveInfluenceCondition(selectedPlot, performer) || !selectedPlot.IsEffectiveCostEnough(performer)) return;
        if (InGameManager.Instance != null) InGameManager.Instance.ExecuteNpcActionsBeforePlayerAction(performer);
        selectedPlot.Execute(performer);
        performer?.OnPlotExecuted();
        if (InGameManager.Instance != null && InGameManager.Instance.EventManager != null)
        {
            Event tutorial = InGameManager.Instance.EventManager.GetFirstPlotTutorialEvent();
            InGameManager.Instance.QueueImmediateEventAfterPlayerAction(tutorial);
        }
        AvailPlotSets[plotSet].use(index);

        if (ActionRecordManager.Instance != null)
        {
            ActionRecordManager.Instance.RecordPlot(performer);
        }
        if (InGameManager.Instance != null) InGameManager.Instance.CompletePlayerAction(performer);

        CheckIsAllUsed(plotSet);
    }

    public void CheckIsAllUsed(int plotSet = 0)
    {
        if (AvailPlotSets[plotSet].isAllUsed())
        {
            RerollPlotSet(plotSet);
        }
    }

    public void RerollPlotSet(int plotSet = 0)
    {
        availPlotSets[plotSet] = GeneratePlotSet();
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
        PlotManagerSaveData saveData = new PlotManagerSaveData();

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
            bool canRestore = true;

            for (int slot = 0; slot < 3; slot++)
            {
                restoredPlots[slot] = GetPlotById(setSave.plotIds[slot]);
                if (restoredPlots[slot] == null)
                {
                    canRestore = false;
                    break;
                }
            }

            if (!canRestore)
            {
                Debug.LogWarning($"[Save] {i}번 공작 세트를 완전히 복원하지 못했습니다.");
                continue;
            }

            availPlotSets[i] = new PlotSet(restoredPlots);

            if (setSave.usedSlots != null)
            {
                for (int slot = 0; slot < 3 && slot < setSave.usedSlots.Count; slot++)
                {
                    if (setSave.usedSlots[slot])
                    {
                        availPlotSets[i].use(slot);
                    }
                }
            }
        }
    }
}
