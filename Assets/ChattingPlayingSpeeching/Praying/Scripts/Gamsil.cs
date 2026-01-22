using System.Collections.Generic;
using UnityEngine;

public class Gamsil : MonoBehaviour
{
    [Header("설정")]
    [Tooltip("기도하러 이동할 목표 지점 (Transform)")]
    [SerializeField] private Transform prayTargetPoint;

    [Tooltip("기도 호출 쿨타임 (초) - 자리가 비더라도 최소 이 시간은 기다림")]
    [SerializeField] private float callCooldown = 5.0f;

    [Tooltip("개별 NPC 재호출 대기 시간 (초)")]
    [SerializeField] private float individualCooldownDuration = 30.0f;

    // 감지된 NPC 리스트
    private List<StateController> candidates = new List<StateController>();

    // 각 NPC별 마지막 호출 시간을 저장하는 딕셔너리
    private Dictionary<StateController, float> npcLastCalledTime = new Dictionary<StateController, float>();

    // 현재 기도를 수행 중인(또는 이동 중인) NPC
    private StateController currentPrayerNPC = null;

    // 타이머
    private float currentTimer = 0f;

    void Update()
    {
        // 1. [중요] 현재 자리가 차있는지 확인
        if (IsPrayerSpotOccupied())
        {
            // 자리가 차있으면 쿨타임 타이머도 멈추거나, 혹은 그냥 리턴해서 호출 안함
            return;
        }

        // 2. 쿨타임 체크
        if (currentTimer > 0)
        {
            currentTimer -= Time.deltaTime;
            return;
        }

        // 3. 호출
        CallNearestNPC();
    }

    // 자리가 차있는지 판단하는 함수
    private bool IsPrayerSpotOccupied()
    {
        // 1. 할당된 NPC가 없으면 빈 자리
        if (currentPrayerNPC == null) return false;

        // 2. 할당된 NPC가 도중에 사라졌거나(Destroy) 죽었으면 빈 자리
        if (currentPrayerNPC == null || currentPrayerNPC.gameObject == null)
        {
            currentPrayerNPC = null;
            return false;
        }

        // 3. 할당된 NPC의 상태 확인
        // ReadyPraying(가는 중)이거나 Praying(기도 중)이면 자리가 찬 것으로 간주
        if (currentPrayerNPC.CurrentState == CardinalState.ReadyPraying ||
            currentPrayerNPC.CurrentState == CardinalState.Praying)
        {
            return true;
        }

        // 그 외의 상태(Idle로 돌아옴, 납치당해서 Chatting이 됨 등)라면 자리가 빈 것으로 간주
        currentPrayerNPC = null;
        return false;
    }

    private void CallNearestNPC()
    {
        if (candidates.Count == 0 || prayTargetPoint == null) return;

        StateController nearestNPC = null;
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

            // [조건] Idle 상태여야 함
            if (sc.CurrentState == CardinalState.Idle)
            {
                float dist = Vector3.Distance(transform.position, sc.transform.position);
                if (dist < minDistance)
                {
                    minDistance = dist;
                    nearestNPC = sc;
                }
            }
        }

        if (nearestNPC != null)
        {
            // [중요] 현재 기도 수행자로 등록
            currentPrayerNPC = nearestNPC;

            nearestNPC.OrderToPray(prayTargetPoint.position);

            if (npcLastCalledTime.ContainsKey(nearestNPC)) npcLastCalledTime[nearestNPC] = Time.time;
            else npcLastCalledTime.Add(nearestNPC, Time.time);

            currentTimer = callCooldown;
            Debug.Log($"Gamsil called {nearestNPC.name} to pray.");
        }
    }

    // --- Trigger 감지 로직 (기존 동일) ---
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