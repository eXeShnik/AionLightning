namespace AionLightning.Game.Model;

public enum PlayerClass : byte
{
    WARRIOR      = 0,
    GLADIATOR    = 1,
    TEMPLAR      = 2,
    SCOUT        = 3,
    ASSASSIN     = 4,
    RANGER       = 5,
    MAGE         = 6,
    SORCERER     = 7,
    SPIRIT_MASTER = 8,
    PRIEST       = 9,
    CLERIC       = 10,
    CHANTER      = 11,
    ENGINEER     = 12,
    RIDER        = 13,
    GUNNER       = 14,
    ARTIST       = 15,
    BARD         = 16,
    ALL          = 17,
}

public static class PlayerClassExtensions
{
    public static bool IsStartingClass(this PlayerClass cls) => cls is
        PlayerClass.WARRIOR  or PlayerClass.SCOUT   or
        PlayerClass.MAGE     or PlayerClass.PRIEST  or
        PlayerClass.ENGINEER or PlayerClass.ARTIST  or
        PlayerClass.RIDER;

    public static PlayerClass FromId(byte id) => id < 17
        ? (PlayerClass)id
        : throw new ArgumentOutOfRangeException(nameof(id), $"Unknown class id {id}");

    /// <summary>Maps an advanced class to its starting class (Java PlayerClass.getStartingClassFor).
    /// A starting class maps to itself.</summary>
    public static PlayerClass GetStartingClassFor(this PlayerClass cls) => cls switch
    {
        PlayerClass.GLADIATOR or PlayerClass.TEMPLAR       => PlayerClass.WARRIOR,
        PlayerClass.ASSASSIN  or PlayerClass.RANGER        => PlayerClass.SCOUT,
        PlayerClass.SORCERER  or PlayerClass.SPIRIT_MASTER => PlayerClass.MAGE,
        PlayerClass.CLERIC    or PlayerClass.CHANTER       => PlayerClass.PRIEST,
        PlayerClass.GUNNER                                 => PlayerClass.ENGINEER,
        PlayerClass.BARD                                   => PlayerClass.ARTIST,
        _                                                  => cls,
    };
}
