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
            this.isUsed[i] = false;
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

        for (int i = 0; i < selectedPlots.Length; i++)
        {
            selectedPlots[i] = PickByInfluenceBand(playerInfluence, selectedPlots);
        }

        return new PlotSet(selectedPlots);
    }

    private float GetPlayerInfluence()
    {
        if (CardinalManager.Instance == null) return 0f;
        Cardinal player = CardinalManager.Instance.Cardinals.FirstOrDefault(
            candidate => candidate != null && candidate.CompareTag("Player"));
        return player != null ? player.Influence : 0f;
    }

    private Plot PickByInfluenceBand(float playerInfluence, Plot[] alreadySelected)
    {
        List<Plot> available = plots.Where(plot => plot != null && IsSupportedByTurnSystem(plot) && !usedPlots.Contains(plot) &&
            !alreadySelected.Contains(plot)).ToList();
        List<Plot> low = available.Where(plot => plot.GetInfluenceRequirement() < playerInfluence - 2f).ToList();
        List<Plot> middle = available.Where(plot => plot.GetInfluenceRequirement() > playerInfluence - 2f &&
            plot.GetInfluenceRequirement() < playerInfluence).ToList();
        List<Plot> high = available.Where(plot => plot.GetInfluenceRequirement() > playerInfluence &&
            plot.GetInfluenceRequirement() < playerInfluence + 2f).ToList();

        var bands = new List<(List<Plot> candidates, float weight)>();
        if (low.Count > 0) bands.Add((low, 30f));
        if (middle.Count > 0) bands.Add((middle, 50f));
        if (high.Count > 0) bands.Add((high, 20f));
        if (bands.Count == 0) return PickWeightedPlot(available);

        float roll = Random.Range(0f, bands.Sum(band => band.weight));
        foreach (var band in bands)
        {
            roll -= band.weight;
            if (roll <= 0f) return PickWeightedPlot(band.candidates);
        }
        return PickWeightedPlot(bands[bands.Count - 1].candidates);
    }

    private static bool IsSupportedByTurnSystem(Plot plot)
    {
        // P007/P019는 NPC 행동 슬롯, P021은 저장 가능한 익일 예약 효과,
        // P031은 콘클라베 단위 기도 제한 계약, P033은 저장 가능한 차회 예약 효과가 필요하다.
        // 부분 효과로 소비되지 않게 제외한다.
        return plot.plotID != "P007" && plot.plotID != "P019" &&
            plot.plotID != "P021" && plot.plotID != "P031" && plot.plotID != "P033";
    }

    private Plot PickWeightedPlot(List<Plot> candidates)
    {
        if (candidates == null || candidates.Count == 0) return null;
        float sum = candidates.Sum(plot => Mathf.Max(0f, plot.GetPlotWeight()));
        if (sum <= 0f) return candidates[Random.Range(0, candidates.Count)];

        float roll = Random.Range(0f, sum);
        foreach (Plot candidate in candidates)
        {
            roll -= Mathf.Max(0f, candidate.GetPlotWeight());
            if (roll <= 0f) return candidate;
        }
        return candidates[candidates.Count - 1];
    }

    public void RefreshPlotManager()
    {
        usedPlots.Clear();
    }

    private Plot GetWeightedRandPlot(PlotGrade grade)
    {
        var candidates = plots.Where(p => p.plotGrade == grade && !usedPlots.Contains(p)).ToList();

        float weightSum = candidates.Sum(p => p.GetPlotWeight());
        
        float randChoice = Random.Range(0f, weightSum);
        float currentSum = 0f;

        foreach(var p in candidates)
        {
            currentSum += p.GetPlotWeight();
            if (currentSum >= randChoice)
            {
                return p;
            }
        }

        return candidates[0];
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
        AvailPlotSets[plotSet].plots[index].Execute(performer);
        performer?.OnPlotExecuted();
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
