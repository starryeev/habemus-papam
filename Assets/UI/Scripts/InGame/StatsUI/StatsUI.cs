using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static UnityEngine.GraphicsBuffer;
using UnityEngine.UIElements;
using UnityEngine.UI;

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
    public Cardinal[] LinkedCardinals => linkedCardinals;
    private float[] MaxStats = new float[4]; 
    private float[] SubStats = new float[4];
    private Coroutine[] moveCoroutines = new Coroutine[4];
    private CanvasGroup canvasGroup;
    private Coroutine alphaCoroutine;
    private bool isInitialized = false;
    private int closeupIndex = -1;

    private void Awake()
    {
        HideForConclaveEntrance();
    }

    public int GetLinkedCardinalIndex(Cardinal candidate)
    {
        if (candidate == null)
        {
            return -1;
        }

        return Array.IndexOf(linkedCardinals, candidate);
    }

    public string GetDisplayName(int linkedIndex)
    {
        if (linkedIndex < 0 || linkedIndex >= linkedCardinals.Length)
        {
            return string.Empty;
        }

        GameNameSaveData names = SaveManager.Instance != null
            ? SaveManager.Instance.CurrentGameNames
            : null;

        if (names != null)
        {
            if (linkedIndex == 0 && !string.IsNullOrWhiteSpace(names.playerName))
            {
                return names.playerName;
            }

            int npcIndex = linkedIndex - 1;
            if (npcIndex >= 0 && names.npcNames != null &&
                npcIndex < names.npcNames.Count &&
                !string.IsNullOrWhiteSpace(names.npcNames[npcIndex]))
            {
                return names.npcNames[npcIndex];
            }
        }

        Cardinal cardinal = linkedCardinals[linkedIndex];
        return cardinal != null ? cardinal.name : string.Empty;
    }

    public string ResolveCandidateNames(string source, int randomCandidateNumber)
    {
        if (string.IsNullOrEmpty(source))
        {
            return source;
        }

        string resolved = source;
        string randomName = GetDisplayName(randomCandidateNumber);
        if (!string.IsNullOrEmpty(randomName))
        {
            resolved = resolved
                .Replace("(랜덤후보)", randomName)
                .Replace("(랜덤 후보)", randomName)
                .Replace("(랜덤) 후보", randomName)
                .Replace("(후보 n)", randomName)
                .Replace("(후보n)", randomName)
                .Replace("랜덤후보", randomName)
                .Replace("랜덤 후보", randomName)
                .Replace("후보 n", randomName)
                .Replace("후보n", randomName);
        }

        string candidate1 = GetDisplayName(1);
        string candidate2 = GetDisplayName(2);
        string candidate3 = GetDisplayName(3);
        if (!string.IsNullOrEmpty(candidate1) &&
            !string.IsNullOrEmpty(candidate2) &&
            !string.IsNullOrEmpty(candidate3))
        {
            resolved = resolved.Replace(
                "후보 1, 2, 3",
                $"{candidate1}, {candidate2}, {candidate3}");
        }

        for (int candidateNumber = 1; candidateNumber <= 3; candidateNumber++)
        {
            string displayName = GetDisplayName(candidateNumber);
            if (string.IsNullOrEmpty(displayName))
            {
                continue;
            }

            resolved = resolved
                .Replace($"(후보 {candidateNumber})", displayName)
                .Replace($"(후보{candidateNumber})", displayName)
                .Replace($"후보 {candidateNumber}", displayName)
                .Replace($"후보{candidateNumber}", displayName);
        }

#if UNITY_EDITOR
        if (!string.IsNullOrEmpty(randomName) &&
            !string.IsNullOrEmpty(candidate1) &&
            !string.IsNullOrEmpty(candidate2) &&
            !string.IsNullOrEmpty(candidate3))
        {
            Debug.Assert(
                !resolved.Contains("후보 n") &&
                !resolved.Contains("후보n") &&
                !resolved.Contains("랜덤후보") &&
                !resolved.Contains("랜덤 후보") &&
                !resolved.Contains("(후보 1)") &&
                !resolved.Contains("(후보 2)") &&
                !resolved.Contains("(후보 3)"),
                $"이벤트 후보 이름 치환에 실패했습니다: {resolved}");
        }
#endif

        return resolved;
    }

    public void HideForConclaveEntrance()
    {
        EnsureCanvasGroup();
        if (alphaCoroutine != null)
        {
            StopCoroutine(alphaCoroutine);
            alphaCoroutine = null;
        }

        canvasGroup.alpha = 0f;
        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;
    }

    public void FadeInAfterConclaveEntrance(float duration = 1f)
    {
        EnsureCanvasGroup();
        if (alphaCoroutine != null)
        {
            StopCoroutine(alphaCoroutine);
        }

        alphaCoroutine = StartCoroutine(FadeCanvasAlpha(1f, duration));
    }

    private void EnsureCanvasGroup()
    {
        if (canvasGroup == null)
        {
            canvasGroup = GetComponent<CanvasGroup>();
            if (canvasGroup == null)
            {
                canvasGroup = gameObject.AddComponent<CanvasGroup>();
            }
        }
    }

    private IEnumerator FadeCanvasAlpha(float targetAlpha, float duration)
    {
        float startAlpha = canvasGroup.alpha;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float progress = duration <= 0f ? 1f : Mathf.Clamp01(elapsed / duration);
            canvasGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, progress);
            yield return null;
        }

        canvasGroup.alpha = targetAlpha;
        canvasGroup.interactable = targetAlpha > 0f;
        canvasGroup.blocksRaycasts = targetAlpha > 0f;
        alphaCoroutine = null;
    }


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
                //Debug.Log($"[StatsUI] Slot[{uiSlotIndex}] 연결 완료: {linkedCardinals[uiSlotIndex].name}");
                uiSlotIndex++;
            }
            else
            {
                break;
            }
        }

        HideCloseup();
        ApplySavedNames();

        isInitialized = true;
    }

    public void ApplySavedNames()
    {
        if (StatsList == null)
        {
            return;
        }

        for (int i = 0; i < StatsList.Length; i++)
        {
            string displayName = GetDisplayName(i);
            if (StatsList[i] != null && !string.IsNullOrEmpty(displayName))
            {
                StatsList[i].SetName(displayName);
            }
        }
    }
    void Update()
    {
        if (!isInitialized) return;

        if (closeup.gameObject.activeSelf)
        {
            closeup.SetStats(linkedCardinals[closeupIndex].Hp,
            linkedCardinals[closeupIndex].Piety,
            linkedCardinals[closeupIndex].Influence,
            linkedCardinals[closeupIndex].MaxHp);
            closeup.SetGrayedOut(ShouldGrayOut(linkedCardinals[closeupIndex]));
        }
        else CalculateAndMoveStats();
    }

    public void RefreshVisualOrder()
    {
        if (isInitialized && (closeup == null || !closeup.gameObject.activeSelf))
        {
            CalculateAndMoveStats();
        }
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

            for (int i = 0; i < 4; i++)
            {
                if (tempMaxStats[i] <= -99999f)
                {
                    continue;
                }

                if (targetIndex == -1 || HasHigherVisualPriority(i, targetIndex, tempMaxStats))
                {
                    targetIndex = i;
                }
            }

            if (targetIndex != -1)
            {
                float targetY = 0f;

                // top에서 시작하여 간격을 더함
                // 현재 MaxStats[0]을 처리 중이라면 MoveY = top + i*length + playerLength/2
                // ex : NPC 이후 플레이어 처리 차례라면 MoveStats(1)이므로 top + NPC 길이 + 플레이어 길이 절반
                // MaxStats[0] == -99999f라면 플레이어가 이미 처리되어 있으므로 top + (i-1/2)*length + playerLength 
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

    private bool HasHigherVisualPriority(int candidateIndex, int currentIndex, float[] maxStats)
    {
        int candidateGroup = GetVisualOrderGroup(linkedCardinals[candidateIndex]);
        int currentGroup = GetVisualOrderGroup(linkedCardinals[currentIndex]);

        if (candidateGroup != currentGroup)
        {
            return candidateGroup < currentGroup;
        }

        // 탈락 후보는 항상 하단에서 UI 슬롯 순서대로 유지한다.
        if (candidateGroup == 1)
        {
            return candidateIndex < currentIndex;
        }

        if (maxStats[candidateIndex] != maxStats[currentIndex])
        {
            return maxStats[candidateIndex] > maxStats[currentIndex];
        }

        return SubStats[candidateIndex] > SubStats[currentIndex];
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

        StatsList[i].SetHP(hp, linkedCardinals[i].MaxHp);
        StatsList[i].SetInfluence(inf);
        StatsList[i].SetPiety(pie);
        StatsList[i].SetGrayedOut(ShouldGrayOut(linkedCardinals[i]));

        MaxStats[i] = Math.Max(inf, pie);
        SubStats[i] = Math.Min(inf, pie);
    }

    private static int GetVisualOrderGroup(Cardinal cardinal)
    {
        if (cardinal == null)
        {
            return 2;
        }

        return cardinal.IsKnockedOut || cardinal.Hp <= 0f ? 1 : 0;
    }

    private static bool ShouldGrayOut(Cardinal cardinal)
    {
        if (cardinal == null)
        {
            return false;
        }

        StateController stateController = cardinal.GetComponent<StateController>();
        return cardinal.IsKnockedOut || cardinal.Hp <= 0f ||
            (stateController != null && stateController.CurrentState == CardinalState.Stun);
    }

    void MoveStat(int uiIndex, float targetY)
    {
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
    public void ShowCloseup(int idx)
    {
        if (linkedCardinals[idx] != null)
        {
            //먼저 플레이어를 맨 위에 배치하고 나머지 UI 비활성화
            MoveStat(0, top - playerLength/2);
            StatsList[1].gameObject.SetActive(false);
            StatsList[2].gameObject.SetActive(false);
            StatsList[3].gameObject.SetActive(false);

            closeup.gameObject.SetActive(true);
            closeupIndex = idx;
            UnityEngine.UI.Image sourcePortrait = StatsList[idx] != null ? StatsList[idx].PortraitImage : null;
            closeup.SetCardinal(linkedCardinals[idx], closeupIndex, sourcePortrait, GetDisplayName(idx));
        }
    }

    public void HideCloseup()
    {
        //모든 UI 활성화
        for (int i = 1; i < 4; i++)
        {
            StatsList[i].gameObject.SetActive(true);
        }

        closeup.gameObject.SetActive(false);
        closeupIndex = -1;
    }
}
