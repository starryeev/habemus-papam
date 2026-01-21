using System.Collections.Generic;
using UnityEngine;

public class ChatTrigger : MonoBehaviour
{
    // 범위 안에 들어온 NPC들을 담아둘 리스트
    public List<StateController> collectedNPCs = new List<StateController>();

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("NPC"))
        {
            // 부모(자신을 생성한 ChatMaster)는 제외
            if (transform.parent != null && other.gameObject == transform.parent.gameObject)
                return;

            StateController controller = other.GetComponent<StateController>();
            if (controller != null && !collectedNPCs.Contains(controller))
            {
                collectedNPCs.Add(controller);
            }
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        // 범위 밖으로 나가면 후보군에서 제거
        if (other.CompareTag("NPC"))
        {
            StateController controller = other.GetComponent<StateController>();
            if (controller != null && collectedNPCs.Contains(controller))
            {
                collectedNPCs.Remove(controller);
            }
        }
    }
}