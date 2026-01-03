using UnityEngine;

public struct CardinalInputData
{
    public Vector2? targetPos;

    public Vector2 moveDirection;
}

public interface ICardinalController
{
    CardinalInputData GetInput();
}
