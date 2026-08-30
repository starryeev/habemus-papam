using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class InputNameUIController : MonoBehaviour
{
    private const int MinNameLength = 1;
    private const int MaxNameLength = 10;
    private const string EmptyNameWarningMessage = "이름을 입력해주세요.";
    private const string TooLongNameWarningMessage = "이름은 10글자 이하로 입력해주세요.";

    [SerializeField] private Book book;
    [SerializeField] private GameObject inputNamePopup;
    [SerializeField] private GameObject dim;

    [SerializeField] private GameObject inputNameGroup;
    [SerializeField] private Button inputNameButton;
    [SerializeField] private TextMeshProUGUI playerNameDisplayText;
    [SerializeField] private TMP_InputField playerNameInputField;

    [SerializeField] private TextMeshProUGUI warningText;

    [SerializeField] private Button confirmButton;
    [SerializeField] private Button cancelButton;

    private int visiblePageMin = 2;
    private int visiblePageMax = 3;

    public string CurrentPlayerName { get; private set; } = string.Empty;
    public bool HasPlayerName => !string.IsNullOrWhiteSpace(CurrentPlayerName);

    private enum NameValidationResult
    {
        Valid,
        Empty,
        TooLong,
    }

    private void Awake()
    {
        ResolveBookReference();
        ResolveInputFieldReference();
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
        NameValidationResult validationResult = ValidateName(inputName);

        if (validationResult != NameValidationResult.Valid)
        {
            ShowWarning(GetWarningMessage(validationResult));
            Debug.LogWarning($"Invalid player name input: '{inputName}'");
            return false;
        }

        CurrentPlayerName = inputName;
        UpdatePlayerNameDisplay();
        HideWarning();
        return true;
    }

    private static NameValidationResult ValidateName(string inputName)
    {
        if (string.IsNullOrEmpty(inputName))
            return NameValidationResult.Empty;

        if (inputName.Length < MinNameLength || inputName.Length > MaxNameLength)
            return NameValidationResult.TooLong;

        return NameValidationResult.Valid;
    }

    private static string GetWarningMessage(NameValidationResult validationResult)
    {
        switch (validationResult)
        {
            case NameValidationResult.Empty:
                return EmptyNameWarningMessage;
            case NameValidationResult.TooLong:
                return TooLongNameWarningMessage;
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
}
