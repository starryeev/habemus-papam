using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class EndingCutscenePlayer : MonoBehaviour
{
    private enum TextPlayState
    {
        Idle,
        FadingIn,
        Holding,
        Finished
    }

    [Header("Dialogue")]
    [SerializeField] private EndingDialogueTable dialogueTable;
    [SerializeField] private TextMeshProUGUI endingText;
    [SerializeField] private TextMeshProUGUI subText;
    [SerializeField] private TMP_FontAsset regularFont;
    [SerializeField] private TMP_FontAsset boldFont;
    [SerializeField] private float fadeInDuration = 1f;
    [SerializeField] private float holdDuration = 4f;

    [Header("Flow")]
    [SerializeField] private bool playOnStart = true;
    [SerializeField] private bool clearEndingResultAfterPlay = true;
    [SerializeField] private string mainMenuSceneName = "MainScene";
    [SerializeField] private Button skipButton;
    [SerializeField] private ClickChecker clickChecker;
    [SerializeField] private Button goToMainSceneButton;
    [SerializeField] private float goToMainSceneButtonFadeDuration = 1f;

    private readonly List<EndingTextPage> preparedLines = new List<EndingTextPage>();
    private CanvasGroup goToMainSceneButtonCanvasGroup;
    private Coroutine textSequenceCoroutine;
    private Coroutine buttonFadeCoroutine;
    private TextPlayState playState = TextPlayState.Idle;
    private bool completeCurrentFade;
    private bool advanceRequested;
    private int currentLineIndex = -1;

    private void Awake()
    {
        EnsureReferences();
        HideGoToMainSceneButton();
    }

    private void OnEnable()
    {
        EnsureReferences();

        if (clickChecker != null)
        {
            clickChecker.Clicked += HandleClickCheckerClicked;
        }

        if (skipButton != null)
        {
            skipButton.onClick.AddListener(SkipToLastText);
        }

        if (goToMainSceneButton != null)
        {
            goToMainSceneButton.onClick.AddListener(LoadMainMenu);
        }
    }

    private void Start()
    {
        HideGoToMainSceneButton();

        if (playOnStart)
        {
            PlaySelectedEnding();
        }
    }

    private void OnDisable()
    {
        if (clickChecker != null)
        {
            clickChecker.Clicked -= HandleClickCheckerClicked;
        }

        if (skipButton != null)
        {
            skipButton.onClick.RemoveListener(SkipToLastText);
        }

        if (goToMainSceneButton != null)
        {
            goToMainSceneButton.onClick.RemoveListener(LoadMainMenu);
        }
    }

    public void PlaySelectedEnding()
    {
        EnsureReferences();
        PrepareLines();

        if (textSequenceCoroutine != null)
        {
            StopCoroutine(textSequenceCoroutine);
        }

        textSequenceCoroutine = StartCoroutine(PlayTextSequence());
    }

    private void EnsureReferences()
    {
        if (endingText == null)
        {
            GameObject textObject = GameObject.Find("UI/EndingUI/EndingText");
            if (textObject == null)
            {
                textObject = GameObject.Find("EndingText");
            }

            endingText = textObject != null ? textObject.GetComponent<TextMeshProUGUI>() : null;
        }

        if (subText == null)
        {
            GameObject subTextObject = GameObject.Find("UI/EndingUI/SubText");
            if (subTextObject == null)
            {
                subTextObject = GameObject.Find("SubText");
            }

            subText = subTextObject != null ? subTextObject.GetComponent<TextMeshProUGUI>() : null;
        }

        RegisterFontAssets();
        ConfigureText(endingText);
        ConfigureText(subText);
        ClearText(subText);

        if (skipButton == null)
        {
            GameObject skipObject = GameObject.Find("UI/EndingUI/SkipBtn");
            if (skipObject == null)
            {
                skipObject = GameObject.Find("SkipBtn");
            }

            skipButton = skipObject != null ? skipObject.GetComponent<Button>() : null;
        }

        if (clickChecker == null)
        {
            GameObject clickObject = GameObject.Find("UI/EndingUI/ClickChecker");
            if (clickObject == null)
            {
                clickObject = GameObject.Find("ClickChecker");
            }

            if (clickObject != null)
            {
                clickChecker = clickObject.GetComponent<ClickChecker>();
                if (clickChecker == null)
                {
                    clickChecker = clickObject.AddComponent<ClickChecker>();
                }
            }
        }

        EnsureGoToMainSceneButton();
    }

    private void PrepareLines()
    {
        preparedLines.Clear();
        currentLineIndex = -1;

        if (dialogueTable == null)
        {
            Debug.LogWarning("[Ending] Dialogue table is missing.");
            return;
        }

        IReadOnlyList<EndingDialogueLine> lines = dialogueTable.GetLines(EndingResult.Current);
        if (lines == null || lines.Count == 0)
        {
            Debug.LogWarning($"[Ending] No dialogue lines found for {EndingResult.Current}.");
            return;
        }

        foreach (EndingDialogueLine line in lines)
        {
            if (line == null || !ShouldShowLine(line))
            {
                continue;
            }

            string rawText = line.RawText;
            if (string.IsNullOrWhiteSpace(rawText))
            {
                continue;
            }

            string formattedText = FormatLine(rawText);
            if (line.DisplayMode == EndingLineDisplayMode.SubTextWithPrevious)
            {
                if (preparedLines.Count == 0)
                {
                    preparedLines.Add(new EndingTextPage(string.Empty, formattedText));
                }
                else
                {
                    preparedLines[preparedLines.Count - 1].SubText = formattedText;
                }

                continue;
            }

            preparedLines.Add(new EndingTextPage(formattedText, string.Empty));
        }
    }

    private bool ShouldShowLine(EndingDialogueLine line)
    {
        switch (line.Condition)
        {
            case EndingLineCondition.PlayerPietyGreaterThanInfluence:
                return EndingContext.PlayerPiety > EndingContext.PlayerInfluence;
            case EndingLineCondition.PlayerInfluenceGreaterThanPiety:
                return EndingContext.PlayerInfluence > EndingContext.PlayerPiety;
            case EndingLineCondition.TriggerEventOption:
                return string.Equals(EndingContext.TriggerEventId, line.TriggerEventId, System.StringComparison.OrdinalIgnoreCase) &&
                       EndingContext.SelectedOptionIndex == line.TriggerOptionIndex;
            default:
                return true;
        }
    }

    private IEnumerator PlayTextSequence()
    {
        if (endingText == null || preparedLines.Count == 0)
        {
            playState = TextPlayState.Finished;
            ShowGoToMainSceneButton();
            yield break;
        }

        for (currentLineIndex = 0; currentLineIndex < preparedLines.Count; currentLineIndex++)
        {
            ApplyTextPage(preparedLines[currentLineIndex], 0f);

            completeCurrentFade = false;
            advanceRequested = false;
            playState = TextPlayState.FadingIn;

            float fadeElapsed = 0f;
            float fadeDuration = Mathf.Max(0f, fadeInDuration);

            while (fadeElapsed < fadeDuration && !completeCurrentFade)
            {
                fadeElapsed += Time.unscaledDeltaTime;
                SetTextAlpha(fadeDuration <= 0f ? 1f : Mathf.Clamp01(fadeElapsed / fadeDuration));
                yield return null;
            }

            SetTextAlpha(1f);
            completeCurrentFade = false;
            playState = TextPlayState.Holding;

            float holdElapsed = 0f;
            float waitDuration = Mathf.Max(0f, holdDuration);

            while (holdElapsed < waitDuration && !advanceRequested)
            {
                holdElapsed += Time.unscaledDeltaTime;
                yield return null;
            }

            advanceRequested = false;
        }

        playState = TextPlayState.Finished;
        textSequenceCoroutine = null;

        if (clearEndingResultAfterPlay)
        {
            EndingResult.Clear();
        }

        ShowGoToMainSceneButton();
    }

    private void HandleClickCheckerClicked()
    {
        if (playState == TextPlayState.FadingIn)
        {
            completeCurrentFade = true;
        }
        else if (playState == TextPlayState.Holding)
        {
            advanceRequested = true;
        }
    }

    private void SkipToLastText()
    {
        if (preparedLines.Count == 0)
        {
            PrepareLines();
        }

        if (textSequenceCoroutine != null)
        {
            StopCoroutine(textSequenceCoroutine);
            textSequenceCoroutine = null;
        }

        if (endingText != null && preparedLines.Count > 0)
        {
            currentLineIndex = preparedLines.Count - 1;
            ApplyTextPage(preparedLines[currentLineIndex], 1f);
        }

        playState = TextPlayState.Finished;

        if (clearEndingResultAfterPlay)
        {
            EndingResult.Clear();
        }

        ShowGoToMainSceneButton();
    }

    private void ApplyTextPage(EndingTextPage page, float alpha)
    {
        if (endingText != null)
        {
            endingText.text = page.MainText;
            endingText.alpha = alpha;
        }

        if (subText != null)
        {
            subText.text = page.SubText;
            subText.alpha = string.IsNullOrEmpty(page.SubText) ? 0f : alpha;
        }
    }

    private void SetTextAlpha(float alpha)
    {
        if (endingText != null)
        {
            endingText.alpha = alpha;
        }

        if (subText != null && !string.IsNullOrEmpty(subText.text))
        {
            subText.alpha = alpha;
        }
    }

    private void ConfigureText(TextMeshProUGUI targetText)
    {
        if (targetText == null)
        {
            return;
        }

        if (regularFont != null)
        {
            targetText.font = regularFont;
        }

        targetText.richText = true;
    }

    private static void ClearText(TextMeshProUGUI targetText)
    {
        if (targetText == null)
        {
            return;
        }

        targetText.text = string.Empty;
        targetText.alpha = 0f;
    }

    private void RegisterFontAssets()
    {
        if (regularFont != null)
        {
            MaterialReferenceManager.AddFontAsset(regularFont);
        }

        if (boldFont != null)
        {
            MaterialReferenceManager.AddFontAsset(boldFont);
        }

        RegisterFallbackFont(regularFont, boldFont);

        if (boldFont != null)
        {
            List<TMP_FontAsset> fallbackFonts = TMP_Settings.fallbackFontAssets;
            if (fallbackFonts == null)
            {
                fallbackFonts = new List<TMP_FontAsset>();
                TMP_Settings.fallbackFontAssets = fallbackFonts;
            }

            if (!fallbackFonts.Contains(boldFont))
            {
                fallbackFonts.Add(boldFont);
            }
        }
    }

    private static void RegisterFallbackFont(TMP_FontAsset baseFont, TMP_FontAsset fallbackFont)
    {
        if (baseFont == null || fallbackFont == null || baseFont == fallbackFont)
        {
            return;
        }

        if (baseFont.fallbackFontAssetTable == null)
        {
            baseFont.fallbackFontAssetTable = new List<TMP_FontAsset>();
        }

        if (!baseFont.fallbackFontAssetTable.Contains(fallbackFont))
        {
            baseFont.fallbackFontAssetTable.Add(fallbackFont);
        }
    }

    private string FormatLine(string rawText)
    {
        return ApplyBoldTags(ReplacePlaceholders(rawText));
    }

    private string ReplacePlaceholders(string rawText)
    {
        return Regex.Replace(rawText, "\\{([^{}]+)\\}", match =>
        {
            string key = match.Groups[1].Value.Trim();
            return ResolvePlaceholder(key);
        });
    }

    private string ResolvePlaceholder(string key)
    {
        string normalizedKey = key.Trim();
        string deparenthesizedKey = normalizedKey.Trim('(', ')').Trim();
        ActionRecordManager records = ActionRecordManager.Instance;

        switch (normalizedKey)
        {
            case "콘클라베 DAY":
                return EndingContext.ConclaveDay > 0
                    ? EndingContext.ConclaveDay.ToString()
                    : (records != null ? records.GetCurrentConclaveCount().ToString() : "0");
            case "기도 횟수":
                return records != null ? records.GetCurrentPrayCount().ToString() : "0";
            case "연설 횟수":
                return records != null ? records.GetCurrentSpeechCount().ToString() : "0";
            case "데이터에 저장되어 있는 몇번째 교주인지":
                return records != null ? records.GetCurrentPopeGeneration().ToString() : "0";
            case "플레이어의 이름":
                return GetFallback(EndingContext.PlayerName, "Player");
            case "당선된 NPC 의 이름":
                return GetFallback(EndingContext.ElectedNpcName, "NPC");
            case "엔딩 기준 1순위의 이름":
                return EndingContext.GetRankedCandidateName(0);
            case "엔딩 기준 2순위의 이름":
                return EndingContext.GetRankedCandidateName(1);
            case "엔딩 기준 3순위의 이름":
                return EndingContext.GetRankedCandidateName(2);
        }

        if (deparenthesizedKey == "후보 2")
        {
            return EndingContext.GetRankedCandidateName(1);
        }

        return "{" + normalizedKey + "}";
    }

    private string ApplyBoldTags(string text)
    {
        if (string.IsNullOrEmpty(text))
        {
            return string.Empty;
        }

        string boldFontName = boldFont != null ? boldFont.name : string.Empty;
        if (string.IsNullOrEmpty(boldFontName))
        {
            return text;
        }

        StringBuilder builder = new StringBuilder();
        int braceDepth = 0;

        for (int i = 0; i < text.Length; i++)
        {
            char current = text[i];

            if (current == '{')
            {
                braceDepth++;
                builder.Append(current);
                continue;
            }

            if (current == '}')
            {
                braceDepth = Mathf.Max(0, braceDepth - 1);
                builder.Append(current);
                continue;
            }

            if (current == '(' && braceDepth == 0)
            {
                int endIndex = FindClosingParenthesis(text, i + 1);
                builder.Append("<font=\"");
                builder.Append(boldFontName);
                builder.Append("\">");

                if (endIndex >= 0)
                {
                    builder.Append(text, i + 1, endIndex - i - 1);
                    builder.Append("</font>");
                    i = endIndex;
                }
                else
                {
                    builder.Append(text, i + 1, text.Length - i - 1);
                    builder.Append("</font>");
                    break;
                }

                continue;
            }

            builder.Append(current);
        }

        return builder.ToString();
    }

    private static int FindClosingParenthesis(string text, int startIndex)
    {
        for (int i = startIndex; i < text.Length; i++)
        {
            if (text[i] == ')')
            {
                return i;
            }
        }

        return -1;
    }

    private static string GetFallback(string value, string fallback)
    {
        return string.IsNullOrWhiteSpace(value) ? fallback : value;
    }

    private void EnsureGoToMainSceneButton()
    {
        if (goToMainSceneButton == null)
        {
            Button[] buttons = FindObjectsByType<Button>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None);

            foreach (Button button in buttons)
            {
                if (button != null && button.gameObject.name == "GoToMainSceneBtn")
                {
                    goToMainSceneButton = button;
                    break;
                }
            }
        }

        if (goToMainSceneButton == null)
        {
            return;
        }

        goToMainSceneButtonCanvasGroup = goToMainSceneButton.GetComponent<CanvasGroup>();

        if (goToMainSceneButtonCanvasGroup == null)
        {
            goToMainSceneButtonCanvasGroup = goToMainSceneButton.gameObject.AddComponent<CanvasGroup>();
        }
    }

    private void HideGoToMainSceneButton()
    {
        if (buttonFadeCoroutine != null)
        {
            StopCoroutine(buttonFadeCoroutine);
            buttonFadeCoroutine = null;
        }

        if (goToMainSceneButton == null)
        {
            return;
        }

        goToMainSceneButton.interactable = false;
        goToMainSceneButton.gameObject.SetActive(false);

        if (goToMainSceneButtonCanvasGroup != null)
        {
            goToMainSceneButtonCanvasGroup.alpha = 0f;
            goToMainSceneButtonCanvasGroup.interactable = false;
            goToMainSceneButtonCanvasGroup.blocksRaycasts = false;
        }
    }

    private void ShowGoToMainSceneButton()
    {
        EnsureGoToMainSceneButton();

        if (goToMainSceneButton == null)
        {
            Debug.LogWarning("[Ending] GoToMainSceneBtn was not found. Returning to MainScene immediately.");
            LoadMainMenu();
            return;
        }

        if (buttonFadeCoroutine != null)
        {
            StopCoroutine(buttonFadeCoroutine);
        }

        buttonFadeCoroutine = StartCoroutine(FadeInGoToMainSceneButton());
    }

    private IEnumerator FadeInGoToMainSceneButton()
    {
        goToMainSceneButton.gameObject.SetActive(true);
        goToMainSceneButton.interactable = false;

        if (goToMainSceneButtonCanvasGroup == null)
        {
            EnsureGoToMainSceneButton();
        }

        float elapsed = 0f;
        float duration = Mathf.Max(0f, goToMainSceneButtonFadeDuration);

        if (goToMainSceneButtonCanvasGroup != null)
        {
            goToMainSceneButtonCanvasGroup.alpha = 0f;
            goToMainSceneButtonCanvasGroup.interactable = false;
            goToMainSceneButtonCanvasGroup.blocksRaycasts = false;
        }

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;

            if (goToMainSceneButtonCanvasGroup != null)
            {
                goToMainSceneButtonCanvasGroup.alpha = Mathf.Clamp01(elapsed / duration);
            }

            yield return null;
        }

        if (goToMainSceneButtonCanvasGroup != null)
        {
            goToMainSceneButtonCanvasGroup.alpha = 1f;
            goToMainSceneButtonCanvasGroup.interactable = true;
            goToMainSceneButtonCanvasGroup.blocksRaycasts = true;
        }

        goToMainSceneButton.interactable = true;
        buttonFadeCoroutine = null;
    }

    private void LoadMainMenu()
    {
        Time.timeScale = 1f;

        if (SaveManager.Instance != null)
        {
            SaveManager.Instance.CompleteCurrentGame();
        }

        if (ActionRecordManager.Instance != null)
        {
            ActionRecordManager.Instance.ClearCurrentRunStats();
        }

        if (!string.IsNullOrWhiteSpace(mainMenuSceneName))
        {
            SceneManager.LoadScene(mainMenuSceneName);
        }
    }
}

internal class EndingTextPage
{
    public EndingTextPage(string mainText, string subText)
    {
        MainText = mainText;
        SubText = subText;
    }

    public string MainText { get; }
    public string SubText { get; set; }
}
