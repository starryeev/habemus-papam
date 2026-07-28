using System.Collections;
using UnityEngine;
using UnityEngine.UI;

[CreateAssetMenu(fileName = "E12100", menuName = "Events/태양의 눈")]
public class E12100 : Event
{
    void Reset()
    {
        eventID = "E12100";
        eventName = "태양의 눈";
        maxAppear = 2;

        eventWeightBase = 30f;
        eventWeightMultiplier = 0f;

        option1Chance = 1f;
        option2Chance = 0.5f;

        // 선행 충돌 이벤트는 일단 인스펙터에서 드래그드롭으로처리
        //preEvents.Add(InGameManager.Instance.EventManager.GetEventById("E11100"));
    }

    public override bool CanChoiceOption1(Cardinal performer)
    {
        if(performer.Piety >= 30f) return true;
        else return false;
    }

    public override bool CanChoiceOption2(Cardinal performer)
    {
        return true;
    }

    public override bool OnChoiceOption1(Cardinal performer)
    {
        if(Random.value > option1Chance) return false;
        if(!CanChoiceOption1(performer)) return false;

        performer.ChangeInfluence(5f);

        return true;
    }


    public override bool OnChoiceOption2(Cardinal performer)
    {
        if(!CanChoiceOption2(performer)) return false;

        if(Random.value <= option2Chance)
        {
            performer.ChangeHp(-10f);
            return true;
        }
        else
        {
            performer.ChangeHp(-20f);
            performer.StartCoroutine(PlayWhiteFlash());
            return false;
        }
    }

    private static IEnumerator PlayWhiteFlash()
    {
        GameObject overlay = new GameObject("E12100_WhiteFlash", typeof(Canvas));
        Canvas canvas = overlay.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = short.MaxValue;

        GameObject imageObject = new GameObject(
            "White",
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(Image));
        imageObject.transform.SetParent(overlay.transform, false);

        RectTransform rectTransform = imageObject.GetComponent<RectTransform>();
        rectTransform.anchorMin = Vector2.zero;
        rectTransform.anchorMax = Vector2.one;
        rectTransform.offsetMin = Vector2.zero;
        rectTransform.offsetMax = Vector2.zero;

        Image image = imageObject.GetComponent<Image>();
        image.color = Color.white;
        image.raycastTarget = false;

        const float holdDuration = 0.12f;
        const float fadeDuration = 0.45f;
        yield return new WaitForSecondsRealtime(holdDuration);

        float elapsed = 0f;
        while (elapsed < fadeDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            image.color = new Color(1f, 1f, 1f, 1f - Mathf.Clamp01(elapsed / fadeDuration));
            yield return null;
        }

        Destroy(overlay);
    }
}
