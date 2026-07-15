using AionLightning.Game.Model.Siege;

namespace AionLightning.Game.Model.GameObjects.Siege;

/// <summary>
/// Java model.gameobjects.siege.SiegeNpc extends Npc, carrying a siege-location id + siege race used
/// for hostility checks between opposing siege spawns. This port's <see cref="Npc"/> is sealed (the
/// project favors composition over Java's class-per-behavior hierarchy), so SiegeNpc wraps an Npc
/// instance instead of extending it.
/// Constructed by <see cref="AionLightning.Game.Services.SpawnService.SpawnSiegeNpc"/> and registered via
/// <see cref="AionLightning.Game.Services.SiegeService.RegisterSiegeNpc"/> whenever
/// SiegeService.SpawnNpcs spawns a location's siege-spawn templates.
/// </summary>
public sealed class SiegeNpc(Npc npc, int siegeId, SiegeRace siegeRace, bool isBoss = false, bool isShieldGenerator = false)
{
    public Npc Npc { get; } = npc;
    public int SiegeId { get; } = siegeId;
    public SiegeRace SiegeRace { get; } = siegeRace;

    /// <summary>Java NpcTemplate.getAbyssNpcType() == AbyssNpcType.BOSS — identifies the single siege
    /// boss NPC whose death (see Services.Siege.Siege.InitSiegeBoss) ends the siege.
    /// note: always false in this port — AbyssNpcType isn't part of the ported NPC static data, so
    /// nothing ever constructs a SiegeNpc with isBoss: true yet; InitSiegeBoss will keep throwing until
    /// that data is ported.</summary>
    public bool IsBoss { get; } = isBoss;

    /// <summary>Java ai.siege.ShieldNpcAI2's <c>@AIName("siege_shieldnpc")</c> tag on the NPC's static
    /// template (NpcTemplate.Ai, unlike AbyssNpcType, IS part of this port's ported NPC data — see
    /// <see cref="AionLightning.Game.Services.SpawnService.SpawnSiegeNpc"/>) — identifies a fortress
    /// shield-generator NPC that <see cref="AionLightning.Game.Services.ShieldService"/> tracks.</summary>
    public bool IsShieldGenerator { get; } = isShieldGenerator;
}
