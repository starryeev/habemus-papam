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

            if (playerSC != null && playerSC.CurrentState == CardinalState.Idle && lectureManager != null)
            {
                if (incomingNPC != null)
                {
                    incomingNPC.ChangeState(CardinalState.Idle);
                    incomingNPC = null;
                }

                lectureManager.RegisterPlayerToQueue(playerSC);
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