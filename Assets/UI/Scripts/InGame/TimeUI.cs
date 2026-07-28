using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
/*
오브젝트 : 상단 UI
하위 오브젝트
- 좌측 텍스트 : 콘클라베 일자와 시간(새벽/아침/점심/저녁)을 텍스트로 표시
- 우측 텍스트 : 남은 시간을 0:00.00으로 표시
- 시계 : 남은 시간을 비율로 표기 (콘클라베 1회 동안 1바퀴 돌아감)
- 표시등 4개 : '꺼짐' 상태로 있다가 콘클라베가 시작되면 시간에 맞도록 켜짐
*/
public class TimeUI : MonoBehaviour
{
    [Header("텍스트")]
    [SerializeField] TextMeshProUGUI LeftText1;
    [SerializeField] TextMeshProUGUI LeftText2;
    [SerializeField] TextMeshProUGUI RightText1;
    [SerializeField] TextMeshProUGUI RightText2;
    [Space(10f)]
    [Header("오브젝트")]
    [SerializeField] RectTransform ClockHand;
    [SerializeField] Image Dawn;
    [SerializeField] Image Morning;
    [SerializeField] Image Afternoon;
    [SerializeField] Image Evening;
    [Space(10f)]
    [Header("이미지")]
    [SerializeField] List<Sprite> LightList;


    void Start()
    {
        if (InGameManager.Instance != null && InGameManager.Instance.Context != null)
        {
            InGameManager.Instance.Context.OnGameContextEvent += HandleGameContextEvent;
        }
    }
    void Update()
    {

        if (InGameManager.Instance == null) return;

        if (InGameManager.Instance.IsTimeRunning)
        {
            int turn = InGameManager.Instance.GetCurrentTurn();
            int phase = InGameManager.Instance.GetCurrentTurnPhase();
            RightText2.text = $"Turn {turn}-{phase}";
            ClockHand.transform.rotation = Quaternion.Euler(0, 0, -90f * (turn - 1));
        }
    }

    void OnDestroy()
    {
        if (InGameManager.Instance != null && InGameManager.Instance.Context != null)
        {
            InGameManager.Instance.Context.OnGameContextEvent -= HandleGameContextEvent;
        }
    }

    private void HandleGameContextEvent(GameContext.GameContextEvent evt)
    {
        switch (evt)
        {
            case GameContext.GameContextEvent.ConclaveStart:
                ResetUI();
                break;
            case GameContext.GameContextEvent.ConclaveEnd:
                EndConclaveUI();
                break;
        }
    }

    public void ResetUI()
    {
        var currentDay = InGameManager.Instance.GetCurrentDay();
        var currentCon = InGameManager.Instance.GetCurrentConclave();

        LeftText1.text = $"Day {currentDay}";
        LeftText2.text = $"{currentCon}";
        RightText1.text = "턴";

        UpdateLights(currentCon);
    }

    public void EndConclaveUI()
    {
        RightText2.text = "Turn 4-2";
        ClockHand.transform.rotation = Quaternion.identity;

        Dawn.sprite = LightList[0];
        Morning.sprite = LightList[0];
        Afternoon.sprite = LightList[0];
        Evening.sprite = LightList[0];
    }

    
    private void UpdateLights(GameContext.Conclave currentCon)
    {
        Dawn.sprite = LightList[0];
        Morning.sprite = LightList[0];
        Afternoon.sprite = LightList[0];
        Evening.sprite = LightList[0];

        switch (currentCon)
        {
            case GameContext.Conclave.Dawn:
                Dawn.sprite = LightList[1];
                break;
            case GameContext.Conclave.Morning:
                Dawn.sprite = LightList[1];
                Morning.sprite = LightList[2];
                break;
            case GameContext.Conclave.Afternoon:
                Dawn.sprite = LightList[1];
                Morning.sprite = LightList[2];
                Afternoon.sprite = LightList[3];
                break;
            case GameContext.Conclave.Evening:
                Dawn.sprite = LightList[1];
                Morning.sprite = LightList[2];
                Afternoon.sprite = LightList[3];
                Evening.sprite = LightList[4];
                break;
        }
    }
}
