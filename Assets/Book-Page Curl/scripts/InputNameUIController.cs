using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class InputNameUIController : MonoBehaviour
{
    private const int MinNameLength = 1;
    private const int MaxNameLength = 10;
    private const string EmptyNameWarningMessage = "이름을 입력해주세요.";
    private const string TooLongNameWarningMessage = "이름은 10글자 이하로 입력해주세요.";
    private const string DuplicateCandidateNameWarningMessage = "다른 후보와 같은 이름은 사용할 수 없습니다.";
    private const string IncompleteHangulWarningMessage = "한글은 자음·모음을 조합하여 입력해주세요.";

    [SerializeField] private Book book;
    [SerializeField] private GameObject inputNamePopup;
    [SerializeField] private GameObject dim;

    [SerializeField] private GameObject inputNameGroup;
    [SerializeField] private Button inputNameButton;
    [SerializeField] private UIButtonPulseEffect inputNamePulseEffect;
    [SerializeField] private TextMeshProUGUI playerNameDisplayText;
    [SerializeField] private TMP_InputField playerNameInputField;

    [SerializeField] private TextMeshProUGUI warningText;

    [SerializeField] private Button confirmButton;
    [SerializeField] private Button cancelButton;

    private int visiblePageMin = 2;
    private int visiblePageMax = 3;

    public string CurrentPlayerName { get; private set; } = string.Empty;
    public bool HasPlayerName => !string.IsNullOrWhiteSpace(CurrentPlayerName);
    public event Action OnPlayerNameConfirmed;

    private enum NameValidationResult
    {
        Valid,
        Empty,
        TooLong,
        DuplicateCandidateName,
        IncompleteHangul,
    }

    private void Awake()
    {
        ResolveBookReference();
        ResolveInputFieldReference();
        ResolveInputNamePulseEffect();
        ConfigureInputField();
        HideWarning();

        if (inputNameButton != null)
            inputNameButton.onClick.AddListener(ShowInputNamePopup);

        if (confirmButton != null)
            confirmButton.onClick.AddListener(OnClickConfirm);

        if (cancelButton != null)
            cancelButton.onClick.AddListener(OnClickCancel);
    }

    private void OnEnable()
    {
        ResolveBookReference();

        if (book != null)
        {
            book.OnFlipStarted += HideInputNameButton;
            book.OnFlipSettled += RefreshInputNameButton;
        }
    }

    private void Start()
    {
        RefreshInputNameButton();
    }

    private void OnDisable()
    {
        if (book != null)
        {
            book.OnFlipStarted -= HideInputNameButton;
            book.OnFlipSettled -= RefreshInputNameButton;
        }
    }

    public void ShowInputNamePopup()
    {
        ConfigureInputField();
        HideWarning();

        if (inputNamePopup != null)
            inputNamePopup.SetActive(true);

        if (dim != null)
            dim.SetActive(true);

        if (playerNameInputField != null)
        {
            playerNameInputField.text = string.Empty;
            playerNameInputField.Select();
            playerNameInputField.ActivateInputField();
        }
    }

    public void OnClickConfirm()
    {
        if (!TrySaveInputName())
        {
            FocusInputField();
            return;
        }

        HideInputNamePopup();
        SetInputNamePulseActive(false);
        OnPlayerNameConfirmed?.Invoke();
    }

    public void OnClickCancel()
    {
        HideInputNamePopup();
    }

    private void RefreshInputNameButton()
    {
        GameObject targetGroup = ResolveInputNameGroup();

        if (book == null || targetGroup == null)
            return;

        bool shouldShowButton = visiblePageMin <= book.currentPage && book.currentPage <= visiblePageMax;
        targetGroup.SetActive(shouldShowButton);

        if (!shouldShowButton)
        {
            HideInputNamePopup();
            return;
        }

        targetGroup.transform.SetAsLastSibling();
    }

    private void HideInputNameButton()
    {
        GameObject targetGroup = ResolveInputNameGroup();

        if (targetGroup != null)
            targetGroup.SetActive(false);

        HideInputNamePopup();
    }

    private void HideInputNamePopup()
    {
        if (inputNamePopup != null)
            inputNamePopup.SetActive(false);

        if (dim != null)
            dim.SetActive(false);

        HideWarning();
    }

    private bool TrySaveInputName()
    {
        string inputName = playerNameInputField != null ? playerNameInputField.text : string.Empty;
        IReadOnlyList<string> candidateNames = SaveManager.Instance != null
            ? SaveManager.Instance.CurrentGameNames.npcNames
            : null;
        NameValidationResult validationResult = ValidateName(inputName, candidateNames);

        if (validationResult != NameValidationResult.Valid)
        {
            ShowWarning(GetWarningMessage(validationResult));
            Debug.LogWarning($"Invalid player name input: '{inputName}'");
            return false;
        }

        CurrentPlayerName = inputName.Trim();
        UpdatePlayerNameDisplay();
        HideWarning();
        return true;
    }

    private static NameValidationResult ValidateName(
        string inputName,
        IReadOnlyList<string> candidateNames)
    {
        if (string.IsNullOrWhiteSpace(inputName))
            return NameValidationResult.Empty;

        if (inputName.Length < MinNameLength || inputName.Length > MaxNameLength)
            return NameValidationResult.TooLong;

        string normalizedName = inputName.Trim();

        for (int i = 0; i < normalizedName.Length; i++)
        {
            if (IsHangulJamo(normalizedName[i]))
                return NameValidationResult.IncompleteHangul;
        }

        if (candidateNames != null)
        {
            for (int i = 0; i < candidateNames.Count; i++)
            {
                if (string.Equals(normalizedName, candidateNames[i], StringComparison.Ordinal))
                    return NameValidationResult.DuplicateCandidateName;
            }
        }

        return NameValidationResult.Valid;
    }

    private static bool IsHangulJamo(char character)
    {
        return (character >= '\u1100' && character <= '\u11FF') || // 한글 자모
               (character >= '\u3130' && character <= '\u318F') || // 호환용 한글 자모
               (character >= '\uA960' && character <= '\uA97F') || // 한글 자모 확장 A
               (character >= '\uD7B0' && character <= '\uD7FF') || // 한글 자모 확장 B
               (character >= '\uFFA0' && character <= '\uFFDC');   // 반각 한글 자모
    }

    private static string GetWarningMessage(NameValidationResult validationResult)
    {
        switch (validationResult)
        {
            case NameValidationResult.Empty:
                return EmptyNameWarningMessage;
            case NameValidationResult.TooLong:
                return TooLongNameWarningMessage;
            case NameValidationResult.DuplicateCandidateName:
                return DuplicateCandidateNameWarningMessage;
            case NameValidationResult.IncompleteHangul:
                return IncompleteHangulWarningMessage;
            default:
                return string.Empty;
        }
    }

    private void ConfigureInputField()
    {
        if (playerNameInputField == null)
            return;

        playerNameInputField.characterLimit = MaxNameLength;
    }

    private void FocusInputField()
    {
        if (playerNameInputField == null)
            return;

        playerNameInputField.Select();
        playerNameInputField.ActivateInputField();
    }

    private void ShowWarning(string message)
    {
        if (warningText == null)
            return;

        warningText.text = message;
        warningText.gameObject.SetActive(true);
    }

    private void HideWarning()
    {
        if (warningText == null)
            return;

        warningText.text = string.Empty;
        warningText.gameObject.SetActive(false);
    }

    private void UpdatePlayerNameDisplay()
    {
        if (playerNameDisplayText == null)
            return;

        playerNameDisplayText.text = CurrentPlayerName;
    }

    private GameObject ResolveInputNameGroup()
    {
        if (inputNameGroup != null)
            return inputNameGroup;

        return inputNameButton != null ? inputNameButton.gameObject : null;
    }

    private void ResolveBookReference()
    {
        if (book == null)
            book = FindFirstObjectByType<Book>();
    }

    private void ResolveInputFieldReference()
    {
        if (playerNameInputField == null && inputNamePopup != null)
            playerNameInputField = inputNamePopup.GetComponentInChildren<TMP_InputField>(true);
    }

    private void ResolveInputNamePulseEffect()
    {
        if (inputNamePulseEffect == null && inputNameButton != null)
            inputNamePulseEffect = inputNameButton.GetComponent<UIButtonPulseEffect>();
    }

    private void SetInputNamePulseActive(bool isActive)
    {
        if (inputNamePulseEffect != null)
            inputNamePulseEffect.enabled = isActive;
    }
}
