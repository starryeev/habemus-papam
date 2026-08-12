using TMPro;
using UnityEngine;
using UnityEngine.UI;

class ChoiceButton : MonoBehaviour
{
    [SerializeField] Sprite ButtonWithReq;
    [SerializeField] Sprite ButtonWithoutReq;
    [SerializeField] Button ButtonKoraji;
    [SerializeField] TextMeshProUGUI text;
    [SerializeField] private TextMeshProUGUI req;
    public bool IsConditionUnavailable { get; private set; }

    public void SetButton(string desc, string req = "")
    {
        SetConditionAvailable();

        if(req != "")
        {
            ButtonKoraji.image.sprite = ButtonWithReq;
            SetReq(req);
            SetText(desc);
        }
        else
        {
            ButtonKoraji.image.sprite = ButtonWithoutReq;
        SetReq("");
        SetText(desc);
        }
    }
    public void DisableButton()
    {
        IsConditionUnavailable = true;
        ButtonKoraji.image.color = new Color(0.5f, 0.5f, 0.5f);
        ButtonKoraji.interactable = false;
    }
    public void SetReq(string eventreq)
    {
        string replaced = eventreq.Replace("정치력", "<sprite name=\"pol\">정치력").
            Replace("경건함", "<sprite name=\"piety\">경건함").
            Replace("체력", "<sprite name=\"hp\">체력").
            Replace("이상", "<sprite name=\"upArrow\">").
            Replace("이하", "<sprite name=\"downArrow\">");
        req.text=replaced;
    }
    public void SetText(string eventdesc)
    {
        text.text = eventdesc;
    }
    public void Clear()
    {
        SetConditionAvailable();

        if (text != null) text.text = "";
        if (req != null) req.text = "";
    }

    private void SetConditionAvailable()
    {
        IsConditionUnavailable = false;

        if (ButtonKoraji != null)
        {
            ButtonKoraji.interactable = true;
            ButtonKoraji.image.color = Color.white;
        }
    }
}
