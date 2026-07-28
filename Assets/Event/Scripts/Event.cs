using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public abstract class Event : ScriptableObject
{
    [SerializeField] public string eventID;
    [SerializeField] public string eventName;
    [TextArea] public string eventDescription;
    [SerializeField] public Sprite itemImage;
    [SerializeField] public int maxAppear;

    [SerializeField] public float eventWeightBase;
    [SerializeField] public float eventWeightMultiplier;

    [SerializeField] public List<Event> preEvents;
    [SerializeField] public List<Event> conflictEvents;

    [SerializeField] public string option1;
    [SerializeField] public float option1Chance;
    [SerializeField] public string option1Requirement = "";
    [TextArea] public string option1SuccessDescription;
    [TextArea] public string option1SuccessResult;
    [TextArea] public string option1FailDescription;
    [TextArea] public string option1FailResult;

    [SerializeField] public string option2;
    [SerializeField] public float option2Chance;
    [TextArea] public string option2SuccessDescription;
    [TextArea] public string option2SuccessResult;
    [TextArea] public string option2FailDescription;
    [TextArea] public string option2FailResult;

    public float GetEventWeight()
    {
        int day = InGameManager.Instance != null ? InGameManager.Instance.GetCurrentDay() : 1;
        return eventWeightBase + eventWeightMultiplier * Mathf.Max(1, day);
    }

    public virtual bool CanChoiceOption1(Cardinal performer)
    {
        return true;
    }
    public virtual bool CanChoiceOption2(Cardinal performer)
    {
        return true;
    }

    public abstract bool OnChoiceOption1(Cardinal performer);
    public abstract bool OnChoiceOption2(Cardinal performer);

    protected bool FinishChoice(int optionIndex, bool succeeded)
    {
        if (InGameManager.Instance != null && InGameManager.Instance.EventManager != null)
        {
            InGameManager.Instance.EventManager.RecordChoice(eventID, optionIndex, succeeded);
        }

        return succeeded;
    }

    protected bool FinishChoiceWithEnding(int optionIndex, EndingType endingType)
    {
        FinishChoice(optionIndex, true);
        EndingContext.CaptureFromCurrentGame();
        EndingContext.SetEventTrigger(eventID, optionIndex);
        EndingResult.Set(endingType);
        Time.timeScale = 1f;
        SceneManager.LoadScene("EndingScene");
        return true;
    }

    protected Cardinal GetCandidate(int candidateNumber)
    {
        if (candidateNumber < 1 || candidateNumber > 3 || CardinalManager.Instance == null)
        {
            return null;
        }

        StatsUI statsUI = CardinalManager.Instance.StatsUI;
        Cardinal[] linked = statsUI != null ? statsUI.LinkedCardinals : null;
        if (linked != null && linked.Length > candidateNumber && linked[candidateNumber] != null)
        {
            return linked[candidateNumber];
        }

        List<Cardinal> aiCardinals = CardinalManager.Instance.GetAICardinlas();
        int index = candidateNumber - 1;
        return index < aiCardinals.Count ? aiCardinals[index] : null;
    }

    protected void EliminateCandidate(int candidateNumber)
    {
        Cardinal candidate = GetCandidate(candidateNumber);
        if (candidate != null)
        {
            candidate.ChangeHp(-candidate.Hp);
        }
    }

    protected void SetCandidateStats(int candidateNumber, float hp, float influence, float piety)
    {
        Cardinal candidate = GetCandidate(candidateNumber);
        if (candidate == null)
        {
            return;
        }

        candidate.ChangeHp(hp - candidate.Hp);
        candidate.ChangeInfluence(influence - candidate.Influence);
        candidate.ChangePiety(piety - candidate.Piety);
    }

    protected void CopyDefinitionFrom(Event source)
    {
        if (source == null || source == this) return;

        eventID = source.eventID;
        eventName = source.eventName;
        eventDescription = source.eventDescription;
        itemImage = source.itemImage;
        maxAppear = source.maxAppear;
        eventWeightBase = source.eventWeightBase;
        eventWeightMultiplier = source.eventWeightMultiplier;
        preEvents = source.preEvents;
        conflictEvents = source.conflictEvents;
        option1 = source.option1;
        option1Chance = source.option1Chance;
        option1Requirement = source.option1Requirement;
        option1SuccessDescription = source.option1SuccessDescription;
        option1SuccessResult = source.option1SuccessResult;
        option1FailDescription = source.option1FailDescription;
        option1FailResult = source.option1FailResult;
        option2 = source.option2;
        option2Chance = source.option2Chance;
        option2SuccessDescription = source.option2SuccessDescription;
        option2SuccessResult = source.option2SuccessResult;
        option2FailDescription = source.option2FailDescription;
        option2FailResult = source.option2FailResult;
    }
}
