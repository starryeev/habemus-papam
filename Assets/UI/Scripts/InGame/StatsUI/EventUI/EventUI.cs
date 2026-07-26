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
                Time.timeScale = 1f;
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
        Debug.Log("EventUI Set to" + InGameManager.Instance.GetCurrentEvent());
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
        Debug.Log($"EventUI {InGameManager.Instance.GetCurrentEvent()} Result 1 Showing");
    }
    public void ShowResult2()
    {
        SetState(EventUIState.RESULT2);
        Debug.Log($"EventUI Result 2 {InGameManager.Instance.GetCurrentEvent()} Showing");
    }
    public void Close()
    {
        Debug.Log("EventUI.Close");
        SetState(EventUIState.NONE);
    }
}
