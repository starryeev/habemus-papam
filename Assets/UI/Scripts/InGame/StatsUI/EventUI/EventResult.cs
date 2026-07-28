using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Unity.VisualScripting;

class EventResult : MonoBehaviour
{
    [SerializeField] TextMeshProUGUI title;
    [SerializeField] TextMeshProUGUI text;
    [SerializeField] TextMeshProUGUI result;
    [SerializeField] Button button;
    [SerializeField] TextMeshProUGUI ButtonText;
    private Event currentEvent;
    
    public void ShowEvent(Event evt, int result)
    {
        ButtonText.text = "확인";
        currentEvent = evt;
        title.text = evt.eventName;
        bool succeeded;

        if(result == 1)
        {
            succeeded = evt.OnChoiceOption1(UIManager.Instance.Ingame.Stats.LinkedCardinals[0]);
            if(succeeded)
            {
                text.text = evt.option1SuccessDescription;
                this.result.text = evt.option1SuccessResult;
            }
            else
            {
                text.text = evt.option1FailDescription;
                this.result.text = evt.option1FailResult;
            }
        }
        else if(result == 2)
        {
            succeeded = evt.OnChoiceOption2(UIManager.Instance.Ingame.Stats.LinkedCardinals[0]);
            if(succeeded)
            {
                text.text = evt.option2SuccessDescription;
                this.result.text = evt.option2SuccessResult;
            }
            else
            {
                text.text = evt.option2FailDescription;
                this.result.text = evt.option2FailResult;
            }
        }
        else
        {
            return;
        }

        if (InGameManager.Instance != null && InGameManager.Instance.EventManager != null)
        {
            InGameManager.Instance.EventManager.RecordChoice(evt.eventID, result, succeeded);
            StartCoroutine(AutoSaveChoiceResult());
        }
    }

    private static IEnumerator AutoSaveChoiceResult()
    {
        // 엔딩 전환은 같은 프레임에 이 오브젝트를 파괴하므로 완료 세이브를 다시 만들지 않는다.
        yield return null;
        if (SaveManager.Instance != null)
        {
            SaveManager.Instance.AutoSave();
        }
    }
}
