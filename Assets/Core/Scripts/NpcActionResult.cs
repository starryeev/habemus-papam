public enum NpcActionOutcomeState
{
    None = 0,
    KnockedOut = 1,
    Dead = 2
}

public readonly struct NpcActionResult
{
    public NpcActionResult(string candidateName, NPCBehaviour behaviour, bool? succeeded,
        NpcActionOutcomeState outcomeState)
    {
        CandidateName = candidateName ?? string.Empty;
        Behaviour = behaviour;
        Succeeded = succeeded;
        OutcomeState = outcomeState;
    }

    public string CandidateName { get; }
    public NPCBehaviour Behaviour { get; }
    public bool? Succeeded { get; }
    public NpcActionOutcomeState OutcomeState { get; }

    public bool ShouldDisplay => Behaviour == NPCBehaviour.Pray || Behaviour == NPCBehaviour.Speech ||
        OutcomeState != NpcActionOutcomeState.None;
}
