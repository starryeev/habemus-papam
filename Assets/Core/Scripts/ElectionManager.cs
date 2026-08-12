using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using UnityEngine.SceneManagement;

public class ElectionManager : MonoBehaviour
{
    public static ElectionManager Instance { get; private set; }

    [Header("UI 연결")]
    [SerializeField] private StatsUI statsUI;
    [Tooltip("투표 화면")]
    [SerializeField] private CheckUI checkUI;
    [Tooltip("당선 확정 후 이동할 엔딩 씬 이름")]
    [SerializeField] private string endingSceneName = "EndingScene";

    private Cardinal currentWinnerCandidate;
    public Cardinal CurrentWinnerCandidate => currentWinnerCandidate;
    private bool isElected = false;
    public bool IsElected => isElected;

    public void DebugElectPlayer()
    {
        Cardinal playerCandidate = FindPlayerCandidate();
        if (playerCandidate == null)
        {
            Debug.LogWarning("[Election Debug] Player candidate was not found.");
            return;
        }

        ForceElectCandidate(playerCandidate, EndingType.PlayerPope);
    }

    public void DebugElectNpc()
    {
        Cardinal npcCandidate = FindNpcCandidate();
        if (npcCandidate == null)
        {
            Debug.LogWarning("[Election Debug] NPC candidate was not found.");
            return;
        }

        ForceElectCandidate(npcCandidate, EndingType.NpcPope);
    }

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    void Start()
    {
        if (checkUI != null) checkUI.gameObject.SetActive(false);
        isElected = false;
    }

    public void OnConclaveEnded()
    {
        if (checkUI != null)
        {
            SetCheckUI();
        }
    }

    private void SetCheckUI()
    {
        if (statsUI == null) return;
        checkUI.gameObject.SetActive(true);

        Cardinal[] candidates = statsUI.LinkedCardinals;
        int winner = GetWinner(candidates);
        currentWinnerCandidate = candidates[winner];
        checkUI.SetWinner(winner);

        if (currentWinnerCandidate != null)
        {
            float winProbability = CalculateWinProbability(currentWinnerCandidate);
            checkUI.SetProbability(winProbability);
            ExecuteJudgment();
        }
    }
    private int GetWinner(Cardinal[] candidates)
    {
        float[,] stats = new float[4,2];
        int winner = 0;

        for(int i = 0; i<4; i++)
        {
            float influence = candidates[i].Influence;
            float piety = candidates[i].Piety;
            stats[i,0] = Mathf.Max(influence, piety);
            stats[i,1] = Mathf.Min(influence, piety);
        }
        
        for(int i = 1; i<4; i++)
        {
            if(stats[i,0] > stats[winner,0]) winner = i;
            else if (stats[i,0] == stats[winner,0])
            {
                if(stats[i,1]>stats[winner,1]) winner = i;
                //스탯 다 같으면 번호 낮은 놈이 승자. StatsUI와 동일한 방법으로 적용해야 함.
            }
        }
        return winner;
    }

    // 최종 확률 판정 및 게임 결과 도출
    public void ExecuteJudgment()
    {
        if (currentWinnerCandidate == null || InGameManager.Instance == null) return;

        float winProbability = CalculateWinProbability(currentWinnerCandidate);

        float diceRoll = UnityEngine.Random.Range(0f, 100f);
        isElected = diceRoll <= winProbability;

        if (isElected && currentWinnerCandidate.CompareTag("Player"))
        {
            Item smokeBomb = InventoryManager.Instance.GetItemByID("I012");

            if (smokeBomb != null && smokeBomb is I012 bombScript)
            {
                Debug.Log("플레이어 당선 위기! 인벤토리의 연막탄(I012)을 확인했습니다.");
                float playerPiety = currentWinnerCandidate.Piety;

                if (bombScript.TryDefendElection(playerPiety))
                {
                    Debug.Log("<color=white>연막탄 성공! 당선이 무효화되어 부결 처리됩니다.</color>");
                    isElected = false; 
                }
                else
                {
                    Debug.Log("<color=red>연막탄 실패... 그대로 게임 오버 판정으로 넘어갑니다.</color>");
                    LoadEndingScene(EndingType.SmokeBomb);
                    return;
                }
            }
        }
    }
    public void GetNextScenes()
    {
        if (isElected)
        {
            Debug.Log($"-> {currentWinnerCandidate.name} 당선!");

            if (currentWinnerCandidate.CompareTag("Player"))
            {
                Debug.Log($"[Election] Player elected: {currentWinnerCandidate.name}");
                LoadEndingScene(EndingType.PlayerPope);
            }
            else
            {
                Debug.Log($"[Election] NPC elected: {currentWinnerCandidate.name}");
                LoadEndingScene(EndingType.NpcPope);
            }
        }
        else
        {
            if (ActionRecordManager.Instance != null)
            {
                ActionRecordManager.Instance.RecordPapalElectionFailed();
                ClosePanel();
            }
        }
    }

    private void ClosePanel()
    {
        if (checkUI != null)
        {
            checkUI.gameObject.SetActive(false);
        }

        if (InGameManager.Instance.IsSushiOn && SushiUI.Instance != null)
        {
            SushiUI.Instance.Show();
        }
        else
        {
            InGameManager.Instance.StartConclaveCycle();
        }
    }

    private float CalculateWinProbability(Cardinal candidate)
    {
        if (InGameManager.Instance == null) return 0f;

        int currentDay = Mathf.Max(1, InGameManager.Instance.GetCurrentDay());
        float dayBonus = currentDay == 1 ? 0f : currentDay == 2 ? 5f : currentDay == 3 ? 10f : 15f;
        float candidatePassive = InGameManager.Instance.GetNpcCandidateNumber(candidate) == 2 ? -2f : 0f;
        return Mathf.Clamp(dayBonus + candidate.Influence + candidatePassive, 0f, 100f);
    }

    private Cardinal FindPlayerCandidate()
    {
        Cardinal[] candidates = GetCandidatePool();
        foreach (Cardinal candidate in candidates)
        {
            if (candidate != null && IsPlayerCandidate(candidate))
            {
                return candidate;
            }
        }

        return null;
    }

    private Cardinal FindNpcCandidate()
    {
        Cardinal[] candidates = GetCandidatePool();
        foreach (Cardinal candidate in candidates)
        {
            if (candidate != null && !IsPlayerCandidate(candidate))
            {
                return candidate;
            }
        }

        return null;
    }

    private Cardinal[] GetCandidatePool()
    {
        if (statsUI != null && statsUI.LinkedCardinals != null)
        {
            foreach (Cardinal candidate in statsUI.LinkedCardinals)
            {
                if (candidate != null)
                {
                    return statsUI.LinkedCardinals;
                }
            }
        }

        return FindObjectsByType<Cardinal>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None);
    }

    private bool IsPlayerCandidate(Cardinal candidate)
    {
        if (candidate == null)
        {
            return false;
        }

        return candidate.CompareTag("Player") ||
               candidate.gameObject.name.Contains("Player");
    }

    private void ForceElectCandidate(Cardinal candidate, EndingType endingType)
    {
        currentWinnerCandidate = candidate;
        Debug.Log($"[Election Debug] {candidate.name} elected. Ending: {endingType}");
        LoadEndingScene(endingType);
    }

    private void LoadEndingScene(EndingType endingType)
    {
        EndingContext.CaptureFromCurrentGame(currentWinnerCandidate);

        if (ActionRecordManager.Instance != null &&
            (endingType == EndingType.PlayerPope || endingType == EndingType.NpcPope))
        {
            string electedName = endingType == EndingType.PlayerPope
                ? EndingContext.PlayerName
                : EndingContext.ElectedNpcName;

            if (string.IsNullOrWhiteSpace(electedName) && currentWinnerCandidate != null)
            {
                electedName = currentWinnerCandidate.name;
            }

            CandidateSlot candidateSlot = ResolveCandidateSlot(currentWinnerCandidate);
            ActionRecordManager.Instance.RecordPapalElection(endingType, electedName, candidateSlot);
        }

        EndingResult.Set(endingType);
        if (SaveManager.Instance != null)
        {
            SaveManager.Instance.ClearContinueSaveForEnding();
        }
        Time.timeScale = 1f;
        SceneManager.LoadScene(endingSceneName);
    }

    private CandidateSlot ResolveCandidateSlot(Cardinal candidate)
    {
        StatsUI sourceStatsUI = statsUI;
        if (sourceStatsUI == null && CardinalManager.Instance != null)
        {
            sourceStatsUI = CardinalManager.Instance.StatsUI;
        }

        int linkedIndex = sourceStatsUI != null
            ? sourceStatsUI.GetLinkedCardinalIndex(candidate)
            : -1;

        switch (linkedIndex)
        {
            case 0: return CandidateSlot.Player;
            case 1: return CandidateSlot.Npc1;
            case 2: return CandidateSlot.Npc2;
            case 3: return CandidateSlot.Npc3;
            default: return CandidateSlot.Unknown;
        }
    }
}
