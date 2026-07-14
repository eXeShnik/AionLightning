namespace AionLightning.Game.Model.Siege;

/// <summary>
/// Tracks per-map (Inggison/Gelkmaros/Abyss) race influence ratios computed from fortress ownership,
/// plus the derived global elyos/asmodian/balaur percentages used for the "X is now vulnerable"
/// broadcasts and (elsewhere) the PvP damage bonus. Java model.siege.Influence was a static singleton
/// (getInstance()); ported here as a plain mutable POCO owned by SiegeService instead, per this port's
/// DI-over-statics convention — call sites reach it via SiegeService.GetInfluence().
/// </summary>
public sealed class Influence
{
    public float InggisonElyos { get; private set; }
    public float InggisonAsmodians { get; private set; }
    public float InggisonBalaur { get; private set; }
    public float GelkmarosElyos { get; private set; }
    public float GelkmarosAsmodians { get; private set; }
    public float GelkmarosBalaur { get; private set; }
    public float AbyssElyos { get; private set; }
    public float AbyssAsmodians { get; private set; }
    public float AbyssBalaur { get; private set; }
    public float TiamarantaElyos { get; private set; }
    public float TiamarantaAsmodians { get; private set; }
    public float TiamarantaBalaur { get; private set; }
    public float GlobalElyos { get; private set; }
    public float GlobalAsmodians { get; private set; }
    public float GlobalBalaur { get; private set; }

    /// <summary>Java calculateInfluence(). Only Inggison/Gelkmaros (FortressLocation-owned) and Abyss
    /// (any siege location) feed the global ratio, matching Java exactly. Tiamaranta has no fortress
    /// data in this data set either (same as Java, where those fields stay 0).
    /// note: unlike Java, zero-total maps are guarded to 0 instead of producing NaN — Java's raw
    /// division has no such guard, but with real fortress data the denominator is never 0, so this
    /// only changes behavior for a degenerate empty-location edge case.</summary>
    public void Recalculate(IEnumerable<SiegeLocation> siegeLocations)
    {
        const float balaurea = 0.0019512194f;
        const float abyss = 0.006097561f;

        float eInggison = 0, aInggison = 0, bInggison = 0, tInggison = 0;
        float eGelkmaros = 0, aGelkmaros = 0, bGelkmaros = 0, tGelkmaros = 0;
        float eAbyss = 0, aAbyss = 0, bAbyss = 0, tAbyss = 0;

        foreach (var loc in siegeLocations)
        {
            switch (loc.WorldId)
            {
                case 210050000 when loc is FortressLocation:
                    tInggison += loc.InfluenceValue;
                    switch (loc.Race)
                    {
                        case SiegeRace.ELYOS: eInggison += loc.InfluenceValue; break;
                        case SiegeRace.ASMODIANS: aInggison += loc.InfluenceValue; break;
                        case SiegeRace.BALAUR: bInggison += loc.InfluenceValue; break;
                    }
                    break;
                case 220070000 when loc is FortressLocation:
                    tGelkmaros += loc.InfluenceValue;
                    switch (loc.Race)
                    {
                        case SiegeRace.ELYOS: eGelkmaros += loc.InfluenceValue; break;
                        case SiegeRace.ASMODIANS: aGelkmaros += loc.InfluenceValue; break;
                        case SiegeRace.BALAUR: bGelkmaros += loc.InfluenceValue; break;
                    }
                    break;
                case 400010000:
                    tAbyss += loc.InfluenceValue;
                    switch (loc.Race)
                    {
                        case SiegeRace.ELYOS: eAbyss += loc.InfluenceValue; break;
                        case SiegeRace.ASMODIANS: aAbyss += loc.InfluenceValue; break;
                        case SiegeRace.BALAUR: bAbyss += loc.InfluenceValue; break;
                    }
                    break;
            }
        }

        InggisonElyos = tInggison > 0 ? eInggison / tInggison : 0;
        InggisonAsmodians = tInggison > 0 ? aInggison / tInggison : 0;
        InggisonBalaur = tInggison > 0 ? bInggison / tInggison : 0;

        GelkmarosElyos = tGelkmaros > 0 ? eGelkmaros / tGelkmaros : 0;
        GelkmarosAsmodians = tGelkmaros > 0 ? aGelkmaros / tGelkmaros : 0;
        GelkmarosBalaur = tGelkmaros > 0 ? bGelkmaros / tGelkmaros : 0;

        AbyssElyos = tAbyss > 0 ? eAbyss / tAbyss : 0;
        AbyssAsmodians = tAbyss > 0 ? aAbyss / tAbyss : 0;
        AbyssBalaur = tAbyss > 0 ? bAbyss / tAbyss : 0;

        GlobalElyos = (InggisonElyos * balaurea + GelkmarosElyos * balaurea + AbyssElyos * abyss) * 100f;
        GlobalAsmodians = (InggisonAsmodians * balaurea + GelkmarosAsmodians * balaurea + AbyssAsmodians * abyss) * 100f;
        GlobalBalaur = (InggisonBalaur * balaurea + GelkmarosBalaur * balaurea + AbyssBalaur * abyss) * 100f;
    }
}
