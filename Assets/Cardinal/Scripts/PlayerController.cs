using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController : MonoBehaviour, ICardinalController
{
    private Vector2? targetPos;

    // 테스트용 임시 마우스 조작 로직
    private void Update()
    {
        var mouse = Mouse.current;
        if (mouse == null) return;

        if (mouse.leftButton.wasPressedThisFrame)
        {
            Vector2 screenPos = mouse.position.ReadValue();

            float distance = -Camera.main.transform.position.z;
            Vector3 world = Camera.main.ScreenToWorldPoint(new Vector3(screenPos.x, screenPos.y, -Camera.main.transform.position.z));


            targetPos = new Vector2(world.x, world.y);
        }
    }

    public CardinalInputData GetInput()
    {
        CardinalInputData inputData = new CardinalInputData { targetPos = this.targetPos };

        targetPos = null;

        var keyboard = Keyboard.current;
        if(keyboard != null)
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
