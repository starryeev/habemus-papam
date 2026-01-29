using System.Collections.Generic;
using UnityEngine;

public class Lecture : MonoBehaviour
{
    [Header("위치 설정")]
    [Tooltip("실제 연설을 수행할 위치")]
    [SerializeField] private Transform speechTargetPoint;

    [Tooltip("연설 순서를 기다릴 대기 장소")]
    [SerializeField] private Transform waitingPoint;

    [Tooltip("자리가 꽉 찼을 때 플레이어가 대기할 3번째 장소")]
    [SerializeField] private Transform playerOverflowPoint;

    [Tooltip("WaitingPoint 오브젝트에 붙어있는 SpeechWaitingTrigger 컴포넌트")]
    [SerializeField] private SpeechWaitingTrigger waitingTrigger;

    [Header("시간 설정")]
    [SerializeField] private float callInterval = 3.0f;
    [SerializeField] private float individualCooldownDuration = 30.0f;

    private List<StateController> candidates = new List<StateController>();
    private Dictionary<StateController, float> npcLastCalledTime = new Dictionary<StateController, float>();

    private List<StateController> speechList = new List<StateController>();

    private StateController currentSpeaker = null;
    private StateController overflowPlayer = null; // 3번 자리 대기자
    private float timer = 0f;

    void Update()
    {
        ProcessQueue();

        if (speechList.Count == 0 && overflowPlayer == null)
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

    public void CancelPlayerRegistration(StateController playerSC)
    {
        bool removed = false;

        if (overflowPlayer == playerSC)
        {
            overflowPlayer = null;
            removed = true;
            Debug.Log("연설 3번째 대기석 예약 취소됨.");
        }
        else if (speechList.Contains(playerSC))
        {
            speechList.Remove(playerSC);
            removed = true;
            Debug.Log("연설 대기열 예약 취소됨.");

            if (waitingTrigger != null) waitingTrigger.SetIncomingNPC(null);
        }

        if (removed)
        {
            playerSC.CancelApproach();
        }
    }

    public void RegisterPlayerToQueue(StateController playerSC)
    {
        if (speechList.Contains(playerSC) || currentSpeaker == playerSC || overflowPlayer == playerSC) return;
        if (playerSC.CurrentState != CardinalState.Idle) return;

        bool isMainSpotAvailable = false;
        if (waitingTrigger != null)
        {
            isMainSpotAvailable = waitingTrigger.TryReserveSpotForPlayer();
        }

        if (isMainSpotAvailable)
        {
            Debug.Log("Player entered Speech Waiting Zone! Added to Queue.");
            speechList.Add(playerSC);
            playerSC.OrderToSpeech(waitingPoint.position, true);
        }
        else
        {
            if (playerOverflowPoint != null)
            {
                Debug.Log("연설 대기석이 꽉 차서 플레이어 전용 대기석(3순위)으로 이동합니다.");
                overflowPlayer = playerSC;
                playerSC.OrderToSpeech(playerOverflowPoint.position, true);
            }
        }
    }
    private void CallNewNPCToQueue()
    {
        if (overflowPlayer != null) return;
        if (candidates.Count == 0 || waitingPoint == null) return;

        StateController bestCandidate = null;
        float minDistance = float.MaxValue;

        for (int i = candidates.Count - 1; i >= 0; i--)
        {
            StateController sc = candidates[i];
            if (sc.CurrentState == CardinalState.Scheme || sc.IsSchemer) continue;

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

            if (speechList.Contains(sc) || sc == currentSpeaker) continue;

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
            speechList.Add(bestCandidate);

            if (npcLastCalledTime.ContainsKey(bestCandidate)) npcLastCalledTime[bestCandidate] = Time.time;
            else npcLastCalledTime.Add(bestCandidate, Time.time);

            bestCandidate.OrderToSpeech(waitingPoint.position, true);

            if (waitingTrigger != null)
            {
                waitingTrigger.SetIncomingNPC(bestCandidate);
            }
        }
    }

    private void ProcessQueue()
    {
        bool isSpot1Occupied = IsSpeechSpotOccupied();

        if (!isSpot1Occupied && speechList.Count > 0)
        {
            StateController nextCandidate = speechList[0];
            speechList.RemoveAt(0);

            if (nextCandidate != null)
            {
                currentSpeaker = nextCandidate;
                nextCandidate.ProceedToRealSpeech(speechTargetPoint.position);

                if (overflowPlayer != null)
                {
                    MoveOverflowPlayerToWaitingSpot();
                }
            }
            return;
        }

        if (speechList.Count == 0 && overflowPlayer != null)
        {
            MoveOverflowPlayerToWaitingSpot();
        }
    }

    private void MoveOverflowPlayerToWaitingSpot()
    {
        if (overflowPlayer == null) return;

        Debug.Log("연설 대기석이 비어 플레이어가 3순위 -> 2순위로 이동합니다.");

        speechList.Add(overflowPlayer);

        if (waitingTrigger != null)
        {
            waitingTrigger.SetIncomingNPC(overflowPlayer);
        }

        overflowPlayer.OrderToSpeech(waitingPoint.position, true);
        overflowPlayer = null;
    }

    private bool IsSpeechSpotOccupied()
    {
        if (currentSpeaker == null) return false;

        if (currentSpeaker.IsPerformingSpeechAction)
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