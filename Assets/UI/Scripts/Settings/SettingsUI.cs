using UnityEngine;

public class SettingsUI : MonoBehaviour
{
    private SettingsWindow settingsWindow;
    private UIManager.UIState prevState;
    void OnEnable()
    {
        prevState = UIManager.Instance.State;
        UIManager.Instance.SetUIState(UIManager.UIState.SETTINGS);
    }

    void EnableWindow()
    {
        settingsWindow.gameObject.SetActive(true);
    }
    void DisableWindow()
    {
        settingsWindow.gameObject.SetActive(false);
    }

    void OnDisable()
    {
        UIManager.Instance.SetUIState(prevState);
    }
    bool isCredit = false;
    bool isHelp = false;
    void OpenCredits()
    {
        isCredit = true;
    }
    void CloseCredits()
    {
        isCredit = false;
    }
    void OpenHelp()
    {
        isHelp = true;
    }
    void CloseHelp()
    {
        isHelp = false;
    }

    // 어떤 방식으로 credits와 help를 열지는 나중에 결정

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if(isCredit) CloseCredits();
            else if(isHelp) CloseHelp();
        }
    }
}