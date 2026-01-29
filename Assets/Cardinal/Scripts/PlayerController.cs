using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController : MonoBehaviour, ICardinalController
{
    [Header("Interaction Settings")]
    [Tooltip("기도 대기열에 등록할 키 설정")]
    [SerializeField] private Key interactKey = Key.F;

    [Tooltip("연설(Speech) 키 (기본 G)")]
    [SerializeField] private Key speechKey = Key.G; 

    [Tooltip("상호작용 쿨타임 (초)")]
    [SerializeField] private float interactCooldown = 5.0f; 

    [Tooltip("Gamsil 매니저 참조")]
    [SerializeField] private Gamsil gamsilManager;
    [SerializeField] private Lecture lectureManager;

    private float currentCooldownTimer = 0f; // 현재 남은 쿨타임
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
            if (gamsilManager == null) gamsilManager = FindAnyObjectByType<Gamsil>();
            if (lectureManager == null) lectureManager = FindAnyObjectByType<Lecture>();
        }
    }
    

    private void Update()
    {
        if (currentCooldownTimer > 0)
        {
            currentCooldownTimer -= Time.deltaTime;
        }

        var keyboard = Keyboard.current;
        var mouse = Mouse.current;
        if (keyboard == null || mouse == null) return;

        // =========================================================
        //  Idle 상태일 때만 입력 처리 (이동, 취소, 신청)
        // =========================================================
        if (myStateController != null && myStateController.CurrentState == CardinalState.Idle)
        {
            bool isMovingInput = false;

            if (mouse.leftButton.wasPressedThisFrame)
            {
                Vector2 screenPos = mouse.position.ReadValue();
                Vector3 world = Camera.main.ScreenToWorldPoint(new Vector3(screenPos.x, screenPos.y, -Camera.main.transform.position.z));
                targetPos = new Vector2(world.x, world.y);
                isMovingInput = true;
            }

            if (keyboard.wKey.isPressed || keyboard.sKey.isPressed || keyboard.aKey.isPressed || keyboard.dKey.isPressed ||
                keyboard.upArrowKey.isPressed || keyboard.downArrowKey.isPressed || keyboard.leftArrowKey.isPressed || keyboard.rightArrowKey.isPressed)
            {
                isMovingInput = true;
            }

            // 취소 로직 
            if (isMovingInput)
            {
                // 기도하러 가는 중이었다면 -> Gamsil 취소
                if (myStateController.IsHeadingToQueue)
                {
                    if (gamsilManager != null) gamsilManager.CancelPlayerRegistration(myStateController);
                    targetPos = null;
                }
                else if (myStateController.IsHeadingToSpeech)
                {
                    if (lectureManager != null) lectureManager.CancelPlayerRegistration(myStateController);
                    targetPos = null;
                }
            }

            // 기도
            if (keyboard[interactKey].wasPressedThisFrame)
            {
                if (!myStateController.IsHeadingToQueue)
                {
                    if (currentCooldownTimer <= 0)
                    {
                        RequestPrayerEntry();
                    }
                    else
                    {
                        Debug.Log($"쿨타임 중입니다. {currentCooldownTimer:F1}초 남음");
                    }
                }
            }
            // 연설
            if (keyboard[speechKey].wasPressedThisFrame)
            {
                if (!myStateController.IsHeadingToQueue && !myStateController.IsHeadingToSpeech)
                {
                    if (currentCooldownTimer <= 0)
                    {
                        if (lectureManager != null)
                        {
                            lectureManager.RegisterPlayerToQueue(myStateController);
                            currentCooldownTimer = interactCooldown;
                        }
                    }
                }
            }
        }
    }

    private void RequestPrayerEntry()
    {
        if (gamsilManager == null || myStateController == null) return;

        if (myStateController.CurrentState == CardinalState.Idle)
        {
            gamsilManager.RegisterPlayerToQueue(myStateController);
            currentCooldownTimer = interactCooldown; // 쿨타임 시작
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
            if (keyboard.wKey.isPressed || keyboard.upArrowKey.isPressed) moveDir.y += 1;
            if (keyboard.sKey.isPressed || keyboard.downArrowKey.isPressed) moveDir.y -= 1;
            if (keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed) moveDir.x -= 1;
            if (keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed) moveDir.x += 1;

            inputData.moveDirection = moveDir.normalized;
        }
        return inputData;
    }
}