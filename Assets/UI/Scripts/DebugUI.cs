using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class DebugUI : MonoBehaviour
{
    private const float GuaranteedElectionInfluence = 102f;
    private static readonly FieldInfo InfluenceField = typeof(Cardinal).GetField(
        "influence", BindingFlags.Instance | BindingFlags.NonPublic);

    [SerializeField] private string endingSceneName = "EndingScene";
    [SerializeField] private Item[] debugItems;
    private bool isHereticWarGameOverFlag;
    private Coroutine gameOverRoutine;
    private Coroutine restoreElectionOverrideRoutine;
    private GameObject itemPanel;
    private Cardinal electionOverrideCandidate;
    private float originalElectionOverrideInfluence;

    private void Awake()
    {
        ConfigureItemPanel();
        WireButtons();
    }

    public void QueuePlayerPopeElection()
    {
        QueueGuaranteedElection(FindPlayerCardinal());
    }

    public void QueueNpcPopeElection()
    {
        List<Cardinal> npcCandidates = GetNpcCandidates();
        if (npcCandidates.Count == 0)
        {
            Debug.LogWarning("[Election Debug] NPC candidate was not found.");
            return;
        }

        QueueGuaranteedElection(npcCandidates[Random.Range(0, npcCandidates.Count)]);
    }

    public void TriggerCrusadeE21101()
    {
        TriggerEnding(EndingType.Crusade, "E21101", 1);
    }

    public void TriggerCrusadeE31211()
    {
        TriggerEnding(EndingType.Crusade, "E31211", 1);
    }

    public void EnableHereticWarGameOverFlag()
    {
        isHereticWarGameOverFlag = true;
    }

    public void TriggerGreatSage()
    {
        TriggerEnding(EndingType.GreatSage, "E31101", 1);
    }

    public void TriggerPolarBear()
    {
        TriggerEnding(EndingType.PolarBear, "E31101", 2);
    }

    public void TriggerAscension()
    {
        TriggerEnding(EndingType.Ascension, "E31212", 1);
    }

    public void TriggerDiplomaticVictory()
    {
        TriggerEnding(EndingType.DiplomaticVictory, "E32002", 2);
    }

    public void TriggerSmokeBombFail()
    {
        TriggerEnding(EndingType.SmokeBomb);
    }

    public void TriggerGameOver()
    {
        if (gameOverRoutine == null)
        {
            gameOverRoutine = StartCoroutine(TriggerGameOverRoutine());
        }
    }

    private IEnumerator TriggerGameOverRoutine()
    {
        Cardinal player = FindPlayerCardinal();
        if (player == null)
        {
            Debug.LogWarning("[Ending Debug] Player cardinal was not found. Cannot trigger game over.");
            gameOverRoutine = null;
            yield break;
        }

        player.ChangeHp(1f - player.Hp);

        yield return new WaitForSecondsRealtime(3f);

        if (player == null)
        {
            gameOverRoutine = null;
            yield break;
        }

        player.ChangeHp(-1f);
        gameOverRoutine = null;

        if (!isHereticWarGameOverFlag)
        {
            yield break;
        }

        isHereticWarGameOverFlag = false;
        TriggerEnding(EndingType.Crusade, "E31201", 1);
    }

    private void WireButtons()
    {
        AddClick("E21101_Flag", TriggerCrusadeE21101);
        AddClick("E31211_Flag", TriggerCrusadeE31211);
        AddClick("E31201, E31202_Flag", EnableHereticWarGameOverFlag);
        AddClick("E31101_1_Flag", TriggerGreatSage);
        AddClick("E31101_2_Flag", TriggerPolarBear);
        AddClick("E31212_1_Flag", TriggerAscension);
        AddClick("E32002_2_Flag", TriggerDiplomaticVictory);
        AddClick("GameOver", TriggerGameOver);
        AddClick("smoke_shell_Fail", TriggerSmokeBombFail);
        AddClick("TurnEnd", EndTurn);
        AddClick("ItemPanelOpen", OpenItemPanel);

        if (debugItems == null)
        {
            return;
        }

        foreach (Item item in debugItems)
        {
            if (item == null || string.IsNullOrWhiteSpace(item.itemID))
            {
                continue;
            }

            Item capturedItem = item;
            AddClick($"ItemPanel/{capturedItem.itemID}", () => AddDebugItem(capturedItem));
        }
    }

    private void ConfigureItemPanel()
    {
        Transform panelTransform = transform.Find("ItemPanel");
        if (panelTransform == null)
        {
            Debug.LogWarning("[Debug UI] ItemPanel was not found.");
            return;
        }

        itemPanel = panelTransform.gameObject;
        itemPanel.SetActive(false);
    }

    private void OpenItemPanel()
    {
        if (itemPanel != null)
        {
            itemPanel.SetActive(true);
        }
    }

    private void EndTurn()
    {
        if (InGameManager.Instance != null)
        {
            InGameManager.Instance.DebugEndTurn();
        }
    }

    private void AddDebugItem(Item item)
    {
        if (item == null || InventoryManager.Instance == null)
        {
            return;
        }

        InventoryManager.Instance.AddItem(item);
    }

    private void QueueGuaranteedElection(Cardinal candidate)
    {
        if (candidate == null)
        {
            Debug.LogWarning("[Election Debug] Candidate was not found.");
            return;
        }

        RestoreElectionOverride();

        if (InfluenceField == null)
        {
            Debug.LogWarning("[Election Debug] Candidate influence field was not found.");
            return;
        }

        electionOverrideCandidate = candidate;
        originalElectionOverrideInfluence = candidate.Influence;
        InfluenceField.SetValue(candidate, GuaranteedElectionInfluence);
        restoreElectionOverrideRoutine = StartCoroutine(RestoreElectionOverrideAfterJudgment(candidate));
        Debug.Log($"[Election Debug] {candidate.name} will be selected with a 100% election chance.");
    }

    private IEnumerator RestoreElectionOverrideAfterJudgment(Cardinal candidate)
    {
        CheckUI checkUI = FindFirstObjectByType<CheckUI>(FindObjectsInactive.Include);
        yield return new WaitUntil(() =>
            checkUI != null &&
            checkUI.isActiveAndEnabled &&
            ElectionManager.Instance != null &&
            ElectionManager.Instance.CurrentWinnerCandidate == candidate);

        yield return null;
        RestoreElectionOverride();
    }

    private void RestoreElectionOverride()
    {
        if (restoreElectionOverrideRoutine != null)
        {
            StopCoroutine(restoreElectionOverrideRoutine);
            restoreElectionOverrideRoutine = null;
        }

        if (electionOverrideCandidate != null && InfluenceField != null)
        {
            InfluenceField.SetValue(electionOverrideCandidate, originalElectionOverrideInfluence);
        }

        electionOverrideCandidate = null;
    }

    private void AddClick(string buttonName, UnityEngine.Events.UnityAction action)
    {
        Transform buttonTransform = transform.Find(buttonName);
        if (buttonTransform == null)
        {
            Debug.LogWarning($"[Debug UI] Button was not found: {buttonName}");
            return;
        }

        Button button = buttonTransform.GetComponent<Button>();
        if (button == null)
        {
            Debug.LogWarning($"[Debug UI] Button component was not found: {buttonName}");
            return;
        }

        button.onClick.AddListener(action);
    }

    private void TriggerEnding(EndingType endingType, string triggerEventId = "", int optionIndex = 0)
    {
        EndingContext.CaptureFromCurrentGame();

        if (!string.IsNullOrWhiteSpace(triggerEventId))
        {
            EndingContext.SetEventTrigger(triggerEventId, optionIndex);
        }

        EndingResult.Set(endingType);
        if (SaveManager.Instance != null)
        {
            SaveManager.Instance.ClearContinueSaveForEnding();
        }
        Time.timeScale = 1f;

        if (!string.IsNullOrWhiteSpace(endingSceneName))
        {
            SceneManager.LoadScene(endingSceneName);
        }
    }

    private Cardinal FindPlayerCardinal()
    {
        if (CardinalManager.Instance == null || CardinalManager.Instance.Cardinals == null)
        {
            return null;
        }

        foreach (Cardinal cardinal in CardinalManager.Instance.Cardinals)
        {
            if (cardinal != null && cardinal.CompareTag("Player"))
            {
                return cardinal;
            }
        }

        return null;
    }

    private List<Cardinal> GetNpcCandidates()
    {
        var candidates = new List<Cardinal>();
        if (CardinalManager.Instance == null || CardinalManager.Instance.Cardinals == null)
        {
            return candidates;
        }

        foreach (Cardinal cardinal in CardinalManager.Instance.Cardinals)
        {
            if (cardinal != null && !cardinal.CompareTag("Player"))
            {
                candidates.Add(cardinal);
            }
        }

        return candidates;
    }

}
