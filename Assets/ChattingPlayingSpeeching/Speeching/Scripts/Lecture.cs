using System.Collections.Generic;
using UnityEngine;

public class Lecture : MonoBehaviour
{
    [Header("위치 설정")]
    [Tooltip("실제 연설을 수행할 위치")]
    [SerializeField] private Transform speechTargetPoint;

    [Tooltip("연설 순서를 기다릴 대기 장소")]
    [SerializeField] private Transform waitingPoint;

    [Tooltip("WaitingPoint 오브젝트에 붙어있는 SpeechWaitingTrigger 컴포넌트")]
    [SerializeField] private SpeechWaitingTrigger waitingTrigger;

    [Header("시간 설정")]
    [SerializeField] private float callInterval = 3.0f;
    [SerializeField] private float individualCooldownDuration = 30.0f;

    private List<StateController> candidates = new List<StateController>();
    private Dictionary<StateController, float> npcLastCalledTime = new Dictionary<StateController, float>();

    // [변경] 연설 큐
    private Queue<StateController> speechQueue = new Queue<StateController>();
    private StateController currentSpeaker = null;
    private float timer = 0f;

    void Update()
    {
        ProcessQueue();

        if (speechQueue.Count == 0)
        {
            timer += Time.deltaTime;
            if (timer >= callInterval)
            {
                CallNewNPCToQueue();
                timer = 0f;
            }
        }
        else
        {
            timer = 0f;
        }
    }

    public void RegisterPlayerToQueue(StateController playerSC)
    {
        if (speechQueue.Contains(playerSC) || currentSpeaker == playerSC) return;
        if (playerSC.CurrentState != CardinalState.Idle) return;

        Debug.Log("Player entered Speech Waiting Zone!");
        speechQueue.Enqueue(playerSC);

        playerSC.OrderToSpeech(waitingPoint.position, true);
    }

    private void CallNewNPCToQueue()
    {
        if (candidates.Count == 0 || waitingPoint == null) return;

        StateController bestCandidate = null;
        float minDistance = float.MaxValue;

        for (int i = candidates.Count - 1; i >= 0; i--)
        {
            StateController sc = candidates[i];

            if (sc == null || sc.CompareTag("Player"))
            {
                candidates.RemoveAt(i);
                if (sc != null && npcLastCalledTime.ContainsKey(sc)) npcLastCalledTime.Remove(sc);
                continue;
            }

            if (npcLastCalledTime.ContainsKey(sc))
            {
                if (Time.time - npcLastCalledTime[sc] < individualCooldownDuration) continue;
            }

            if (speechQueue.Contains(sc) || sc == currentSpeaker) continue;

            if (sc.CurrentState == CardinalState.Idle)
            {
                float dist = Vector3.Distance(transform.position, sc.transform.position);
                if (dist < minDistance)
                {
                    minDistance = dist;
                    bestCandidate = sc;
                }
            }
        }

        if (bestCandidate != null)
        {
            speechQueue.Enqueue(bestCandidate);

            if (npcLastCalledTime.ContainsKey(bestCandidate)) npcLastCalledTime[bestCandidate] = Time.time;
            else npcLastCalledTime.Add(bestCandidate, Time.time);

            bestCandidate.OrderToSpeech(waitingPoint.position, true);

            if (waitingTrigger != null)
            {
                waitingTrigger.SetIncomingNPC(bestCandidate);
            }

            Debug.Log($"Lecture added NPC {bestCandidate.name} to queue.");
        }
    }

    private void ProcessQueue()
    {
        if (IsSpeechSpotOccupied()) return;

        if (speechQueue.Count > 0)
        {
            StateController nextCandidate = speechQueue.Dequeue();

            if (nextCandidate != null && nextCandidate.CurrentState == CardinalState.ReadyInSpeech)
            {
                currentSpeaker = nextCandidate;

                nextCandidate.ProceedToRealSpeech(speechTargetPoint.position);
            }
        }
    }

    private bool IsSpeechSpotOccupied()
    {
        if (currentSpeaker == null) return false;

        if (currentSpeaker.CurrentState == CardinalState.ReadyInSpeech ||
            currentSpeaker.CurrentState == CardinalState.InSpeech)
        {
            return true;
        }

        currentSpeaker = null;
        return false;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("NPC"))
        {
            StateController sc = other.GetComponent<StateController>();
            if (sc != null && !candidates.Contains(sc)) candidates.Add(sc);
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("NPC"))
        {
            StateController sc = other.GetComponent<StateController>();
            if (sc != null && candidates.Contains(sc)) candidates.Remove(sc);
        }
    }
}