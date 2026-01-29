using UnityEngine;

public class PrayerWaitingTrigger : MonoBehaviour
{
    [Tooltip("플레이어를 등록할 Gamsil 매니저")]
    [SerializeField] private Gamsil gamsilManager;

    private bool isNpcInside = false;

    private StateController incomingNPC;

    public void SetIncomingNPC(StateController npc)
    {
        incomingNPC = npc;
    }

    public bool TryReserveSpotForPlayer()
    {
        if (isNpcInside)
        {
            Debug.Log("누군가 서 있어서 기도를 신청할 수 없습니다.");
            return false;
        }

        if (incomingNPC != null)
        {
            incomingNPC.ChangeState(CardinalState.Idle);
            incomingNPC = null;
            Debug.Log("오고 있던 NPC의 예약을 취소하고 플레이어가 자리를 차지합니다.");
        }

        return true; // 입장 가능
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