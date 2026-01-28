using System.Collections.Generic;
using UnityEngine;

public class Gamsil : MonoBehaviour
{
    [Header("위치 설정")]
    [Tooltip("실제 기도를 수행할 위치")]
    [SerializeField] private Transform prayTargetPoint;

    [Tooltip("기도 순서를 기다릴 대기 장소 (줄 서는 곳)")]
    [SerializeField] private Transform waitingPoint;

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
    private Queue<StateController> prayerQueue = new Queue<StateController>();

    // 현재 기도를 수행 중인 대상
    private StateController currentPrayerNPC = null;

    // 호출 타이머
    private float timer = 0f;

    void Update()
    {
        ProcessQueue();

        if (prayerQueue.Count == 0)
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
        if (prayerQueue.Contains(playerSC) || currentPrayerNPC == playerSC) return;

        if (playerSC.CurrentState != CardinalState.Idle) return;

        prayerQueue.Enqueue(playerSC);

        playerSC.OrderToPray(waitingPoint.position, true);
    }

    private void CallNewNPCToQueue()
    {
        if (candidates.Count == 0 || waitingPoint == null) return;

        StateController bestCandidate = null;
        float minDistance = float.MaxValue;

        for (int i = candidates.Count - 1; i >= 0; i--)
        {
            StateController sc = candidates[i];

            if (sc.CurrentState == CardinalState.Scheme || sc.IsSchemer)
            {
                continue;
            }

            if (sc == null || sc.CompareTag("Player"))
            {
                candidates.RemoveAt(i);
                if (sc != null && npcLastCalledTime.ContainsKey(sc)) npcLastCalledTime.Remove(sc);
                continue;
            }

            // 쿨타임 체크
            if (npcLastCalledTime.ContainsKey(sc))
            {
                if (Time.time - npcLastCalledTime[sc] < individualCooldownDuration) continue;
            }

            // 중복 체크
            if (prayerQueue.Contains(sc) || sc == currentPrayerNPC) continue;

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
            // NPC도 큐에 추가
            prayerQueue.Enqueue(bestCandidate);

            if (npcLastCalledTime.ContainsKey(bestCandidate)) npcLastCalledTime[bestCandidate] = Time.time;
            else npcLastCalledTime.Add(bestCandidate, Time.time);

            // 대기소로 이동 명령
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
        if (IsPrayerSpotOccupied()) return;

        if (prayerQueue.Count > 0)
        {
            StateController nextCandidate = prayerQueue.Dequeue();

            if (nextCandidate != null && nextCandidate.CurrentState == CardinalState.ReadyPraying)
            {
                currentPrayerNPC = nextCandidate;

                nextCandidate.ProceedToRealPrayer(prayTargetPoint.position);

            }
        }
    }

    // 자리가 찼는지 확인
    private bool IsPrayerSpotOccupied()
    {
        if (currentPrayerNPC == null) return false;

        if (currentPrayerNPC.CurrentState == CardinalState.ReadyPraying ||
            currentPrayerNPC.CurrentState == CardinalState.Praying)
        {
            return true;
        }

        currentPrayerNPC = null;
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