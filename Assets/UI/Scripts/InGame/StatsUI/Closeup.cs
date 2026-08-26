using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class Closeup : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] Image Picture;
    [SerializeField] TextMeshProUGUI Name;
    [SerializeField] TextMeshProUGUI Title;
    [SerializeField] TextMeshProUGUI Description;
    [SerializeField] TextMeshProUGUI Passive;
    [SerializeField] Image HP;
    [SerializeField] TextMeshProUGUI hp;
    [SerializeField] Image Piety;
    [SerializeField] TextMeshProUGUI piety;
    [SerializeField] Image Influence;
    [SerializeField] TextMeshProUGUI influence;
    [Space(10f)]
    [Header("캐릭터 설명(임시)")]
    //임시 데이터. 추후 구조가 확정되면 이동 또는 삭제할 것!!
    [SerializeField] string[] DummyNames = new string[4];
    [SerializeField] string[] DummyTitles = new string[4];
    [SerializeField] string[] DummyDescriptions = new string[4];
    [SerializeField] string[] DummyPassives = new string[4];

    public void SetCardinal(Cardinal cardinal, int idx, Image sourcePortrait)
    {
        //초상화 및 설명 설정
        SetPortrait(sourcePortrait);
        Name.text = DummyNames[idx];
        Title.text = DummyTitles[idx];
        Description.text = DummyDescriptions[idx];
        Passive.text = DummyPassives[idx];

        SetStats(cardinal.Hp, cardinal.Piety, cardinal.Influence, cardinal.MaxHp);
    }

    private void SetPortrait(Image sourcePortrait)
    {
        if (Picture == null)
        {
            return;
        }

        if (sourcePortrait == null)
        {
            Picture.sprite = null;
            Picture.enabled = false;
            return;
        }

        Picture.sprite = sourcePortrait.sprite;
        Picture.color = sourcePortrait.color;
        Picture.enabled = sourcePortrait.enabled;
        Picture.preserveAspect = sourcePortrait.preserveAspect;

        RectTransform sourceRect = sourcePortrait.rectTransform;
        RectTransform targetRect = Picture.rectTransform;
        if (sourceRect == null || targetRect == null)
        {
            return;
        }

        targetRect.anchorMin = sourceRect.anchorMin;
        targetRect.anchorMax = sourceRect.anchorMax;
        targetRect.pivot = sourceRect.pivot;
        targetRect.anchoredPosition = sourceRect.anchoredPosition;
        targetRect.sizeDelta = sourceRect.sizeDelta;
        targetRect.localScale = sourceRect.localScale;
    }

    public void SetStats(float hp, float piety, float influence, float maxHp = 10f)
    {
        this.hp.text = $"{(int)hp}";
        HP.fillAmount = hp / Mathf.Max(1f, maxHp);
        this.piety.text = $"{(int)piety}";
        Piety.fillAmount = piety/10;
        this.influence.text = $"{(int)influence}";
        Influence.fillAmount = influence/10;
    }
}
