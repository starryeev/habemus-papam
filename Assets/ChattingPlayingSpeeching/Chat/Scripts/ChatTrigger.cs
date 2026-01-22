using System.Collections.Generic;
using UnityEngine;

public class ChatTrigger : MonoBehaviour
{
    public List<StateController> collectedNPCs = new List<StateController>();

    public bool IsPlayerInFormation { get; private set; } = false;
    private bool isFormationActive = false;

    // 컴포넌트들
    private Collider2D scanCollider;       // 초기 스캔용 (원형 등)
    private PolygonCollider2D polyCollider; // 3명 이상 (다각형)
    private EdgeCollider2D lineCollider;    // 2명 (직선)

    void Awake()
    {
        scanCollider = GetComponent<Collider2D>();

        // 1. 다각형 콜라이더 추가 (3명 이상용)
        polyCollider = gameObject.AddComponent<PolygonCollider2D>();
        polyCollider.isTrigger = true;
        polyCollider.enabled = false;

        // 2. 선형 콜라이더 추가 (2명용)
        lineCollider = gameObject.AddComponent<EdgeCollider2D>();
        lineCollider.isTrigger = true;
        lineCollider.enabled = false;
        // 선이 너무 얇으면 감지가 안 될 수 있으므로 두께를 약간 줍니다 (필요 시 조절)
        lineCollider.edgeRadius = 0.1f;
    }

    public void CreateFormationCollider(List<Transform> participants)
    {
        if (participants == null || participants.Count < 2) return;

        // 기존 스캔 콜라이더 끄기
        if (scanCollider != null) scanCollider.enabled = false;

        // 로컬 좌표 변환
        List<Vector2> localPoints = new List<Vector2>();
        foreach (var p in participants)
        {
            localPoints.Add(transform.InverseTransformPoint(p.position));
        }

        // --- 분기 처리 ---
        if (participants.Count == 2)
        {
            // [2명] -> EdgeCollider2D (직선) 사용
            polyCollider.enabled = false; // 다각형 끄기

            lineCollider.SetPoints(localPoints); // 두 점을 잇는 선 설정
            lineCollider.enabled = true;  // 선 켜기
        }
        else
        {
            // [3명 이상] -> PolygonCollider2D (다각형) 사용
            lineCollider.enabled = false; // 선 끄기

            polyCollider.points = localPoints.ToArray();
            polyCollider.enabled = true;  // 다각형 켜기
        }

        isFormationActive = true;
    }

    // --- 충돌 감지 로직 (동일) ---
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!isFormationActive)
        {
            // 초기 수집 단계
            if (other.CompareTag("NPC") || other.CompareTag("Player"))
            {
                if (transform.parent != null && other.gameObject == transform.parent.gameObject) return;

                StateController sc = other.GetComponent<StateController>();
                if (sc != null && !collectedNPCs.Contains(sc)) collectedNPCs.Add(sc);
            }
        }
        else
        {
            // 포메이션 형성 후 감지
            if (other.CompareTag("Player"))
            {
                IsPlayerInFormation = true;
            }
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (!isFormationActive)
        {
            if (other.CompareTag("NPC") || other.CompareTag("Player"))
            {
                StateController sc = other.GetComponent<StateController>();
                if (sc != null && collectedNPCs.Contains(sc)) collectedNPCs.Remove(sc);
            }
        }
        else
        {
            if (other.CompareTag("Player"))
            {
                IsPlayerInFormation = false;
            }
        }
    }
}