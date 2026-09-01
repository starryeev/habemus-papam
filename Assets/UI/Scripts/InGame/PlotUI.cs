using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using TMPro;

public class PlotUI : MonoBehaviour
{
    private const float PlotContactCooldown = 3f;

    [Header("--- 공작 선택 UI ---")]
    public GameObject plotSelectUI;
    public Image[] plotPanels = new Image[3];
    public Button[] plotUseButtons = new Button[3];

    [Header("--- 공작 정보 텍스트 ---")]
    public TextMeshProUGUI[] plotNameList = new TextMeshProUGUI[3];
    public TextMeshProUGUI[] plotDescList = new TextMeshProUGUI[3];
    public TextMeshProUGUI[] plotEffectList = new TextMeshProUGUI[3];
    public TextMeshProUGUI[] plotCondiList = new TextMeshProUGUI[3];

    [Header("등급별 스프라이트 설정")]
    [SerializeField] private Sprite commonSprite;
    [SerializeField] private Sprite rareSprite;
    [SerializeField] private Sprite legendSprite;



    [Header("--- 테스트용 공작 ---")]
    public Plot testPlot;

    [Header("--- 테스트용 아이템 ---")]
    public GameObject testItem;

    private Cardinal performer;
    private StateController performerState;
    private StateController schemerState;

    void Start()
    {
        if (InGameManager.Instance != null && InGameManager.Instance.Context != null)
        {
            InGameManager.Instance.Context.OnGameContextEvent += OnGameContextChanged;
        }

        plotSelectUI.SetActive(false);

        for (int i = 0; i < 3; i++)
        {
            int index = i;
            plotUseButtons[index].onClick.AddListener(() => OnSelectPlot(index));
        }

        ResetPlotUI();
    }

    void OnDestroy()
    {
        SetPlotMovementLocked(false);

        if (InGameManager.Instance != null && InGameManager.Instance.Context != null)
        {
            InGameManager.Instance.Context.OnGameContextEvent -= OnGameContextChanged;
        }
    }

    private void OnEnable() // UI가 켜질 때 시작
    {
        StartCoroutine(Co_UpdatePlotStates());
    }

    private IEnumerator Co_UpdatePlotStates()
    {
        while (gameObject.activeSelf)
        {
            for (int i = 0; i < 3; i++)
            {
                UpdatePlotButtonState(i);
            }
            yield return new WaitForSeconds(0.1f); // 0.1초 대기 (초당 10번만 실행)
        }
    }
    private void OnGameContextChanged(GameContext.GameContextEvent eventType)
    {
        if (eventType == GameContext.GameContextEvent.ConclaveEnd)
        {
            OnClickClose();
        }
    }


    public void ShowPlotUI(Cardinal performer, StateController schemerState)
    {
        this.performer = performer;
        performerState = performer != null ? performer.GetComponent<StateController>() : null;
        this.schemerState = schemerState;

        if (!SetPlotUI()) return;

        plotSelectUI.SetActive(true);
        SetPlotMovementLocked(true);

        /* 다른 상호작용 버튼 비활성화
        foreach (var item in actionButtons)
        {
            item.interactable = false;
        }*/
    }

    public void OnClickClose()
    {
        schemerState?.StartPlotContactCooldown(PlotContactCooldown);
        plotSelectUI.SetActive(false);
        SetPlotMovementLocked(false);

        /* 다른 상호작용 버튼 활성화
        foreach (var item in actionButtons)
        {
            item.interactable = true;
        }*/
    }

    private void SetPlotMovementLocked(bool locked)
    {
        performerState?.SetPlotMovementLocked(locked);
        schemerState?.SetPlotMovementLocked(locked);

        if (!locked)
        {
            performerState = null;
            schemerState = null;
        }
    }

    private bool SetPlotUI()
    {
        ResetPlotUI();

        var pm = PlotManager.Instance;
        if (pm == null || pm.AvailPlotSets == null || pm.AvailPlotSets.Length == 0 ||
            pm.AvailPlotSets[0] == null)
        {
            Debug.LogWarning("[PlotUI] 표시할 공작 세트가 없습니다.");
            return false;
        }

        for (int i = 0; i < 3; i++)
        {
            var currentPlot = pm.AvailPlotSets[0].plots[i];
            var buttonText = plotUseButtons[i].GetComponentInChildren<TextMeshProUGUI>();

            if (currentPlot == null)
            {
                plotNameList[i].text = "등장 가능한 공작 없음";
                plotDescList[i].text = "현재 정치력 조건을 만족하는 공작이 없습니다.";
                plotCondiList[i].text = string.Empty;
                plotEffectList[i].text = string.Empty;
                buttonText.text = string.Empty;
                plotUseButtons[i].interactable = false;
                continue;
            }

            // 공작 등급에 따른 뒷 배경 세팅
            switch (currentPlot.plotGrade)
            {
                case PlotGrade.Common:
                    plotPanels[i].sprite = commonSprite;
                    break;

                case PlotGrade.Rare:
                    plotPanels[i].sprite = rareSprite;
                    break;

                case PlotGrade.Legendary:
                    plotPanels[i].sprite = legendSprite;
                    break;

                default:
                    // 혹시 모를 예외 처리
                    break;
            }

            // 공작 텍스트 정보 세팅
            plotNameList[i].text = currentPlot.plotName;
            plotDescList[i].text = currentPlot.plotDescription;
            plotCondiList[i].text = pm.GetEffectiveConditionText(currentPlot, performer);
            plotEffectList[i].text = currentPlot.plotEffect;
            buttonText.text = currentPlot.plotCostText;

            if (pm.AvailPlotSets[0].isUsed[i])
            {
                plotPanels[i].color = new Color(0.8f, 0.8f, 0.8f);
                plotUseButtons[i].interactable = false;
            }
            else
            {
                plotPanels[i].color = new Color(1f, 1f, 1f);

                UpdatePlotButtonState(i);
            }
        }

        return true;
    }

    private void UpdatePlotButtonState(int index)
    {
        var pm = PlotManager.Instance;

        if (pm.AvailPlotSets[0] == null) return;

        if (pm.AvailPlotSets[0].isUsed[index])
        {
            plotUseButtons[index].interactable = false;
            return;
        }

        var currentPlot = pm.AvailPlotSets[0].plots[index];
        var buttonText = plotUseButtons[index].GetComponentInChildren<TextMeshProUGUI>();
        if (currentPlot == null)
        {
            plotUseButtons[index].interactable = false;
            buttonText.text = string.Empty;
            return;
        }

        // 조건 확인
        bool isPietyEnough = currentPlot.IsEffectiveCostEnough(performer);
        bool canExecute = PlotManager.Instance != null && PlotManager.Instance.MeetsEffectiveInfluenceCondition(currentPlot, performer);

        // 버튼 활성화 설정
        plotUseButtons[index].interactable = isPietyEnough && canExecute;
        
        string finalProgressText = currentPlot.plotCostText;
        string statusMessage = "";

        if (!isPietyEnough)
        {
            statusMessage += " 비용 부족";
        }

        if (!canExecute)
        {
            statusMessage += " 조건 미충족";
        }

        buttonText.text = finalProgressText + $"<br><color=red><size=60%>{statusMessage}</size></color>";
    }

    public bool IsConditionUnavailable(Button button)
    {
        PlotManager pm = PlotManager.Instance;
        if (button == null || performer == null || pm == null ||
            pm.AvailPlotSets == null || pm.AvailPlotSets.Length == 0 || pm.AvailPlotSets[0] == null)
        {
            return false;
        }

        for (int i = 0; i < plotUseButtons.Length; i++)
        {
            if (plotUseButtons[i] != button)
            {
                continue;
            }

            PlotSet plotSet = pm.AvailPlotSets[0];
            Plot plot = plotSet.plots[i];
            return !plotSet.isUsed[i] && plot != null &&
                   (!plot.IsEffectiveCostEnough(performer) || !pm.MeetsEffectiveInfluenceCondition(plot, performer));
        }

        return false;
    }

    // 공작 UI 정보 리셋 함수
    public void ResetPlotUI()
    {
        for (int i = 0; i < 3; i++)
        {
            plotNameList[i].text = "";
            plotDescList[i].text = "";
            plotEffectList[i].text = "";
            plotUseButtons[i].interactable = false;
        }
    }

    public void OnSelectPlot(int index)
    {
        var pm = PlotManager.Instance;

        pm.UsePlot(0, index);

        OnClickClose();
    }

    public void PlotTest()
    {
        testPlot.Execute(performer);
    }

    public void ItemTest()
    {
        FieldItem rewardItem = testItem.GetComponent<FieldItem>();

        if (rewardItem != null)
        {
            Item data = rewardItem.ItemData;

            if (data != null)
            {
                InventoryManager.Instance.AddItem(data);
            }
        }

    }
}
