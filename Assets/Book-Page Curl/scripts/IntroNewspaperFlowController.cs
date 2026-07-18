using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class IntroNewspaperFlowController : MonoBehaviour
{
    private const string GameSceneName = "GameScene";
    private const string NextButtonName = "btn_next";
    private const string PreviousButtonName = "btn_prev";
    private const string ConfirmButtonName = "ConfirmBtn";
    private const string CancelButtonName = "CancelBtn";
    private const string GameStartConfirmPopupName = "GameStartConfirmPopup";
    private const int NameInputPage = 2;
    private const int GameStartConfirmPage = 4;
    private const float GameSceneLoadDelay = 3f;

    [SerializeField] private Book book;
    [SerializeField] private AutoFlip autoFlip;
    [SerializeField] private InputNameUIController inputNameUIController;
    [SerializeField] private Button nextButton;
    [SerializeField] private Button previousButton;
    [SerializeField] private GameObject gameStartConfirmPopup;
    [SerializeField] private Button gameStartConfirmButton;
    [SerializeField] private Button gameStartCancelButton;

    private bool isWaitingForGameSceneLoad;

    private void Awake()
    {
        ResolveReferences();
        SetGameStartConfirmPopup(false);
    }

    private void OnEnable()
    {
        ResolveReferences();

        if (book != null)
        {
            book.OnFlipStarted += RefreshNavigationButtons;
            book.OnFlipSettled += RefreshNavigationButtons;
        }
    }

    private void Start()
    {
        RefreshNavigationButtons();
    }

    private void OnDisable()
    {
        if (book != null)
        {
            book.OnFlipStarted -= RefreshNavigationButtons;
            book.OnFlipSettled -= RefreshNavigationButtons;
        }
    }

    private void ResolveReferences()
    {
        if (autoFlip == null)
            autoFlip = GetComponent<AutoFlip>() ?? FindFirstObjectByType<AutoFlip>();

        if (book == null)
            book = autoFlip != null && autoFlip.ControledBook != null ? autoFlip.ControledBook : FindFirstObjectByType<Book>();

        if (inputNameUIController == null)
            inputNameUIController = FindFirstObjectByType<InputNameUIController>();

        if (nextButton == null)
            nextButton = FindSceneButton(NextButtonName);

        if (previousButton == null)
            previousButton = FindSceneButton(PreviousButtonName);

        if (gameStartConfirmPopup == null)
            gameStartConfirmPopup = FindSceneObject(GameStartConfirmPopupName);

        if (gameStartConfirmPopup != null)
        {
            if (gameStartConfirmButton == null)
                gameStartConfirmButton = FindChildButton(gameStartConfirmPopup.transform, ConfirmButtonName);

            if (gameStartCancelButton == null)
                gameStartCancelButton = FindChildButton(gameStartConfirmPopup.transform, CancelButtonName);
        }
    }

    public void OnClickNext()
    {
        if (isWaitingForGameSceneLoad || autoFlip == null || book == null || autoFlip.IsFlipping)
            return;

        if (book.currentPage == NameInputPage && (inputNameUIController == null || !inputNameUIController.HasPlayerName))
            return;

        if (book.currentPage == GameStartConfirmPage)
        {
            ShowGameStartConfirmPopup();
            return;
        }

        autoFlip.FlipRightPage();
    }

    public void OnClickPrevious()
    {
        if (isWaitingForGameSceneLoad || autoFlip == null || autoFlip.IsFlipping)
            return;

        autoFlip.FlipLeftPage();
    }

    private void ShowGameStartConfirmPopup()
    {
        if (gameStartConfirmPopup == null)
        {
            Debug.LogWarning("Game start confirm popup is not assigned.", this);
            return;
        }

        SetGameStartConfirmPopup(true);
    }

    public void OnClickConfirmGameStart()
    {
        if (isWaitingForGameSceneLoad || autoFlip == null)
            return;

        SetGameStartConfirmPopup(false);
        isWaitingForGameSceneLoad = true;
        RefreshNavigationButtons();

        autoFlip.FlipRightPage();
        StartCoroutine(LoadGameSceneAfterDelay());
    }

    public void OnClickCancelGameStart()
    {
        SetGameStartConfirmPopup(false);
    }

    private IEnumerator LoadGameSceneAfterDelay()
    {
        yield return new WaitForSeconds(GameSceneLoadDelay);

        string playerName = inputNameUIController != null ? inputNameUIController.CurrentPlayerName : string.Empty;
        if (SaveManager.Instance != null)
        {
            SaveManager.Instance.StartNewGame(playerName);
            yield break;
        }

        SceneManager.LoadScene(GameSceneName);
    }

    private void RefreshNavigationButtons()
    {
        if (book == null)
            return;

        if (isWaitingForGameSceneLoad)
        {
            SetButtonActive(previousButton, false);
            SetButtonActive(nextButton, false);
            return;
        }

        SetButtonActive(previousButton, book.currentPage > 0);
        SetButtonActive(nextButton, book.currentPage < book.TotalPageCount);
    }

    private void SetGameStartConfirmPopup(bool isActive)
    {
        if (gameStartConfirmPopup != null)
            gameStartConfirmPopup.SetActive(isActive);
    }

    private static void SetButtonActive(Button button, bool isActive)
    {
        if (button != null)
            button.gameObject.SetActive(isActive);
    }

    private static Button FindSceneButton(string objectName)
    {
        GameObject targetObject = FindSceneObject(objectName);
        return targetObject != null ? targetObject.GetComponent<Button>() : null;
    }

    private static GameObject FindSceneObject(string objectName)
    {
        if (string.IsNullOrWhiteSpace(objectName))
            return null;

        GameObject activeObject = GameObject.Find(objectName);
        if (activeObject != null)
            return activeObject;

        GameObject[] objects = Resources.FindObjectsOfTypeAll<GameObject>();
        foreach (GameObject candidate in objects)
        {
            if (candidate.name == objectName && candidate.scene.IsValid())
                return candidate;
        }

        return null;
    }

    private static Button FindChildButton(Transform parent, string childName)
    {
        if (parent == null || string.IsNullOrWhiteSpace(childName))
            return null;

        foreach (Transform child in parent)
        {
            if (child.name == childName)
                return child.GetComponent<Button>();

            Button result = FindChildButton(child, childName);
            if (result != null)
                return result;
        }

        return null;
    }
}
