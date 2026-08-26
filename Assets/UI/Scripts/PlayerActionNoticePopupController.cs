using System;
using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class PlayerActionNoticePopupController : MonoBehaviour
{
    private GameObject _additionalPanel;
    private GameObject _unavailablePanel;
    private TMP_Text _additionalTitle;
    private TMP_Text _additionalBody;
    private TMP_Text _additionalButtonLabel;
    private Button _additionalButton;
    private TMP_Text _unavailableTitle;
    private TMP_Text _unavailableBody;
    private TMP_Text _unavailableButtonLabel;
    private Button _unavailableButton;
    private Action _onClosed;

    public static PlayerActionNoticePopupController Instance { get; private set; }
    public bool IsOpen => _additionalPanel != null && _additionalPanel.activeSelf ||
        _unavailablePanel != null && _unavailablePanel.activeSelf;

    public static void Attach(GameObject host)
    {
        if (host == null) return;
        PlayerActionNoticePopupController controller = host.GetComponent<PlayerActionNoticePopupController>();
        if (controller == null) controller = host.AddComponent<PlayerActionNoticePopupController>();
        controller.Configure();
    }

    public void ShowAdditional(PlayerActionEffectData effect, Action onClosed)
    {
        if (effect == null) return;
        Show(_additionalPanel, _additionalTitle, _additionalBody, _additionalButtonLabel,
            "추가 행동!",
            $"{GetSourceTypeName(effect.SourceType)} {GetSourceName(effect)}의 효과로 이번 턴에 " +
            $"{effect.totalCount}번의 행동이 추가되었습니다!",
            "야르~", onClosed);
    }

    public void ShowUnavailable(PlayerActionEffectData effect, bool hasFuturePlayerAction, Action onClosed)
    {
        if (effect == null) return;
        string source = $"{GetSourceTypeName(effect.SourceType)} {GetSourceName(effect)}";
        string message = hasFuturePlayerAction
            ? $"{source}으로 인하여 플레이어 행동이 불가합니다!"
            : $"{source}으로 인하여 이번 턴에 더 이상의 행동이 불가능합니다.";
        Show(_unavailablePanel, _unavailableTitle, _unavailableBody, _unavailableButtonLabel,
            "행동 불가!", message, "확인", onClosed);
    }

    public void ShowNpcResults(IReadOnlyList<NpcActionResult> results, Action onClosed)
    {
        StringBuilder builder = new StringBuilder(192);
        builder.Append("후보들의 행동이 다음과 같이 진행되었습니다.");
        if (results != null)
        {
            for (int i = 0; i < results.Count; i++)
            {
                NpcActionResult result = results[i];
                if (!result.ShouldDisplay) continue;
                if (result.Behaviour == NPCBehaviour.Pray || result.Behaviour == NPCBehaviour.Speech)
                {
                    builder.Append('\n').Append(result.CandidateName).Append(" : ")
                        .Append(result.Behaviour == NPCBehaviour.Pray ? "기도 " : "연설 ")
                        .Append(result.Succeeded == true ? "성공!" : "실패!");
                }

                if (result.OutcomeState == NpcActionOutcomeState.KnockedOut)
                    builder.Append('\n').Append(result.CandidateName).Append("이 기절하였습니다!");
                else if (result.OutcomeState == NpcActionOutcomeState.Dead)
                    builder.Append('\n').Append(result.CandidateName).Append("이 사망하였습니다!");
            }
        }

        Show(_unavailablePanel, _unavailableTitle, _unavailableBody, _unavailableButtonLabel,
            "행동 불가!", builder.ToString(), "확인", onClosed);
    }

    private void Configure()
    {
        Instance = this;
        _additionalPanel = FindSceneObjectIncludingInactive("CanMovePanel");
        _unavailablePanel = FindSceneObjectIncludingInactive("CantMovePanel");
        ConfigurePanel(_additionalPanel, out _additionalTitle, out _additionalBody,
            out _additionalButton, out _additionalButtonLabel);
        ConfigurePanel(_unavailablePanel, out _unavailableTitle, out _unavailableBody,
            out _unavailableButton, out _unavailableButtonLabel);

        if (_additionalButton != null)
        {
            _additionalButton.onClick.RemoveListener(CloseCurrent);
            _additionalButton.onClick.AddListener(CloseCurrent);
        }
        if (_unavailableButton != null)
        {
            _unavailableButton.onClick.RemoveListener(CloseCurrent);
            _unavailableButton.onClick.AddListener(CloseCurrent);
        }

        _additionalPanel?.SetActive(false);
        _unavailablePanel?.SetActive(false);
    }

    private static void ConfigurePanel(GameObject panel, out TMP_Text title, out TMP_Text body,
        out Button button, out TMP_Text buttonLabel)
    {
        title = null;
        body = null;
        button = null;
        buttonLabel = null;
        if (panel == null) return;

        Transform titleTransform = FindDeepChild(panel.transform, "Sum");
        Transform buttonTransform = FindDeepChild(panel.transform, "Button");
        title = titleTransform != null ? titleTransform.GetComponent<TMP_Text>() : null;
        button = buttonTransform != null ? buttonTransform.GetComponent<Button>() : null;
        buttonLabel = buttonTransform != null ? buttonTransform.GetComponentInChildren<TMP_Text>(true) : null;

        Transform bodyTransform = FindDeepChild(panel.transform, "Description");
        if (bodyTransform != null)
        {
            body = bodyTransform.GetComponent<TMP_Text>();
            return;
        }

        GameObject bodyObject = new GameObject("Description", typeof(RectTransform),
            typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        RectTransform bodyRect = bodyObject.GetComponent<RectTransform>();
        bodyRect.SetParent(panel.transform, false);
        bodyRect.anchorMin = new Vector2(0.5f, 0.5f);
        bodyRect.anchorMax = new Vector2(0.5f, 0.5f);
        bodyRect.anchoredPosition = new Vector2(0f, 5f);
        bodyRect.sizeDelta = new Vector2(680f, 190f);

        body = bodyObject.GetComponent<TextMeshProUGUI>();
        if (title != null)
        {
            body.font = title.font;
            body.fontSharedMaterial = title.fontSharedMaterial;
            body.color = title.color;
        }
        body.fontSize = 24f;
        body.enableAutoSizing = true;
        body.fontSizeMin = 16f;
        body.fontSizeMax = 24f;
        body.alignment = TextAlignmentOptions.Center;
        body.textWrappingMode = TextWrappingModes.Normal;
        body.raycastTarget = false;
    }

    private void Show(GameObject panel, TMP_Text title, TMP_Text body, TMP_Text buttonLabel,
        string titleText, string bodyText, string buttonText, Action onClosed)
    {
        if (panel == null)
        {
            onClosed?.Invoke();
            return;
        }

        _onClosed = onClosed;
        if (_additionalPanel != null) _additionalPanel.SetActive(panel == _additionalPanel);
        if (_unavailablePanel != null) _unavailablePanel.SetActive(panel == _unavailablePanel);
        if (title != null) title.text = titleText;
        if (body != null) body.text = bodyText;
        if (buttonLabel != null) buttonLabel.text = buttonText;
    }

    private void CloseCurrent()
    {
        if (_additionalPanel != null) _additionalPanel.SetActive(false);
        if (_unavailablePanel != null) _unavailablePanel.SetActive(false);
        Action callback = _onClosed;
        _onClosed = null;
        callback?.Invoke();
    }

    private static string GetSourceTypeName(PlayerActionEffectSourceType sourceType)
    {
        return sourceType switch
        {
            PlayerActionEffectSourceType.Plot => "공작",
            PlayerActionEffectSourceType.Item => "아이템",
            PlayerActionEffectSourceType.Event => "이벤트",
            _ => "효과"
        };
    }

    private static string GetSourceName(PlayerActionEffectData effect)
    {
        return !string.IsNullOrWhiteSpace(effect.sourceName) ? effect.sourceName : effect.sourceId;
    }

    private static GameObject FindSceneObjectIncludingInactive(string objectName)
    {
        GameObject[] roots = UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects();
        for (int i = 0; i < roots.Length; i++)
        {
            Transform found = FindDeepChild(roots[i].transform, objectName);
            if (found != null) return found.gameObject;
        }
        return null;
    }

    private static Transform FindDeepChild(Transform parent, string childName)
    {
        if (parent == null) return null;
        if (parent.name == childName) return parent;
        for (int i = 0; i < parent.childCount; i++)
        {
            Transform found = FindDeepChild(parent.GetChild(i), childName);
            if (found != null) return found;
        }
        return null;
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }
}
