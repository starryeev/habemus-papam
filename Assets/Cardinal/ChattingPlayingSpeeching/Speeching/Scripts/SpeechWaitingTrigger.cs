using UnityEngine;

public class SpeechWaitingTrigger : MonoBehaviour
{
    [Tooltip("플레이어를 등록할 Lecture 매니저")]
    [SerializeField] private Lecture lectureManager;

    private bool isNpcInside = false;
    private StateController incomingNPC;

    public void SetIncomingNPC(StateController npc)
    {
        incomingNPC = npc;
    }

    // 자리가 비었는지, 뺏을 수 있는지 확인하는 함수
    public bool TryReserveSpotForPlayer()
    {
        if (isNpcInside)
        {
            Debug.Log("연설 대기석에 누군가 서 있습니다.");
            return false;
        }

        if (incomingNPC != null)
        {
            // 오던 NPC 취소시키고 플레이어 우선
            incomingNPC.ChangeState(CardinalState.Idle);
            incomingNPC = null;
            Debug.Log("연설하러 오던 NPC를 돌려보내고 플레이어가 자리를 차지합니다.");
        }

        return true;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("NPC"))
        {
            isNpcInside = true;
            StateController arrivedNPC = other.GetComponent<StateController>();
            if (incomingNPC == arrivedNPC)
            {
                incomingNPC = null;
            }
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("NPC"))
        {
            isNpcInside = false;
            StateController exitingNPC = other.GetComponent<StateController>();
            if (incomingNPC == exitingNPC)
            {
                incomingNPC = null;
            }
        }
    }
}