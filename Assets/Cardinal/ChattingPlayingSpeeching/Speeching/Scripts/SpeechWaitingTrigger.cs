using System.Collections.Generic;
using UnityEngine;

public class SpeechWaitingTrigger : MonoBehaviour
{
    [Tooltip("플레이어를 등록할 Lecture 매니저")]
    [SerializeField] private Lecture lectureManager;
    private readonly HashSet<StateController> occupants = new HashSet<StateController>();
    private StateController incomingNPC;

    public void SetIncomingNPC(StateController npc)
    {
        incomingNPC = npc;
    }

    public bool TryReserveSpotForPlayer()
    {
        if (occupants.Count > 0)
        {
            return false;
        }

        if (incomingNPC != null)
        {
            incomingNPC.CancelApproach();
            incomingNPC = null;
        }

        return true;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        StateController enteredController = other.GetComponent<StateController>();
        if (enteredController != null)
        {
            occupants.Add(enteredController);
        }

        if (other.CompareTag("NPC"))
        {
            StateController arrivedNPC = other.GetComponent<StateController>();
            if (incomingNPC == arrivedNPC)
            {
                incomingNPC = null;
            }
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        StateController exitedController = other.GetComponent<StateController>();
        if (exitedController != null)
        {
            occupants.Remove(exitedController);
        }

        if (other.CompareTag("NPC"))
        {
            StateController exitingNPC = other.GetComponent<StateController>();
            if (incomingNPC == exitingNPC)
            {
                incomingNPC = null;
            }
        }
    }
}
