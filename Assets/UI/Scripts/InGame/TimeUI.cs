using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
/*
오브젝트 : 상단 UI
하위 오브젝트
- 좌측 텍스트 : 콘클라베 일자와 시간(새벽/아침/저녁/밤)을 텍스트로 표시
- 우측 텍스트 : 남은 시간을 0:00.00으로 표시
- 시계 : 시간대별 시작 시각에서 행동 완료마다 1시간씩 시침이 이동
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

    private CanvasGroup canvasGroup;
    private Coroutine alphaCoroutine;

    private void Awake()
    {
        ValidateClockHandRules();
        HideForConclaveEntrance();
    }

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
            GameContext context = InGameManager.Instance.Context;
            RightText2.text = FormatActionProgress(context);
            UpdateClockHand(context);
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

        LeftText1.text = $"Day {(currentDay - 1) * 4 + (int)currentCon + 1}";
        LeftText2.text = $"{currentCon}";
        RightText1.text = "Action";

        UpdateLights(currentCon);
        UpdateClockHand(InGameManager.Instance.Context);
    }

    public void EndConclaveUI()
    {
        if (InGameManager.Instance != null)
        {
            GameContext context = InGameManager.Instance.Context;
            RightText2.text = FormatActionProgress(context);
            UpdateClockHand(context);
        }

        Dawn.sprite = LightList[0];
        Morning.sprite = LightList[0];
        Afternoon.sprite = LightList[0];
        Evening.sprite = LightList[0];
    }

    public void HideForConclaveEntrance()
    {
        EnsureCanvasGroup();
        if (alphaCoroutine != null)
        {
            StopCoroutine(alphaCoroutine);
            alphaCoroutine = null;
        }

        canvasGroup.alpha = 0f;
        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;
    }

    public void FadeInAfterConclaveEntrance(float duration = 1f)
    {
        EnsureCanvasGroup();
        if (alphaCoroutine != null)
        {
            StopCoroutine(alphaCoroutine);
        }

        alphaCoroutine = StartCoroutine(FadeCanvasAlpha(1f, duration));
    }

    private void EnsureCanvasGroup()
    {
        if (canvasGroup == null)
        {
            canvasGroup = GetComponent<CanvasGroup>();
            if (canvasGroup == null)
            {
                canvasGroup = gameObject.AddComponent<CanvasGroup>();
            }
        }
    }

    private IEnumerator FadeCanvasAlpha(float targetAlpha, float duration)
    {
        float startAlpha = canvasGroup.alpha;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float progress = duration <= 0f ? 1f : Mathf.Clamp01(elapsed / duration);
            canvasGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, progress);
            yield return null;
        }

        canvasGroup.alpha = targetAlpha;
        canvasGroup.interactable = targetAlpha > 0f;
        canvasGroup.blocksRaycasts = targetAlpha > 0f;
        alphaCoroutine = null;
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
            case GameContext.Conclave.Evening:
                Dawn.sprite = LightList[1];
                Morning.sprite = LightList[2];
                Afternoon.sprite = LightList[3];
                break;
            case GameContext.Conclave.Afternoon:
                Dawn.sprite = LightList[1];
                Morning.sprite = LightList[2];
                Afternoon.sprite = LightList[3];
                Evening.sprite = LightList[4];
                break;
        }
    }

    private static string FormatActionProgress(GameContext context)
    {
        int total = context.CurrentPositionActionCount;
        int current = context.CurrentActionNumber;
        return $"{current} / {total}";
    }

    private void UpdateClockHand(GameContext context)
    {
        if (ClockHand == null || context == null) return;
        int hour = GetClockHour(context.CurrentConclave, context.CompletedActions);
        ClockHand.rotation = Quaternion.Euler(0f, 0f, -30f * (hour % 12));
    }

    private static int GetClockHour(GameContext.Conclave conclave, int completedActions)
    {
        (int startHour, int maxAdvance) = conclave switch
        {
            GameContext.Conclave.Dawn => (4, 4),
            GameContext.Conclave.Morning => (9, 3),
            GameContext.Conclave.Afternoon => (1, 5),
            GameContext.Conclave.Evening => (7, 8),
            _ => (12, 0)
        };
        int hour = startHour + Mathf.Clamp(completedActions, 0, maxAdvance);
        return (hour - 1) % 12 + 1;
    }

    [System.Diagnostics.Conditional("UNITY_EDITOR")]
    private static void ValidateClockHandRules()
    {
        Debug.Assert(
            GetClockHour(GameContext.Conclave.Dawn, 0) == 4 &&
            GetClockHour(GameContext.Conclave.Dawn, 99) == 8 &&
            GetClockHour(GameContext.Conclave.Morning, 0) == 9 &&
            GetClockHour(GameContext.Conclave.Morning, 99) == 12 &&
            GetClockHour(GameContext.Conclave.Afternoon, 0) == 1 &&
            GetClockHour(GameContext.Conclave.Afternoon, 99) == 6 &&
            GetClockHour(GameContext.Conclave.Evening, 0) == 7 &&
            GetClockHour(GameContext.Conclave.Evening, 99) == 3,
            "시간대별 시침 시작/종료 규칙이 손상됐습니다.");
    }
}
