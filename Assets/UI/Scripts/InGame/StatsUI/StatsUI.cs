using System;
using System.Collections;
using UnityEngine;
//상단 UI
//초상화와 능력치를 표시하는 Stats 블록의 위치와 세부 능력치를 결정
//Stats에서는 단순 표시만을 담당
public class StatsUI : MonoBehaviour
{
    [SerializeField] private Stats[] StatsList = new Stats[4];
    [SerializeField] private Closeup closeup;
    [SerializeField] private int top = 345;
    [SerializeField] private int playerLength = 250;
    [SerializeField] private int length = 180;
    [SerializeField] private int padding = 10;
    [SerializeField] private float moveTime = 0.3f;
    private float[] MaxStats = new float[4];
    void Start()
    {
        
    }
    void Update()
    {
        for (int i = 0; i < 4; i++)
        {
            SetStats(i);
            MoveStats(i);
        }
    }
    //스탯 가져오기
    void SetStats(int i)
    {
        float hp = CardinalManager.Instance.Cardinals[i].Hp;
        float inf = CardinalManager.Instance.Cardinals[i].Influence;
        float pie = CardinalManager.Instance.Cardinals[i].Piety;
        StatsList[i].SetHP(hp);
        StatsList[i].SetInfluence(inf);
        StatsList[i].SetPiety(pie);
        MaxStats[i] = Math.Max(inf, pie);
    }
    void MoveStats(int i)
    {
        //MaxStats에서 가장 큰 스탯을 감지
        int max = 0;
        float target = top; //움직일 Y좌표
        float maxstat = 0f;
        for(int ii = 0; ii < MaxStats.Length; ii++)
        {
            if(MaxStats[ii]>=maxstat) max = ii;
        }
        // top에서 시작하여 간격을 더함
        // 현재 MaxStats[0]을 처리 중이라면 MoveY = top + i*length + playerLength/2
        // ex : NPC 이후 플레이어 처리 차례라면 MoveStats(1)이므로 top + NPC 길이 + 플레이어 길이 절반
        // MaxStats[0] == -1f라면 플레이어가 이미 처리되어 있으므로 top + (i-1/2)*length + playerLength 
        // ex : NPC -> 플레이어 이후 NPC 처리 차례라면 MoveStats(2)이므로 top + 1.5 NPC 길이 + 플레이어 길이
        // 아니라면 top + (i+1/2) * length ex : 3번째 배치 차례라면 MoveStats(2)이고 top + NPC 5/2개
        if(MaxStats[0] == -1f)
        {
            target = top + (i - 0.5f) * length + playerLength;
        }
        else if (max == 0) //현재 Player 처리 중
        {
            target = top + i*length + playerLength/2;
        }
        else target = top + (i + 0.5f) * length;

        //target으로 부드럽게 이동
        StopCoroutine(LerpStats(StatsList[max], target, moveTime));
        StartCoroutine(LerpStats(StatsList[max], target, moveTime));

        //이동 완료된 Stats는 -1f로 처리
        MaxStats[max] = -1f;
    }
    public static IEnumerator LerpStats(Stats st, float target, float time)
    {
        float start = st.transform.position.y;
        Vector2 dest = new Vector2(st.transform.position.x, target);

        if(time <= 0f)
        {
            st.transform.position = dest;
            yield break;
        }

        float t = 0f;
        while (t < time)
        {
            t += Time.deltaTime;
            float u = Mathf.Clamp01(t/time);

            float smooth = Mathf.SmoothStep(0f, 1f, u);

            float y = Mathf.Lerp(start, target, smooth);
            st.transform.position = new Vector2(start, y);

            yield return null;
        }
        st.transform.position = new Vector2(st.transform.position.x, target);
    }
}