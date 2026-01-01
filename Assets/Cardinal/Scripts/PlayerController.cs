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
            Vector3 world = Camera.main.ScreenToWorldPoint(screenPos);
            targetPos = (Vector2)world;
        }
    }

    public CardinalInputData GetInput()
    {
        CardinalInputData inputData = new CardinalInputData { targetPos = this.targetPos };

        targetPos = null;

        return inputData;
    }
}
