using AionLightning.Game.Model;
using AionLightning.Game.Model.Legion;
using AionLightning.Game.Model.Siege;

namespace AionLightning.Game.Services.Siege;

/// <summary>
/// Java services.siegeservice.SiegeCounter — one <see cref="SiegeRaceCounter"/> per <see cref="SiegeRace"/>
/// for a single active siege.
/// </summary>
public sealed class SiegeCounter
{
    private readonly Dictionary<SiegeRace, SiegeRaceCounter> _counters = new()
    {
        [SiegeRace.ELYOS] = new SiegeRaceCounter(SiegeRace.ELYOS),
        [SiegeRace.ASMODIANS] = new SiegeRaceCounter(SiegeRace.ASMODIANS),
        [SiegeRace.BALAUR] = new SiegeRaceCounter(SiegeRace.BALAUR),
    };

    /// <summary>
    /// Java addDamage(Creature, int) — resolves the race from a Player directly. Java's other branch
    /// (<c>creature instanceof SiegeNpc</c>) can't be expressed the same way here since this port's
    /// <see cref="Model.GameObjects.Siege.SiegeNpc"/> wraps <see cref="Npc"/> rather than extending it —
    /// callers (see <see cref="AionLightning.Game.Services.Siege.Siege.AddBossDamage"/>) resolve that
    /// lookup via <see cref="SiegeService.GetSiegeNpc"/> and pass the race in explicitly.
    /// Unresolvable creature types are ignored, matching Java's "please debug me" warn-and-return path.
    /// </summary>
    public void AddDamage(Creature creature, int damage, SiegeRace? npcSiegeRace = null)
    {
        SiegeRace race;
        if (creature is Player player) race = SiegeRaceExtensions.FromPlayerRace(player.Race);
        else if (npcSiegeRace is { } resolved) race = resolved;
        else return;

        _counters[race].AddPoints(creature, damage);
    }

    public void AddAbyssPoints(Player player, int ap) =>
        _counters[SiegeRaceExtensions.FromPlayerRace(player.Race)].AddAbyssPoints(player, ap);

    public void AddGloryPoints(Player player, int gp) =>
        _counters[SiegeRaceExtensions.FromPlayerRace(player.Race)].AddGloryPoints(player, gp);

    public SiegeRaceCounter GetRaceCounter(SiegeRace race) => _counters[race];

    public void AddLegionDamage(SiegeRace race, Legion legion, int damage) => _counters[race].AddLegionDamage(legion, damage);

    public void AddRaceDamage(SiegeRace race, int damage) => _counters[race].AddTotalDamage(damage);

    /// <summary>Java getWinnerRaceCounter() — the race with the highest total boss damage.</summary>
    public SiegeRaceCounter GetWinnerRaceCounter() => _counters.Values.OrderBy(c => c).First();
}
