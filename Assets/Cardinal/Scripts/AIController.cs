using UnityEngine;

public class AIController : MonoBehaviour, ICardinalController
{
    private Vector2? targetPos;

    // 테스트용 임시 로직(일정 시간마다 랜덤으로 돌아다니기)
    private float timer;

    public CardinalInputData GetInput()
    {
        timer += Time.deltaTime;
        if(timer >= 3.0f)
        {
            targetPos = (Vector2)transform.position + Random.insideUnitCircle * 3f;
            timer = 0f;
        }
        
        CardinalInputData inputData = new CardinalInputData { targetPos = this.targetPos };

        targetPos = null;

        return inputData;
    }
}
