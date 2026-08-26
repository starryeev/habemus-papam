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
            headlineText.text = headline;
        }
    }
}
