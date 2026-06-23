using System;
using UnityEngine;

[Serializable]
public sealed class FixedArticleBinding
{
    [SerializeField] private ArticleView view;
    [SerializeField] private ArticleData article;

    public ArticleView View => view;
    public ArticleData Article => article;
    public bool IsValid => view != null && article != null;

    public void Bind()
    {
        if (IsValid)
            view.Bind(article);
    }
}
