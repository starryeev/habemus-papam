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

    // 한 번만 호출하기 위한 플래그
    private bool hasConclaveEnded = false;

    void Start()
    {
        InGameManager.Instance.Context.OnGameContextEvent += OnConclaveStart;
    }

    private void OnConclaveStart(GameContext.GameContextEvent evt)
    {
        Reset();
        hasConclaveEnded = false;

        Debug.Log("Currentcon = " + InGameManager.Instance.GetCurrentConclave());
    }

    public void Reset()
    {
        var currentDay = InGameManager.Instance.GetCurrentDay();
        var currentCon = InGameManager.Instance.GetCurrentConclave();
        
        LeftText1.text = $"Day {currentDay}";
        LeftText2.text = $"{currentCon}";
        RightText1.text = "남은 시간";
        RightText2.text = $"{(int)(InGameManager.Instance.Balance.MaxConclaveTime/60)} : {(InGameManager.Instance.Balance.MaxConclaveTime%60).ToString("00.00")}";
        ClockHand.transform.rotation = Quaternion.identity;

        switch (currentCon)
        {
            case GameContext.Conclave.Dawn:
                {
                    Dawn.sprite = LightList[1];
                    Morning.sprite = LightList[0];
                    Afternoon.sprite = LightList[0];
                    Evening.sprite = LightList[0];
                    break;
                }
            case GameContext.Conclave.Morning:
                {
                    Dawn.sprite = LightList[1];
                    Morning.sprite = LightList[2];
                    Afternoon.sprite = LightList[0];
                    Evening.sprite = LightList[0];
                    break;
                }
            case GameContext.Conclave.Afternoon:
                {
                    Dawn.sprite = LightList[1];
                    Morning.sprite = LightList[2];
                    Afternoon.sprite = LightList[3];
                    Evening.sprite = LightList[0];
                    break;
                }
            case GameContext.Conclave.Evening:
                {
                    Dawn.sprite = LightList[1];
                    Morning.sprite = LightList[2];
                    Afternoon.sprite = LightList[3];
                    Evening.sprite = LightList[4];
                    break;
                }
            
        }
        // 시작 시 종료 플래그 초기화
        hasConclaveEnded = false;
    }

    public void EndConclave()
    {
        LeftText1.text = $"Day {InGameManager.Instance.GetCurrentDay()}";
        LeftText2.text = $"{InGameManager.Instance.GetCurrentConclave()}";
        RightText1.text = "남은 시간";
        RightText2.text = $"{(int)(InGameManager.Instance.Balance.MaxConclaveTime/60)} : {(InGameManager.Instance.Balance.MaxConclaveTime%60).ToString("00.00")}";
        ClockHand.transform.rotation = Quaternion.identity;
        Dawn.sprite = LightList[0];
        Morning.sprite = LightList[0];
        Afternoon.sprite = LightList[0];
        Evening.sprite = LightList[0];
    }
    
    void Update()
    {
        var currentDay = InGameManager.Instance.GetCurrentDay();
        var currentCon = InGameManager.Instance.GetCurrentConclave();
        //시계바늘 돌리기, 우측 텍스트 동기화하기
        float remain = InGameManager.Instance.GetRemainingTime();
        float max = InGameManager.Instance.Balance.MaxConclaveTime;
        ClockHand.transform.rotation = Quaternion.Euler(0, 0, remain/max*360f);
        LeftText1.text = $"Day {currentDay}";
        LeftText2.text = $"{currentCon}";
        RightText2.text = $"{(int)(remain/60)} : {(remain%60).ToString("00.00")}";


        //시간이 0이 되었을 경우, StopConClave() 호출
        if (!hasConclaveEnded && remain <= 0f)
        {
            hasConclaveEnded = true; 

            InGameManager.Instance.StopTimer();

            if (CardinalManager.Instance != null)
            {
                CardinalManager.Instance.StopConClave();
            }
        }
    }
}