using System;
using System.Collections.Generic;
using System.Globalization;
using TMPro;
using UnityEngine;

public sealed class NewsBookInitializer : MonoBehaviour
{
    private const string PublishDateFormat = "yyyy.MM.dd";

    [Header("Data")]
    [SerializeField] private NewspaperData newspaperData;

    [Header("Article Bindings")]
    [SerializeField] private FixedArticleBinding[] fixedArticles;
    [SerializeField] private ArticleView[] randomArticleViews;

    [Header("Newspaper UI")]
    [SerializeField] private TMP_Text[] publishDateTexts = new TMP_Text[3];
    [SerializeField] private GameObject bookRoot;
    [SerializeField] private bool activateBookAfterInitialization = true;

    private readonly HashSet<ArticleData> unavailableArticles = new HashSet<ArticleData>();
    private readonly List<ArticleData> selectedArticles = new List<ArticleData>();
    private UniqueRandomArticleSelector randomSelector;

    public void InitializeNewGameNews()
    {
        if (!TryPrepareRandomArticles())
            return;

        BindFixedArticles();
        BindRandomArticles();
        BindPublishDate();

        if (activateBookAfterInitialization && bookRoot != null)
            bookRoot.SetActive(true);
    }

    private bool TryPrepareRandomArticles()
    {
        if (newspaperData == null)
        {
            Debug.LogError("Newspaper data is not assigned.", this);
            return false;
        }

        BuildUnavailableArticleSet();

        if (randomSelector == null)
            randomSelector = new UniqueRandomArticleSelector();

        selectedArticles.Clear();
        int requiredCount = randomArticleViews != null ? randomArticleViews.Length : 0;

        for (int i = 0; i < requiredCount; i++)
        {
            ArticleView articleView = randomArticleViews[i];
            if (articleView == null)
            {
                Debug.LogError("A random article view is missing at index " + i + ".", this);
                return false;
            }

            if (!randomSelector.TrySelect(
                    newspaperData.GetArticles(articleView.LayoutType),
                    unavailableArticles,
                    out ArticleData selectedArticle))
            {
                Debug.LogError(
                    "There are not enough unique random articles for layout type " +
                    articleView.LayoutType + " at index " + i + ".",
                    this);
                return false;
            }

            selectedArticles.Add(selectedArticle);
            unavailableArticles.Add(selectedArticle);
        }

        return ValidateFixedArticles();
    }

    private void BuildUnavailableArticleSet()
    {
        unavailableArticles.Clear();

        if (fixedArticles == null)
            return;

        for (int i = 0; i < fixedArticles.Length; i++)
        {
            FixedArticleBinding binding = fixedArticles[i];
            if (binding != null && binding.Article != null)
                unavailableArticles.Add(binding.Article);
        }
    }

    private bool ValidateFixedArticles()
    {
        if (fixedArticles == null)
            return true;

        for (int i = 0; i < fixedArticles.Length; i++)
        {
            FixedArticleBinding binding = fixedArticles[i];
            if (binding != null && binding.IsValid)
                continue;

            Debug.LogError("A fixed article binding is invalid at index " + i + ".", this);
            return false;
        }

        return true;
    }

    private void BindFixedArticles()
    {
        if (fixedArticles == null)
            return;

        for (int i = 0; i < fixedArticles.Length; i++)
            fixedArticles[i].Bind();
    }

    private void BindRandomArticles()
    {
        if (randomArticleViews == null)
            return;

        for (int i = 0; i < randomArticleViews.Length; i++)
            randomArticleViews[i].Bind(selectedArticles[i]);
    }

    private void BindPublishDate()
    {
        if (publishDateTexts == null)
            return;

        string publishDate = DateTime.Now.ToString(PublishDateFormat, CultureInfo.InvariantCulture);

        for (int i = 0; i < publishDateTexts.Length; i++)
        {
            if (publishDateTexts[i] != null)
                publishDateTexts[i].text = publishDate;
        }
    }

}
