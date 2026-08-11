using UnityEngine;

public static class SettingsPulseColorEvaluator
{
    public static Color Evaluate(
        Color baseColor,
        float elapsedTime,
        float darkDuration,
        float brightDuration,
        float darkenAmount)
    {
        float cycleDuration = darkDuration + brightDuration;
        if (cycleDuration <= Mathf.Epsilon)
        {
            return baseColor;
        }

        float cycleTime = Mathf.Repeat(Mathf.Max(0f, elapsedTime), cycleDuration);
        float transition = CalculateTransition(cycleTime, darkDuration, brightDuration);
        float easedTransition = Mathf.SmoothStep(0f, 1f, transition);
        Color darkColor = Color.Lerp(baseColor, Color.black, Mathf.Clamp01(darkenAmount));
        return Color.Lerp(baseColor, darkColor, easedTransition);
    }

    private static float CalculateTransition(
        float cycleTime,
        float darkDuration,
        float brightDuration)
    {
        if (cycleTime < darkDuration && darkDuration > Mathf.Epsilon)
        {
            return cycleTime / darkDuration;
        }

        if (brightDuration <= Mathf.Epsilon)
        {
            return 0f;
        }

        return 1f - ((cycleTime - darkDuration) / brightDuration);
    }
}
