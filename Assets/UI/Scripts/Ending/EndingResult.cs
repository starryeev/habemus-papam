public enum EndingType
{
    None,
    PlayerPope,
    NpcPope,
    Crusade,
    SmokeBomb,
    PolarBear,
    DiplomaticVictory,
    Ascension,
    GreatSage,
    VivaSun,
    Bread
}

public static class EndingResult
{
    public static EndingType Current { get; private set; } = EndingType.None;

    public static void Set(EndingType endingType)
    {
        Current = endingType;
    }

    public static void Clear()
    {
        Current = EndingType.None;
    }
}
