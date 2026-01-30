using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static UnityEngine.GraphicsBuffer;
using UnityEngine.UIElements;

//상단 UI

//초상화와 능력치를 표시하는 Stats 블록의 위치와 세부 능력치를 결정

//Stats에서는 단순 표시만을 담당

public class StatsUI : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private Stats[] StatsList = new Stats[4];
    [SerializeField] private Closeup closeup;

    [Header("Layout Settings")]
    [SerializeField] private int top = 345;
    [SerializeField] private int playerLength = 250;
    [SerializeField] private int length = 180;
    [SerializeField] private float moveTime = 0.3f;

    private Cardinal[] linkedCardinals = new Cardinal[4];
    private float[] MaxStats = new float[4]; 
    private Coroutine[] moveCoroutines = new Coroutine[4];
    private bool isInitialized = false;

    // 기존 for 문에서 init 함수로 변경했습니다!
    public void Initialize(List<Cardinal> allCardinals)
    {
        if (allCardinals == null || allCardinals.Count == 0) return;

        Cardinal playerCardinal = null;
        foreach (var c in allCardinals)
        {
            if (c.CompareTag("Player"))
            {
                playerCardinal = c;
                break;
            }
        }

        //Player 스탯 할당
        linkedCardinals[0] = playerCardinal;

        //NPC 스탯 할당
        int uiSlotIndex = 1;
        for (int i = 0; i < allCardinals.Count; i++)
        {
            if (allCardinals[i] == playerCardinal) continue;

            if (uiSlotIndex < 4)
            {
                linkedCardinals[uiSlotIndex] = allCardinals[i];
                uiSlotIndex++;
            }
            else
            {
                break;
            }
        }

        isInitialized = true;
    }
    void Update()
    {
        if (!isInitialized) return;

        CalculateAndMoveStats();
    }

    void CalculateAndMoveStats()
    {
        for (int i = 0; i < 4; i++)
        {
            SetStats(i);
        }


        float[] tempMaxStats = (float[])MaxStats.Clone();

        bool isPlayerPlaced = false;

        for (int rank = 0; rank < 4; rank++)
        {
            int targetIndex = -1;
            float highestVal = -10000f; 

            for (int i = 0; i < 4; i++)
            {
                if (tempMaxStats[i] > highestVal)
                {
                    highestVal = tempMaxStats[i];
                    targetIndex = i;
                }
            }

            if (targetIndex != -1)
            {
                float targetY = 0f;

                // top에서 시작하여 간격을 더함
                // 현재 MaxStats[0]을 처리 중이라면 MoveY = top + i*length + playerLength/2
                // ex : NPC 이후 플레이어 처리 차례라면 MoveStats(1)이므로 top + NPC 길이 + 플레이어 길이 절반
                // MaxStats[0] == -1f라면 플레이어가 이미 처리되어 있으므로 top + (i-1/2)*length + playerLength 
                // ex : NPC -> 플레이어 이후 NPC 처리 차례라면 MoveStats(2)이므로 top + 1.5 NPC 길이 + 플레이어 길이
                // 아니라면 top + (i+1/2) * length ex : 3번째 배치 차례라면 MoveStats(2)이고 top + NPC 5/2개

                if (isPlayerPlaced)
                {
                    // Case 1: 위에 플레이어가 이미 배치됨
                    targetY = top - ((rank - 0.5f) * length + playerLength);
                }
                else if (targetIndex == 0)
                {
                    // Case 2: 지금 배치하는 게 플레이어임
                    targetY = top - (rank * length + playerLength / 2f);

                    isPlayerPlaced = true; 
                }
                else
                {
                    // Case 3: 플레이어는 아직 안 나왔고, 일반 NPC 배치
                    targetY = top - ((rank + 0.5f) * length);
                }

                MoveStat(targetIndex, targetY);
                tempMaxStats[targetIndex] = -99999f; 
            }
        }
    }

    //스탯 가져오기
    void SetStats(int i)
    {
        if (linkedCardinals[i] == null)
        {
            MaxStats[i] = -999f; 
            return;
        }

        float hp = linkedCardinals[i].Hp;
        float inf = linkedCardinals[i].Influence;
        float pie = linkedCardinals[i].Piety;

        StatsList[i].SetHP(hp);
        StatsList[i].SetInfluence(inf);
        StatsList[i].SetPiety(pie);

        MaxStats[i] = Math.Max(inf, pie);
    }


    
    
    void MoveStat(int uiIndex, float targetY)
    {
        //기존 MoveStat 에서 처리하던 로직  CalculateAndMoveStats() 옮겼습니다.
        //target으로 부드럽게 이동

        if (moveCoroutines[uiIndex] != null) StopCoroutine(moveCoroutines[uiIndex]);
        moveCoroutines[uiIndex] = StartCoroutine(LerpStats(StatsList[uiIndex], targetY, moveTime));
    }

    public IEnumerator LerpStats(Stats st, float target, float time)
    {
        float start = st.transform.localPosition.y; 
        float startX = st.transform.localPosition.x;

        if (time <= 0f)
        {
            st.transform.localPosition = new Vector3(startX, target, 0);
            yield break;
        }

        float t = 0f;
        while (t < time)
        {
            t += Time.deltaTime;
            float u = Mathf.Clamp01(t / time);
            float smooth = Mathf.SmoothStep(0f, 1f, u);

            float newY = Mathf.Lerp(start, target, smooth);
            st.transform.localPosition = new Vector3(startX, newY, 0);

            yield return null;
        }

        st.transform.localPosition = new Vector3(startX, target, 0);
    }
}