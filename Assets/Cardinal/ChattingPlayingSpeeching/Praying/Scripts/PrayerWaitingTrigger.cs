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

            return;
        }
        if (other.CompareTag("Player"))
        {
 
            if (isNpcInside)
            {
               
                return;
            }

            StateController playerSC = other.GetComponent<StateController>();

            // B. 조건 확인
            if (playerSC != null && playerSC.CurrentState == CardinalState.Idle && gamsilManager != null)
            {
               
                if (incomingNPC != null)
                {

                    incomingNPC.ChangeState(CardinalState.Idle);

                    incomingNPC = null;
                }

                // 플레이어를 대기열에 등록
                gamsilManager.RegisterPlayerToQueue(playerSC);
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