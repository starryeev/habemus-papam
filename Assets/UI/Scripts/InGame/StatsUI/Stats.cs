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
    public void SetHP(float hp)
    {
        this.hp.text = $"{(int)hp}";
        HP.fillAmount = hp/100;
    }
    public void SetPiety(float piety)
    {
        this.piety.text = $"{(int)piety}";
        Piety.fillAmount = piety/100;
    }
    public void SetInfluence(float inf)
    {
        influence.text = $"{(int)inf}";
        Influence.fillAmount = inf/100;
    }
}