using TMPro;
using UnityEngine;
using UnityEngine.UI;
//이벤트 버튼 창 열기/닫기
//
public class EventUI : MonoBehaviour
{
    [Header("Panels")]
    [SerializeField] EventWindow window;
    [SerializeField] EventResult result;

    public enum EventUIState
    {
        NONE,
        TUTORIAL,
        CHOICE,
        RESULT1,
        RESULT2
    }
    private EventUIState currentState = EventUIState.NONE;
    private void Start()
    {
        SetState(EventUIState.NONE);
    }
    public void SetState(EventUIState state)
    {
        currentState = state;

        switch (state)
        {
            case EventUIState.NONE:
                if (InGameManager.Instance == null || !InGameManager.Instance.IsConclaveExitInProgress)
                {
                    Time.timeScale = 1f;
                }
                window.gameObject.SetActive(false);
                result.gameObject.SetActive(false);
                break;
            case EventUIState.TUTORIAL:
                window.gameObject.SetActive(true);
                result.gameObject.SetActive(false);
                Time.timeScale = 0f;
                break;
            case EventUIState.CHOICE:
                window.gameObject.SetActive(true);
                window.ShowEvent(InGameManager.Instance.GetCurrentEvent());
                result.gameObject.SetActive(false);
                Time.timeScale = 0f;
                break;
            case EventUIState.RESULT1:
                window.Clear();
                window.gameObject.SetActive(false);
                result.gameObject.SetActive(true);
                result.ShowEvent(InGameManager.Instance.GetCurrentEvent(), 1);
                Time.timeScale = 0f;
                break;
            case EventUIState.RESULT2:
                window.Clear();
                window.gameObject.SetActive(false);
                result.gameObject.SetActive(true);
                result.ShowEvent(InGameManager.Instance.GetCurrentEvent(), 2);
                Time.timeScale=0f;
                break;
        }
        
    }
    
    public void UISetEvent()
    {
        GameSceneCameraZoom.ReleaseAllGameCameraZoomAndFollow(1f);
        SetState(EventUIState.CHOICE);
    }
    public void UISetEvent(string eventID = "11100")
    {
        GameSceneCameraZoom.ReleaseAllGameCameraZoomAndFollow(1f);
        SetState(EventUIState.TUTORIAL);
        window.ShowEvent(eventID);
    }
    public void ShowResult1()
    {
        SetState(EventUIState.RESULT1);
    }
    public void ShowResult2()
    {
        SetState(EventUIState.RESULT2);
    }
    public void Close()
    {
        SetState(EventUIState.NONE);
        if (InGameManager.Instance != null)
        {
            InGameManager.Instance.OnTurnEventClosed();
        }
    }
}
