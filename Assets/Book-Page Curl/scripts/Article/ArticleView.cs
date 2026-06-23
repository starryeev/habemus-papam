using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class ArticleView : MonoBehaviour
{
    [SerializeField] private ArticleLayoutType layoutType;
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text bodyText;
    [SerializeField] private Image articleImage;

    public ArticleLayoutType LayoutType => layoutType;

    public void Bind(ArticleData article)
    {
        if (article == null)
            return;

        if (titleText != null && article.HasTitle)
            titleText.text = article.Title;

        if (bodyText != null && article.HasBody)
            bodyText.text = article.Body;

        if (articleImage != null && article.HasImage)
            articleImage.sprite = article.Image;
    }
}
