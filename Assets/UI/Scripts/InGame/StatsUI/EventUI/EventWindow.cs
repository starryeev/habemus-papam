using TMPro;
using UnityEngine;

class EventWindow : MonoBehaviour
{
    [SerializeField] TextMeshProUGUI title;
    [SerializeField] TextMeshProUGUI text;
    [SerializeField] ChoiceButton ChoiceButton1;
    [SerializeField] ChoiceButton ChoiceButton2;
    private Event CurrentEvent;
    public void ShowEvent(Event evt)
    {
        CurrentEvent = evt;
        //텍스트 로딩
        title.text = CurrentEvent.eventName;
        text.text = CurrentEvent.eventDescription;
        //이벤트2 없다면 버튼 뜨지 않음
        if(evt.option2 != "")
        {
            ChoiceButton2.gameObject.SetActive(true);
            ChoiceButton2.SetButton(CurrentEvent.option2);
        }
        else ChoiceButton2.gameObject.SetActive(false);
        ChoiceButton1.SetButton(CurrentEvent.option1, CurrentEvent.option1Requirement);
        Cardinal performer = UIManager.Instance.Ingame.Stats.LinkedCardinals[0];
        
        //이벤트 진행이 불가능하다면 버튼 어둡게 처리하고 비활성화
        if (!CurrentEvent.CanChoiceOption1(performer)) ChoiceButton1.DisableButton();
        if (evt.option2 != "" && !CurrentEvent.CanChoiceOption2(performer)) ChoiceButton2.DisableButton();
    }
    public void ShowEvent(string id)
    {
        CurrentEvent = InGameManager.Instance.EventManager.GetEventById(id);

        title.text = CurrentEvent.eventName;
        text.text = CurrentEvent.eventDescription;
        ChoiceButton1.SetButton(CurrentEvent.option1, CurrentEvent.option1Requirement);
        ChoiceButton2.SetButton(CurrentEvent.option2);
    }
    public void Clear()
    {
        title.text = text.text = "";
        ChoiceButton1.Clear();
        ChoiceButton2.Clear();
    }
}
