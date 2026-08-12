using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;
//여기에 교황 판정 화면을 구현
public class CheckUI : MonoBehaviour
{
    private const string VoteOpenSfxName = "28 인게임- 투표함 개봉";
    private const string ButtonSpecialSfxName = "ButtonSpecial";
    private const float ResultRevealDuration = 0.3f;
    private const float ResultToVideoDelay = 3f;
    private const float ResultPunchScale = 1.15f;
    private static readonly Color ResultGoldColor = new Color32(255, 210, 70, 255);

    private Image img;
    [SerializeField] private Button SkipButton;
    [SerializeField] private Button Vote;
    [SerializeField] private Sprite[] sprites; //코루틴 돌릴 스프라이트.
    [SerializeField] private Animator anim;
    private int currentSprite = 0;
    private bool isClicked = false;
    
    private enum AnimState
    {
        None = 0,
        Enter,
        ElectWait,
        Elect,
        ElectEnd
    }
    [SerializeField] private AnimState animState;
    [SerializeField] private string ElectionMessage = "운명의 순간이다...\n제발 나만 아니면 돼.";
    [SerializeField] private string ElectionSubMessage = "투표함을 눌러 투표 결과를 확인하세요!";
    [SerializeField] private string[] JudgeMessage;
    [SerializeField] private string JudgeSubMessage = "투표함을 눌러 판정 결과를 확인하세요!";
    [SerializeField] private string miscMessage = "신탁 성공 확률";
    [SerializeField] private TextMeshProUGUI text;
    [SerializeField] private TextMeshProUGUI subText;
    [SerializeField] private TextMeshProUGUI miscText;
    [SerializeField] private TextMeshProUGUI probabilityText;
    [SerializeField] private float jackpotDuration = 3f;
    [SerializeField] private GameObject judgementVideoObject;
    [SerializeField] private VideoPlayer vp;
    [Header("Judgement Videos")]
    [Tooltip("당선 판정일 때 순서대로 재생할 영상입니다.")]
    [SerializeField] private VideoClip[] SuccessVideoQueue;
    [Tooltip("낙선 판정일 때 순서대로 재생할 영상입니다.")]
    [SerializeField] private VideoClip[] FailVideoQueue;
    private Coroutine jackpotCoroutine;
    private VideoClip[] activeVideoQueue;
    private int currentVideoIndex;
    private bool isPlayingJudgementVideos;
    private bool isWaitingForJudgementVideoClick;
    private bool isRevealingResult;
    private Coroutine resultRevealCoroutine;
    private Vector3 probabilityBaseScale;
    private Color probabilityBaseColor;
    private Color32 probabilityBaseOutlineColor;
    private float probabilityBaseOutlineWidth;
    private bool probabilityVisualCached;
    private float winProbability = 0; //ElectionManager에서 정해진 승리 확률.
    private int winner = -1;
    public void SetWinner(int i) {winner = i;}
    public void SetProbability(float f) {winProbability = f;}
    void Start()
    {
        img = GetComponent<Image>();
        if(SkipButton != null) SkipButton.onClick.AddListener(Skip);
        if(Vote != null)
        {
            Vote.onClick.AddListener(OnVote);
        }
        animState = 0;
        SetSprite(0);
        isClicked = false;
        text.text = "";
        subText.text = "";
        probabilityText.text = "";
        miscText.text = "";
        CacheProbabilityVisual();
        SetJudgementVideoObjectActive(false);
    }
    private void OnDisable()
    {
        if (jackpotCoroutine != null) StopCoroutine(jackpotCoroutine);
        if (resultRevealCoroutine != null) StopCoroutine(resultRevealCoroutine);
        jackpotCoroutine = null;
        resultRevealCoroutine = null;
        isRevealingResult = false;
        RestoreProbabilityVisual();
    }
    private void OnDestroy()
    {
        if (vp != null)
        {
            vp.loopPointReached -= OnJudgementVideoFinished;
            vp.prepareCompleted -= OnJudgementVideoPrepared;
            vp.errorReceived -= OnJudgementVideoError;
        }
    }
    public void SetSprite(int i)
    {
        if(currentSprite == i) return;
        currentSprite = i;
        img.sprite = sprites[i];
    }
    private void Skip() //스킵 버튼을 누르면 스킵.
    {
        if (isRevealingResult) return;
        if (animState == AnimState.ElectEnd)
        {
            RevealFinalResult();
            return;
        }
        if(animState == AnimState.None)
        {
            Debug.Log("투표 화면 오류!");
        }
        if(animState == AnimState.Enter || animState == AnimState.ElectWait || animState == AnimState.Elect)
        OnElectAnimFinished();
    }
    private void OnVote()
    {
        Debug.Log("투표함 클릭됨");
        if(animState == AnimState.ElectWait)
        {
            SoundManager.Instance.PlaySFX(VoteOpenSfxName);
            text.text = ElectionMessage;
            anim.Play("Elect", 0, 0f);
            animState = AnimState.Elect;
            return;
        }
        if(animState == AnimState.Elect)
        {
            Skip();
            return;
        }
        if(animState==AnimState.ElectEnd)
        {
            if (isPlayingJudgementVideos || isWaitingForJudgementVideoClick || isRevealingResult) return;
            RevealFinalResult();
        }
    }
    private void OnEnable()
    {
        animState = AnimState.Enter;
        text.text = ElectionMessage;
        subText.text = ElectionSubMessage;
        probabilityText.text = "";
        RestoreProbabilityVisual();
        isRevealingResult = false;
        Vote.interactable = true;
        if (SkipButton != null) SkipButton.interactable = true;
        anim.Play("Enter", 0, 0f);
        Vote.gameObject.SetActive(false);
        SetJudgementVideoObjectActive(false);
    }
    public void OnEnterAnimFinished()
    {
        text.text = ElectionMessage;
        subText.text = ElectionSubMessage;
        animState = AnimState.ElectWait;
        Vote.gameObject.SetActive(true);
        anim.Play("Idle", 0, 0f);
    }
    public void OnElectAnimFinished()
    {
        if (animState == AnimState.ElectEnd) return;
        animState = AnimState.ElectEnd;
        SetSprite(4+(winner%4));

        string s = JudgeMessage[winner];
        text.text = s.Replace("NAME", GetWinnerDisplayName());
        miscText.text = miscMessage;

        jackpotCoroutine = StartCoroutine(JackpotRoutine(winProbability));
    }
    private string GetWinnerDisplayName()
    {
        Cardinal candidate = ElectionManager.Instance != null
            ? ElectionManager.Instance.CurrentWinnerCandidate
            : null;
        string fallbackName = candidate != null ? candidate.name : string.Empty;
        GameNameSaveData names = SaveManager.Instance != null
            ? SaveManager.Instance.CurrentGameNames
            : null;

        if (names == null) return fallbackName;

        string displayName = winner == 0
            ? names.playerName
            : winner > 0 && names.npcNames != null && winner - 1 < names.npcNames.Count
                ? names.npcNames[winner - 1]
                : string.Empty;

        return string.IsNullOrWhiteSpace(displayName) ? fallbackName : displayName;
    }
    private IEnumerator JackpotRoutine(float finalProb)
    {
        float elapsed = 0f;

        while (elapsed < jackpotDuration)
        {
            elapsed += Time.deltaTime;

            float randomTick = Random.Range(0f, 100f);

            if (probabilityText != null)
            {
                probabilityText.text = $" <color=white>{randomTick:F1}%</color>";
            }

            yield return null;
        }

        jackpotCoroutine = null;
        RevealFinalResult();
    }
    private void RevealFinalResult()
    {
        if (isRevealingResult || isPlayingJudgementVideos || isWaitingForJudgementVideoClick) return;

        if (jackpotCoroutine != null)
        {
            StopCoroutine(jackpotCoroutine);
            jackpotCoroutine = null;
        }

        resultRevealCoroutine = StartCoroutine(ResultRevealRoutine());
    }
    private IEnumerator ResultRevealRoutine()
    {
        isRevealingResult = true;
        Vote.interactable = false;
        if (SkipButton != null) SkipButton.interactable = false;

        CacheProbabilityVisual();
        if (probabilityText != null)
        {
            probabilityText.text = $" {winProbability:F1}%";
        }

        float elapsed = 0f;
        while (elapsed < ResultRevealDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float progress = Mathf.Clamp01(elapsed / ResultRevealDuration);
            float easedProgress = Mathf.SmoothStep(0f, 1f, progress);

            if (probabilityText != null)
            {
                probabilityText.rectTransform.localScale = probabilityBaseScale *
                    Mathf.Lerp(ResultPunchScale, 1f, easedProgress);
                probabilityText.color = Color.Lerp(ResultGoldColor, probabilityBaseColor, easedProgress);
                probabilityText.outlineColor = (Color32)Color.Lerp(
                    ResultGoldColor, probabilityBaseOutlineColor, easedProgress);
                probabilityText.outlineWidth = Mathf.Lerp(0.25f, probabilityBaseOutlineWidth, easedProgress);
            }

            yield return null;
        }

        RestoreProbabilityVisual();

        float remainingDelay = Mathf.Max(0f, ResultToVideoDelay - ResultRevealDuration);
        if (remainingDelay > 0f)
        {
            yield return new WaitForSecondsRealtime(remainingDelay);
        }

        resultRevealCoroutine = null;
        isRevealingResult = false;
        SoundManager.Instance.PlaySFX(ButtonSpecialSfxName);
        PlayJudgementVideos();
    }
    private void CacheProbabilityVisual()
    {
        if (probabilityVisualCached || probabilityText == null) return;

        probabilityBaseScale = probabilityText.rectTransform.localScale;
        probabilityBaseColor = probabilityText.color;
        probabilityBaseOutlineColor = probabilityText.outlineColor;
        probabilityBaseOutlineWidth = probabilityText.outlineWidth;
        probabilityVisualCached = true;
    }
    private void RestoreProbabilityVisual()
    {
        if (!probabilityVisualCached || probabilityText == null) return;

        probabilityText.rectTransform.localScale = probabilityBaseScale;
        probabilityText.color = probabilityBaseColor;
        probabilityText.outlineColor = probabilityBaseOutlineColor;
        probabilityText.outlineWidth = probabilityBaseOutlineWidth;
    }
    private void PlayJudgementVideos()
    {
        if (isPlayingJudgementVideos) return;
        if (isWaitingForJudgementVideoClick) return;

        bool isElected = ElectionManager.Instance != null && ElectionManager.Instance.IsElected;
        activeVideoQueue = isElected ? SuccessVideoQueue : FailVideoQueue;
        currentVideoIndex = 0;

        if (vp == null || activeVideoQueue == null || activeVideoQueue.Length == 0)
        {
            ElectionManager.Instance.GetNextScenes();
            return;
        }

        isPlayingJudgementVideos = true;
        isWaitingForJudgementVideoClick = false;
        SetJudgementVideoObjectActive(true);
        vp.loopPointReached -= OnJudgementVideoFinished;
        vp.prepareCompleted -= OnJudgementVideoPrepared;
        vp.errorReceived -= OnJudgementVideoError;
        vp.loopPointReached += OnJudgementVideoFinished;
        vp.prepareCompleted += OnJudgementVideoPrepared;
        vp.errorReceived += OnJudgementVideoError;
        PlayNextJudgementVideo();
    }

    private void PlayNextJudgementVideo()
    {
        while (currentVideoIndex < activeVideoQueue.Length && activeVideoQueue[currentVideoIndex] == null)
        {
            currentVideoIndex++;
        }

        if (currentVideoIndex >= activeVideoQueue.Length)
        {
            WaitForJudgementVideoClick();
            return;
        }

        vp.Stop();
        ClearJudgementVideoTexture();
        vp.clip = activeVideoQueue[currentVideoIndex++];
        vp.time = 0;
        vp.frame = 0;
        vp.Prepare();
    }

    private void OnJudgementVideoPrepared(VideoPlayer source)
    {
        if (!isPlayingJudgementVideos) return;

        source.time = 0;
        source.frame = 0;
        source.Play();
    }

    private void OnJudgementVideoFinished(VideoPlayer source)
    {
        PlayNextJudgementVideo();
    }

    private void OnJudgementVideoError(VideoPlayer source, string message)
    {
        Debug.LogWarning($"Judgement video playback failed: {message}");
        FinishJudgementVideos();
    }

    public void OnJudgementVideoClicked()
    {
        if (!isWaitingForJudgementVideoClick) return;

        FinishJudgementVideos();
    }

    private void WaitForJudgementVideoClick()
    {
        isPlayingJudgementVideos = false;
        isWaitingForJudgementVideoClick = true;

        if (vp != null)
        {
            vp.Pause();
            vp.loopPointReached -= OnJudgementVideoFinished;
            vp.prepareCompleted -= OnJudgementVideoPrepared;
            vp.errorReceived -= OnJudgementVideoError;
        }
    }

    private void FinishJudgementVideos()
    {
        isPlayingJudgementVideos = false;
        isWaitingForJudgementVideoClick = false;
        if (vp != null)
        {
            vp.Stop();
            vp.clip = null;
            vp.loopPointReached -= OnJudgementVideoFinished;
            vp.prepareCompleted -= OnJudgementVideoPrepared;
            vp.errorReceived -= OnJudgementVideoError;
        }
        ClearJudgementVideoTexture();
        SetJudgementVideoObjectActive(false);
        ElectionManager.Instance.GetNextScenes();
    }

    private void SetJudgementVideoObjectActive(bool active)
    {
        if (judgementVideoObject != null)
        {
            judgementVideoObject.SetActive(active);
        }
    }

    private void ClearJudgementVideoTexture()
    {
        if (vp == null || vp.targetTexture == null) return;

        RenderTexture previous = RenderTexture.active;
        RenderTexture.active = vp.targetTexture;
        RenderTexture.active = previous;
    }

    public void DoNothing()
    {
    }//애니메이션을 위한 더미함수.
}
