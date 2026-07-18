using System;
using System.Collections.Generic;

public sealed class UniqueRandomArticleSelector
{
    private readonly Random random;
    private readonly List<ArticleData> candidatePool = new List<ArticleData>();
    private readonly HashSet<ArticleData> uniqueCandidates = new HashSet<ArticleData>();

    public UniqueRandomArticleSelector()
        : this(Environment.TickCount)
    {
    }

    public UniqueRandomArticleSelector(int seed)
    {
        random = new Random(seed);
    }

    public bool TrySelect(
        IReadOnlyList<ArticleData> candidates,
        ISet<ArticleData> excludedArticles,
        out ArticleData selectedArticle)
    {
        BuildCandidatePool(candidates, excludedArticles);
        if (candidatePool.Count == 0)
        {
            selectedArticle = null;
            return false;
        }

        selectedArticle = candidatePool[random.Next(candidatePool.Count)];
        return true;
    }

    private void BuildCandidatePool(
        IReadOnlyList<ArticleData> candidates,
        ISet<ArticleData> excludedArticles)
    {
        candidatePool.Clear();
        uniqueCandidates.Clear();

        if (candidates == null)
            return;

        for (int i = 0; i < candidates.Count; i++)
        {
            ArticleData candidate = candidates[i];
            if (candidate == null)
                continue;

            if (excludedArticles != null && excludedArticles.Contains(candidate))
                continue;

            if (uniqueCandidates.Add(candidate))
                candidatePool.Add(candidate);
        }
    }
}
