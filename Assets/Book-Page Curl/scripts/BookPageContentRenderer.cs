using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public sealed class BookPageContentRenderer
{
    const string PageContentRootName = "PageContentRoot";

    readonly Dictionary<Image, int> slotPageIndexes = new Dictionary<Image, int>();
    readonly HashSet<int> warnedMissingPanelPages = new HashSet<int>();

    Sprite background;
    Sprite[] bookPages;
    bool usePagePanels;
    RectTransform[] pagePanels;

    public int PageCount
    {
        get
        {
            if (usePagePanels && pagePanels != null && pagePanels.Length > 0)
                return pagePanels.Length;
            return bookPages != null ? bookPages.Length : 0;
        }
    }

    public void Configure(Sprite background, Sprite[] bookPages, bool usePagePanels, RectTransform[] pagePanels)
    {
        this.background = background;
        this.bookPages = bookPages;
        this.usePagePanels = usePagePanels;
        this.pagePanels = pagePanels;
    }

    public void SetPageContent(Image pageSlot, int pageIndex)
    {
        if (!pageSlot)
            return;

        bool shouldUsePanel = TryGetPagePanel(pageIndex, out RectTransform pagePanel);
        if (IsCurrentContent(pageSlot, pageIndex, shouldUsePanel))
            return;

        ClearPageContent(pageSlot);
        slotPageIndexes[pageSlot] = pageIndex;

        if (pageIndex < 0 || pageIndex >= PageCount)
        {
            pageSlot.sprite = background;
            return;
        }

        if (shouldUsePanel)
        {
            pageSlot.sprite = background;
            RectTransform contentRoot = CreatePageContentRoot(pageSlot);
            RectTransform content = Object.Instantiate(pagePanel, contentRoot);
            StretchToParent(content);
            SetChildGraphicsMaskable(content);
            content.gameObject.SetActive(true);
            return;
        }

        pageSlot.sprite = GetPageSprite(pageIndex);
    }

    public void ClearPageContent(Image pageSlot)
    {
        if (!pageSlot)
            return;

        slotPageIndexes.Remove(pageSlot);
        ClearPageContentRoots(pageSlot.rectTransform);
    }

    bool IsCurrentContent(Image pageSlot, int pageIndex, bool shouldUsePanel)
    {
        if (!slotPageIndexes.TryGetValue(pageSlot, out int currentPageIndex) || currentPageIndex != pageIndex)
            return false;

        bool hasContentRoot = HasPageContentRoot(pageSlot.rectTransform);
        if (pageIndex < 0 || pageIndex >= PageCount)
        {
            if (hasContentRoot)
                return false;

            pageSlot.sprite = background;
            return true;
        }

        return shouldUsePanel ? hasContentRoot : !hasContentRoot;
    }

    bool TryGetPagePanel(int pageIndex, out RectTransform pagePanel)
    {
        pagePanel = null;
        if (!usePagePanels || pagePanels == null || pageIndex < 0 || pageIndex >= pagePanels.Length)
            return false;

        pagePanel = pagePanels[pageIndex];
        if (pagePanel)
            return true;

        if (warnedMissingPanelPages.Add(pageIndex))
            Debug.LogWarning("Book page panel is missing at index " + pageIndex + ".");
        return false;
    }

    Sprite GetPageSprite(int pageIndex)
    {
        if (bookPages != null && pageIndex >= 0 && pageIndex < bookPages.Length)
            return bookPages[pageIndex];
        return background;
    }

    RectTransform CreatePageContentRoot(Image pageSlot)
    {
        GameObject rootObject = new GameObject(PageContentRootName, typeof(RectTransform));
        RectTransform root = rootObject.GetComponent<RectTransform>();
        root.SetParent(pageSlot.rectTransform, false);
        StretchToParent(root);
        root.SetAsLastSibling();
        return root;
    }

    void StretchToParent(RectTransform rectTransform)
    {
        rectTransform.anchorMin = Vector2.zero;
        rectTransform.anchorMax = Vector2.one;
        rectTransform.offsetMin = Vector2.zero;
        rectTransform.offsetMax = Vector2.zero;
        rectTransform.localScale = Vector3.one;
        rectTransform.localRotation = Quaternion.identity;
        rectTransform.anchoredPosition3D = Vector3.zero;
    }

    void SetChildGraphicsMaskable(RectTransform content)
    {
        MaskableGraphic[] graphics = content.GetComponentsInChildren<MaskableGraphic>(true);
        for (int i = 0; i < graphics.Length; i++)
            graphics[i].maskable = true;
    }

    bool HasPageContentRoot(RectTransform pageSlot)
    {
        for (int i = 0; i < pageSlot.childCount; i++)
        {
            if (pageSlot.GetChild(i).name == PageContentRootName)
                return true;
        }
        return false;
    }

    void ClearPageContentRoots(RectTransform pageSlot)
    {
        for (int i = pageSlot.childCount - 1; i >= 0; i--)
        {
            Transform child = pageSlot.GetChild(i);
            if (child.name == PageContentRootName)
                DestroyContentRoot(child.gameObject);
        }
    }

    void DestroyContentRoot(GameObject contentRoot)
    {
        if (!contentRoot)
            return;

        if (Application.isPlaying)
            Object.Destroy(contentRoot);
        else
            Object.DestroyImmediate(contentRoot);
    }
}
