using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;

public class PlayerController : MonoBehaviour, ICardinalController
{
    private const string PushSfxName = "14 인게임- NPC밀기";
    private const string ChatInterruptSfxName = "16 인게임- NPC대화방해";
    private const string SchemeSfxName = "17 인게임- NPC공작";

    [Header("Action Queue References")]
    [SerializeField] private Gamsil gamsilManager;
    [SerializeField] private Lecture lectureManager;

    private Vector2? targetPos;
    private StateController myStateController;

    private void Awake()
    {
        myStateController = GetComponent<StateController>();
    }

    private void Start()
    {
        if (gamsilManager == null)
        {
            gamsilManager = FindAnyObjectByType<Gamsil>();
        }

        if (lectureManager == null)
        {
            lectureManager = FindAnyObjectByType<Lecture>();
        }
    }

    private void Update()
    {
        var keyboard = Keyboard.current;
        var mouse = Mouse.current;
        if (keyboard == null || mouse == null || myStateController == null) return;

        bool canAcceptManualInteraction = myStateController.CanAcceptManualInteraction();
        bool canCancelActionMovement = myStateController.IsActionMovementInProgress;
        bool shouldCaptureMoveInput = canAcceptManualInteraction || canCancelActionMovement;
        bool isMovingInput = false;
        Key moveUpKey = GetConfiguredHotKey(HotKeyAction.MoveUp, Key.W);
        Key moveDownKey = GetConfiguredHotKey(HotKeyAction.MoveDown, Key.S);
        Key moveRightKey = GetConfiguredHotKey(HotKeyAction.MoveRight, Key.D);
        Key moveLeftKey = GetConfiguredHotKey(HotKeyAction.MoveLeft, Key.A);

        Vector2 mouseScreenPosition = mouse.position.ReadValue();
        bool isActionObjectClick = WorldActionButtonRaycastTarget.IsPointerOverAnyTarget(Camera.main, mouseScreenPosition);

        if (mouse.leftButton.wasPressedThisFrame && shouldCaptureMoveInput && !isActionObjectClick &&
            (EventSystem.current == null || !EventSystem.current.IsPointerOverGameObject()))
        {
            Vector3 world = Camera.main.ScreenToWorldPoint(
                new Vector3(mouseScreenPosition.x, mouseScreenPosition.y, -Camera.main.transform.position.z));
            targetPos = new Vector2(world.x, world.y);
            isMovingInput = true;
        }

        if (shouldCaptureMoveInput &&
            (IsKeyPressed(keyboard, moveUpKey) || IsKeyPressed(keyboard, moveDownKey) ||
             IsKeyPressed(keyboard, moveRightKey) || IsKeyPressed(keyboard, moveLeftKey) ||
             keyboard.upArrowKey.isPressed || keyboard.downArrowKey.isPressed ||
             keyboard.leftArrowKey.isPressed || keyboard.rightArrowKey.isPressed))
        {
            isMovingInput = true;
        }

        if (canCancelActionMovement && isMovingInput)
        {
            if (myStateController.IsHeadingToQueue)
            {
                gamsilManager?.CancelPlayerRegistration(myStateController);
            }
            else if (myStateController.IsHeadingToSpeech)
            {
                lectureManager?.CancelPlayerRegistration(myStateController);
            }

            return;
        }

        if (!canAcceptManualInteraction || myStateController.CurrentState != CardinalState.Idle)
        {
            return;
        }

    }

    public CardinalInputData GetInput()
    {
        CardinalInputData inputData = new CardinalInputData { targetPos = this.targetPos };
        targetPos = null;

        var keyboard = Keyboard.current;
        if (keyboard != null)
        {
            Vector2 moveDir = Vector2.zero;
            Key moveUpKey = GetConfiguredHotKey(HotKeyAction.MoveUp, Key.W);
            Key moveDownKey = GetConfiguredHotKey(HotKeyAction.MoveDown, Key.S);
            Key moveRightKey = GetConfiguredHotKey(HotKeyAction.MoveRight, Key.D);
            Key moveLeftKey = GetConfiguredHotKey(HotKeyAction.MoveLeft, Key.A);

            if (IsKeyPressed(keyboard, moveUpKey) || keyboard.upArrowKey.isPressed) moveDir.y += 1;
            if (IsKeyPressed(keyboard, moveDownKey) || keyboard.downArrowKey.isPressed) moveDir.y -= 1;
            if (IsKeyPressed(keyboard, moveLeftKey) || keyboard.leftArrowKey.isPressed) moveDir.x -= 1;
            if (IsKeyPressed(keyboard, moveRightKey) || keyboard.rightArrowKey.isPressed) moveDir.x += 1;

            inputData.moveDirection = moveDir.normalized;
        }
        return inputData;
    }

    private static Key GetConfiguredHotKey(HotKeyAction action, Key fallbackKey)
    {
        if (SettingsManager.Instance == null)
        {
            return fallbackKey;
        }

        return SettingsManager.Instance.GetHotKey(action);
    }

    private static bool IsKeyPressed(Keyboard keyboard, Key key)
    {
        return key != Key.None && keyboard[key].isPressed;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("NPC"))
        {
            return;
        }

        StateController npcState = other.GetComponent<StateController>();
        if (npcState == null)
        {
            return;
        }

        string sfxName = npcState.CurrentState == CardinalState.ChatMaster ||
            npcState.CurrentState == CardinalState.Chatting
            ? ChatInterruptSfxName
            : npcState.CurrentState == CardinalState.Scheme || npcState.IsSchemer
                ? SchemeSfxName
                : PushSfxName;

        SoundManager.Instance.PlaySFX(sfxName);
    }

}
