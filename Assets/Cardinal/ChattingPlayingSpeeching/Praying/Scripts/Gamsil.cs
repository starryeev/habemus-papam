using System.Collections.Generic;
using UnityEngine;

public class Gamsil : MonoBehaviour
{
    [Header("위치 설정")]
    [Tooltip("실제 기도를 수행할 위치")]
    [SerializeField] private Transform prayTargetPoint;

    [Tooltip("기도 순서를 기다릴 대기 장소 (줄 서는 곳)")]
    [SerializeField] private Transform waitingPoint;

    [Tooltip("자리가 꽉 찼을 때 플레이어가 대기할 3번째 장소")]
    [SerializeField] private Transform playerOverflowPoint;

    [Tooltip("WaitingPoint 오브젝트에 붙어있는 PrayerWaitingTrigger 컴포넌트")]
    [SerializeField] private PrayerWaitingTrigger waitingTrigger;

    [Header("시간 설정")]
    [Tooltip("대기열이 비었을 때 다음 NPC를 호출하기까지 걸리는 시간 (초)")]
    [SerializeField] private float callInterval = 3.0f;

    [Tooltip("개별 NPC 재호출 대기 시간 (초)")]
    [SerializeField] private float individualCooldownDuration = 30.0f;

    // 감지된 NPC 리스트 
    private List<StateController> candidates = new List<StateController>();

    // 개별 쿨타임 관리
    private Dictionary<StateController, float> npcLastCalledTime = new Dictionary<StateController, float>();

    // 대기열 큐 
    private List<StateController> prayerList = new List<StateController>();
    // 현재 기도를 수행 중인 대상
    private StateController currentPrayerNPC = null;
    
    // 3번째 자리에 대기 중인 플레이어 저장용
    private StateController overflowPlayer = null;

    // 호출 타이머
    private float timer = 0f;

    void Update()
    {
        CleanupQueue();
        ProcessQueue();

        if (prayerList.Count == 0 && overflowPlayer == null)
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
        }
        else if (prayerList.Contains(playerSC))
        {
            prayerList.Remove(playerSC);
            removed = true;

            if (waitingTrigger != null) waitingTrigger.SetIncomingNPC(null);
        }

        if (removed)
        {
            playerSC.CancelApproach();
        }
    }

    public void RegisterPlayerToQueue(StateController playerSC)
    {
        if (prayerList.Contains(playerSC) || currentPrayerNPC == playerSC || overflowPlayer == playerSC) return;
        if (playerSC == null || !playerSC.CanAcceptManualInteraction()) return;

        if (CanPlayerGoDirectlyToPrayer(playerSC))
        {
            currentPrayerNPC = playerSC;
            playerSC.OrderToPray(prayTargetPoint.position, false);
            return;
        }

        if (waitingPoint == null) return;

        bool isMainSpotAvailable = false;
        if (waitingTrigger != null)
        {
            isMainSpotAvailable = waitingTrigger.TryReserveSpotForPlayer();
        }

        if (isMainSpotAvailable)
        {
            prayerList.Add(playerSC);
            playerSC.OrderToPray(waitingPoint.position, true);
        }
        else
        {
            if (playerOverflowPoint != null)
            {
                overflowPlayer = playerSC;
                playerSC.OrderToPray(playerOverflowPoint.position, true);
            }
        }
    }

    public bool TryStartPlayerPrayerImmediately(StateController playerSC)
    {
        if (playerSC == null || !playerSC.CompareTag("Player") || prayTargetPoint == null)
        {
            return false;
        }

        CancelPlayerRegistration(playerSC);

        if (currentPrayerNPC == playerSC)
        {
            if (playerSC.IsPerformingPrayerAction)
            {
                return false;
            }

            currentPrayerNPC = null;
        }

        if (currentPrayerNPC != null)
        {
            currentPrayerNPC.ForceCompletePrayer();
        }

        currentPrayerNPC = playerSC;
        bool started = playerSC.TeleportToPrayerAndStart(prayTargetPoint.position);
        if (!started)
        {
            currentPrayerNPC = null;
        }

        return started;
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
            if (sc == null || sc.CompareTag("Player"))
            {
                candidates.RemoveAt(i);
                if (sc != null && npcLastCalledTime.ContainsKey(sc)) npcLastCalledTime.Remove(sc);
                continue;
            }

            if (sc.CurrentState == CardinalState.Scheme || sc.IsSchemer) continue;

            if (npcLastCalledTime.ContainsKey(sc))
            {
                if (Time.time - npcLastCalledTime[sc] < individualCooldownDuration) continue;
            }

            if (prayerList.Contains(sc) || sc == currentPrayerNPC) continue;

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
            prayerList.Add(bestCandidate); 

            if (npcLastCalledTime.ContainsKey(bestCandidate)) npcLastCalledTime[bestCandidate] = Time.time;
            else npcLastCalledTime.Add(bestCandidate, Time.time);

            bestCandidate.OrderToPray(waitingPoint.position, true);

            if (waitingTrigger != null)
            {
                waitingTrigger.SetIncomingNPC(bestCandidate);
            }
        }
    }

    // 대기열 처리
    private void ProcessQueue()
    {
        bool isSpot1Occupied = IsPrayerSpotOccupied();

        if (!isSpot1Occupied && prayerList.Count > 0)
        {
            StateController nextCandidate = prayerList[0];
            prayerList.RemoveAt(0);

            if (nextCandidate != null)
            {
                currentPrayerNPC = nextCandidate;
                nextCandidate.ProceedToRealPrayer(prayTargetPoint.position);

                if (overflowPlayer != null)
                {
                    MoveOverflowPlayerToWaitingSpot();
                }
            }
            return;
        }

        if (prayerList.Count == 0 && overflowPlayer != null)
        {
            MoveOverflowPlayerToWaitingSpot();
        }
    }

    private void MoveOverflowPlayerToWaitingSpot()
    {
        if (overflowPlayer == null) return;

        if (!prayerList.Contains(overflowPlayer))
        {
            prayerList.Add(overflowPlayer);
        }

        if (waitingTrigger != null)
        {
            waitingTrigger.SetIncomingNPC(overflowPlayer);
        }

        overflowPlayer.OrderToPray(waitingPoint.position, true);
        overflowPlayer = null;
    }


    private bool IsPrayerSpotOccupied()
    {
        if (currentPrayerNPC == null) return false;

        if (currentPrayerNPC.IsPerformingPrayerAction)
        {
            return true;
        }

        currentPrayerNPC = null;
        return false;
    }

    private void CleanupQueue()
    {
        prayerList.RemoveAll(sc => sc == null || !sc.IsPerformingPrayerAction);

        if (overflowPlayer != null && !overflowPlayer.IsPerformingPrayerAction)
        {
            overflowPlayer = null;
        }

        if (currentPrayerNPC != null && !currentPrayerNPC.IsPerformingPrayerAction)
        {
            currentPrayerNPC = null;
        }
    }

    private bool CanPlayerGoDirectlyToPrayer(StateController playerSC)
    {
        if (playerSC == null || !playerSC.CompareTag("Player"))
        {
            return false;
        }

        if (prayTargetPoint == null)
        {
            return false;
        }

        if (overflowPlayer != null || prayerList.Count > 0)
        {
            return false;
        }

        return !IsPrayerSpotOccupied();
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
