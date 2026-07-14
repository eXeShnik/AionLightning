using AionLightning.Game.Model;

namespace AionLightning.Game.Model.Siege;

/// <summary>Java model.siege.SiegeRace. Values match the Java raceId ordinals exactly — they are
/// written on the wire as-is by the siege packets (SM_SIEGE_LOCATION_INFO etc).</summary>
public enum SiegeRace
{
    ELYOS = 0,
    ASMODIANS = 1,
    BALAUR = 2,
}

public static class SiegeRaceExtensions
{
    /// <summary>Java SiegeRace.getByRace(Race) — maps a character race to its siege-ownership race,
    /// defaulting to BALAUR for anything that isn't ELYOS/ASMODIANS.</summary>
    public static SiegeRace FromPlayerRace(Race race) => race switch
    {
        Race.ASMODIANS => SiegeRace.ASMODIANS,
        Race.ELYOS => SiegeRace.ELYOS,
        _ => SiegeRace.BALAUR,
    };
}
