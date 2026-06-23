using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "NewspaperData", menuName = "Newspaper/Newspaper Data")]
public sealed class NewspaperData : ScriptableObject
{
    [SerializeField] private List<ArticleData> text160WithoutImageArticles = new List<ArticleData>();
    [SerializeField] private List<ArticleData> text160WithImageArticles = new List<ArticleData>();
    [SerializeField] private List<ArticleData> text260WithoutImageArticles = new List<ArticleData>();
    [SerializeField] private List<ArticleData> text260WithImageArticles = new List<ArticleData>();

    public IReadOnlyList<ArticleData> GetArticles(ArticleLayoutType layoutType)
    {
        switch (layoutType)
        {
            case ArticleLayoutType.Text160WithoutImage:
                return text160WithoutImageArticles;
            case ArticleLayoutType.Text160WithImage:
                return text160WithImageArticles;
            case ArticleLayoutType.Text260WithoutImage:
                return text260WithoutImageArticles;
            case ArticleLayoutType.Text260WithImage:
                return text260WithImageArticles;
            default:
                return Array.Empty<ArticleData>();
        }
    }
}
