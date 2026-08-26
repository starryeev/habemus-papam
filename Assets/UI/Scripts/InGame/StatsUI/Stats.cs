using TMPro;
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

    public Image PortraitImage => Picture;

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
