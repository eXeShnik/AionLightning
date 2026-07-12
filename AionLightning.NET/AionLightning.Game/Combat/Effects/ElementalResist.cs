using AionLightning.Game.Model;

namespace AionLightning.Game.Combat.Effects;

/// <summary>
/// S4b: shared home for CM_CASTSPELL's former private `GetElementalResist` helper (M339). Body copied
/// verbatim — returns the target's elemental resistance value for the given element string
/// (Java scale; 1250 = 100% immune).
/// </summary>
public static class ElementalResist
{
    public static int Get(Creature target, string element) => element switch
    {
        "FIRE"  => target.FireResist,
        "WATER" => target.WaterResist,
        "WIND"  => target.WindResist,
        "EARTH" => target.EarthResist,
        _       => 0,
    };
}
