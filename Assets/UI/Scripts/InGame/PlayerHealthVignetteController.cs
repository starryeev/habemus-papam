using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class PlayerHealthVignetteController : MonoBehaviour
{
    public static PlayerHealthVignetteController Instance { get; private set; }

    private const float CriticalHpThreshold = 2f;
    private const float LowHpMaxAlpha = 34f;
    private const float FirstDownMaxAlpha = 90f;
    private const float FinalGameOverMaxAlpha = 255f;

    [Header("References")]
    [SerializeField] private Image vignettingImage;
    [SerializeField] private CanvasGroup gameOverTextGroup;

    [Header("Auto Find Names")]
    [SerializeField] private string vignettingObjectName = "Vignetting";
    [SerializeField] private string gameOverTextObjectName = "TXT";

    private Cardinal player;
    private StateController playerStateController;
    private Coroutine lowHpPulseCoroutine;
    private Coroutine oneShotEffectCoroutine;
    private bool isFinalGameOverPlaying;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);
            return;
        }

        Instance = this;
        ResolveReferences();
        ResetVisuals();
    }

    private void OnEnable()
    {
        ResolveReferences();
        ResetVisuals();
    }

    private void OnDisable()
    {
        StopLowHpPulse();

        if (oneShotEffectCoroutine != null)
        {
            StopCoroutine(oneShotEffectCoroutine);
            oneShotEffectCoroutine = null;
        }
    }

    private void Update()
    {
        if (isFinalGameOverPlaying)
        {
            return;
        }

        ResolvePlayer();

        if (player == null)
        {
            StopLowHpPulse();
            return;
        }

        if (oneShotEffectCoroutine != null)
        {
            return;
        }

        if (IsPlayerInCutScene())
        {
            StopLowHpPulse();
            SetVignetteAlphaByte(0f);
            return;
        }

        if (player.Hp > 0f && player.Hp <= CriticalHpThreshold)
        {
            if (lowHpPulseCoroutine == null)
            {
                lowHpPulseCoroutine = StartCoroutine(LowHpPulseRoutine());
            }
        }
        else
        {
            StopLowHpPulse();
            SetVignetteAlphaByte(0f);
        }
    }

    public void PlayFirstPlayerDownEffect(Action onComplete)
    {
        if (isFinalGameOverPlaying)
        {
            return;
        }

        StopLowHpPulse();

        if (oneShotEffectCoroutine != null)
        {
            StopCoroutine(oneShotEffectCoroutine);
        }

        oneShotEffectCoroutine = StartCoroutine(FirstPlayerDownRoutine(onComplete));
    }

    public void PlayFinalGameOverEffect(Action onComplete)
    {
        StopLowHpPulse();

        if (oneShotEffectCoroutine != null)
        {
            StopCoroutine(oneShotEffectCoroutine);
            oneShotEffectCoroutine = null;
        }

        oneShotEffectCoroutine = StartCoroutine(FinalGameOverRoutine(onComplete));
    }

    private IEnumerator LowHpPulseRoutine()
    {
        while (true)
        {
            yield return FadeVignetteAlpha(GetVignetteAlphaByte(), LowHpMaxAlpha, 0.2f);
            yield return FadeVignetteAlpha(GetVignetteAlphaByte(), 0f, 0.6f);

            SetVignetteAlphaByte(0f);

            float hiddenDuration = Mathf.Max(0.2f, Mathf.Max(0f, player != null ? player.Hp : 0f) / CriticalHpThreshold + 0.2f);
            yield return WaitUnscaled(hiddenDuration);
        }
    }

    private IEnumerator FirstPlayerDownRoutine(Action onComplete)
    {
        yield return FadeVignetteAlpha(GetVignetteAlphaByte(), FirstDownMaxAlpha, 0.5f);
        yield return WaitUnscaled(1f);

        oneShotEffectCoroutine = null;

        if (IsPlayerInCutScene())
        {
            SetVignetteAlphaByte(0f);
        }

        onComplete?.Invoke();
    }

    private IEnumerator FinalGameOverRoutine(Action onComplete)
    {
        isFinalGameOverPlaying = true;
        ResolveReferences();

        SetTextAlpha(0f);
        SetVignetteAlphaByte(FinalGameOverMaxAlpha);
        yield return WaitUnscaled(1.5f);
        yield return FadeTextAlpha(0f, 1f, 1f);
        yield return WaitUnscaled(3f);

        onComplete?.Invoke();
    }

    private IEnumerator FadeVignetteAlpha(float fromByteAlpha, float toByteAlpha, float duration)
    {
        float elapsed = 0f;
        float safeDuration = Mathf.Max(0f, duration);

        if (safeDuration <= 0f)
        {
            SetVignetteAlphaByte(toByteAlpha);
            yield break;
        }

        while (elapsed < safeDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / safeDuration);
            SetVignetteAlphaByte(Mathf.Lerp(fromByteAlpha, toByteAlpha, t));
            yield return null;
        }

        SetVignetteAlphaByte(toByteAlpha);
    }

    private IEnumerator FadeTextAlpha(float from, float to, float duration)
    {
        float elapsed = 0f;
        float safeDuration = Mathf.Max(0f, duration);

        if (safeDuration <= 0f)
        {
            SetTextAlpha(to);
            yield break;
        }

        while (elapsed < safeDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / safeDuration);
            SetTextAlpha(Mathf.Lerp(from, to, t));
            yield return null;
        }

        SetTextAlpha(to);
    }

    private IEnumerator WaitUnscaled(float duration)
    {
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }
    }

    private void StopLowHpPulse()
    {
        if (lowHpPulseCoroutine == null)
        {
            return;
        }

        StopCoroutine(lowHpPulseCoroutine);
        lowHpPulseCoroutine = null;
    }

    private void ResolveReferences()
    {
        if (vignettingImage == null)
        {
            Transform vignettingTransform = transform.Find(vignettingObjectName);
            if (vignettingTransform == null)
            {
                GameObject vignettingObject = GameObject.Find(vignettingObjectName);
                vignettingTransform = vignettingObject != null ? vignettingObject.transform : null;
            }

            vignettingImage = vignettingTransform != null ? vignettingTransform.GetComponent<Image>() : null;
        }

        if (gameOverTextGroup == null)
        {
            Transform textTransform = transform.Find(gameOverTextObjectName);
            if (textTransform == null)
            {
                GameObject textObject = GameObject.Find(gameOverTextObjectName);
                textTransform = textObject != null ? textObject.transform : null;
            }

            if (textTransform != null)
            {
                gameOverTextGroup = textTransform.GetComponent<CanvasGroup>();
                if (gameOverTextGroup == null)
                {
                    gameOverTextGroup = textTransform.gameObject.AddComponent<CanvasGroup>();
                }
            }
        }
    }

    private void ResolvePlayer()
    {
        if (player != null)
        {
            return;
        }

        if (CardinalManager.Instance != null)
        {
            foreach (Cardinal cardinal in CardinalManager.Instance.Cardinals)
            {
                if (cardinal != null && cardinal.CompareTag("Player"))
                {
                    CachePlayer(cardinal);
                    return;
                }
            }
        }

        GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
        if (playerObject != null && playerObject.TryGetComponent(out Cardinal foundPlayer))
        {
            CachePlayer(foundPlayer);
        }
    }

    private void CachePlayer(Cardinal foundPlayer)
    {
        player = foundPlayer;
        playerStateController = player.GetComponent<StateController>();
    }

    private bool IsPlayerInCutScene()
    {
        return playerStateController != null &&
               playerStateController.CurrentState == CardinalState.CutScene;
    }

    private void ResetVisuals()
    {
        SetVignetteAlphaByte(0f);
        SetTextAlpha(0f);
    }

    private float GetVignetteAlphaByte()
    {
        return vignettingImage != null ? vignettingImage.color.a * 255f : 0f;
    }

    private void SetVignetteAlphaByte(float alpha)
    {
        if (vignettingImage == null)
        {
            return;
        }

        Color color = vignettingImage.color;
        color.a = Mathf.Clamp(alpha, 0f, 255f) / 255f;
        vignettingImage.color = color;
    }

    private void SetTextAlpha(float alpha)
    {
        if (gameOverTextGroup == null)
        {
            return;
        }

        gameOverTextGroup.alpha = Mathf.Clamp01(alpha);
        gameOverTextGroup.interactable = false;
        gameOverTextGroup.blocksRaycasts = false;
    }
}
