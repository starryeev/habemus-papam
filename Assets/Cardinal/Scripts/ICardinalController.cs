using UnityEngine;

public struct CardinalInputData
{
    public Vector2? targetPos;
}

public interface ICardinalController
{
    CardinalInputData GetInput();
}
