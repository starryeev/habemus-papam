using TMPro;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
//간단한 UI
public class Stats : MonoBehaviour
{
    [Header("능력치")]
    [SerializeField] Image HP;
    [SerializeField] TextMeshProUGUI hp;
    [SerializeField] Image Piety;
    [SerializeField] TextMeshProUGUI piety;
    [SerializeField] Image Influence;
    [SerializeField] TextMeshProUGUI influence;
    [Space(10f)]
    [Header("초상화")]
    [SerializeField] TextMeshProUGUI Name;
    [SerializeField] Image Picture;
    [Space(10f)]
    [Header("캐릭터 설명")]
    [SerializeField] string Description;

    private readonly List<Graphic> graphics = new List<Graphic>();
    private readonly List<Color> originalColors = new List<Color>();
    private bool isGrayedOut;

    public Image PortraitImage => Picture;

    private void Awake()
    {
        Graphic[] childGraphics = GetComponentsInChildren<Graphic>(true);
        foreach (Graphic graphic in childGraphics)
        {
            graphics.Add(graphic);
            originalColors.Add(graphic.color);
        }
    }

    public void SetGrayedOut(bool shouldGrayOut)
    {
        if (isGrayedOut == shouldGrayOut)
        {
            return;
        }

        isGrayedOut = shouldGrayOut;
        for (int i = 0; i < graphics.Count; i++)
        {
            Color original = originalColors[i];
            if (!shouldGrayOut)
            {
                graphics[i].color = original;
                continue;
            }

            float luminance = original.r * 0.299f + original.g * 0.587f + original.b * 0.114f;
            graphics[i].color = new Color(luminance, luminance, luminance, original.a);
        }
    }

    public void SetHP(float hp, float maxHp = 10f)
    {
        this.hp.text = $"{(int)hp}";
        HP.fillAmount = hp / Mathf.Max(1f, maxHp);
    }
    public void SetPiety(float piety)
    {
        this.piety.text = $"{(int)piety}";
        Piety.fillAmount = piety/10;
    }
    public void SetInfluence(float inf)
    {
        influence.text = $"{(int)inf}";
        Influence.fillAmount = inf/10;
    }

    public void SetName(string displayName)
    {
        if (Name != null)
        {
            Name.text = displayName;
        }
    }
}
