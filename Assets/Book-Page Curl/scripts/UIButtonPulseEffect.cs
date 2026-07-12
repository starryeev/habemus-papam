using System.Collections;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class UIButtonPulseEffect : MonoBehaviour
{
    [SerializeField] private Graphic targetGraphic;
    [SerializeField] private Color brightColor = Color.white;
    [SerializeField] private Color dimColor = new Color(0.65f, 0.65f, 0.65f, 1f);
    [SerializeField] private float duration = 0.6f;
    [SerializeField] private bool useUnscaledTime = true;

    private Coroutine pulseCoroutine;
    private Color originalColor;

    private void Reset()
    {
        targetGraphic = GetComponent<Graphic>();
    }

    private void Awake()
    {
        ResolveTargetGraphic();
    }

    private void OnEnable()
    {
        ResolveTargetGraphic();

        if (targetGraphic == null)
            return;

        originalColor = targetGraphic.color;
        StartPulse();
    }

    private void OnDisable()
    {
        StopPulse();
    }

    private void StartPulse()
    {
        StopPulse();
        pulseCoroutine = StartCoroutine(PulseRoutine());
    }

    private void StopPulse()
    {
        if (pulseCoroutine != null)
        {
            StopCoroutine(pulseCoroutine);
            pulseCoroutine = null;
        }

        if (targetGraphic != null)
            targetGraphic.color = originalColor;
    }

    private IEnumerator PulseRoutine()
    {
        while (true)
        {
            yield return LerpColor(brightColor, dimColor);
            yield return LerpColor(dimColor, brightColor);
        }
    }

    private IEnumerator LerpColor(Color from, Color to)
    {
        float elapsed = 0f;
        float safeDuration = Mathf.Max(duration, 0.01f);

        while (elapsed < safeDuration)
        {
            float t = elapsed / safeDuration;
            float easedT = Mathf.SmoothStep(0f, 1f, t);

            targetGraphic.color = Color.Lerp(from, to, easedT);
            elapsed += useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;

            yield return null;
        }

        targetGraphic.color = to;
    }

    private void ResolveTargetGraphic()
    {
        if (targetGraphic == null)
            targetGraphic = GetComponent<Graphic>();
    }
}
