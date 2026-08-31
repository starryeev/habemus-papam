using UnityEngine;
using TMPro;

public class Headline : MonoBehaviour
{
    private void Start()
    {
        TMP_Text headlineText = GetComponent<TMP_Text>();
        if (headlineText == null)
        {
            return;
        }

        if (PopeListHistoryPresenter.TryGetLatestPopeHeadline(out string headline))
        {
            headlineText.text = $"제 {ActionRecordManager.Instance.GetLatestPope()?.generation}대 교주 {ActionRecordManager.Instance.GetLatestPope()?.popeName} 선종";
        }
        else
        {
            headlineText.text = "태양교 교주 선종";
        }
    }
}
