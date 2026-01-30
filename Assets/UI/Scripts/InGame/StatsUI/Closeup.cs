using System.Collections.Generic;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public class Closeup : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] Image Picture;
    [SerializeField] TextMeshProUGUI Name;
    [SerializeField] TextMeshProUGUI Title;
    [SerializeField] TextMeshProUGUI Description;
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
    [SerializeField] Sprite[] DummyPortraits = new Sprite[4];
    [SerializeField] string[] DummyTitles = new string[4];
    [SerializeField] string[] DummyDescriptions = new string[4];
    
    
    public void SetCardinal(Cardinal cardinal, int idx)
    {
        //초상화 및 설명 설정
        Picture.sprite = DummyPortraits[idx];
        Name.text = DummyNames[idx];
        Title.text = DummyTitles[idx];
        Description.text = DummyDescriptions[idx];

        SetStats(cardinal.Hp, cardinal.Piety, cardinal.Influence);
    }
    public void SetStats(float hp, float piety, float influence)
    {
        this.hp.text = $"{(int)hp}";
        HP.fillAmount = hp/100;
        this.piety.text = $"{(int)piety}";
        Piety.fillAmount = piety/100;
        this.influence.text = $"{(int)influence}";
        Influence.fillAmount = influence/100;
    }
}