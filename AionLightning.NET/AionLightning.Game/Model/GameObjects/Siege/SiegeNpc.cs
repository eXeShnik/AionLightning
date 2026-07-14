using AionLightning.Game.Model.Siege;

namespace AionLightning.Game.Model.GameObjects.Siege;

/// <summary>
/// Java model.gameobjects.siege.SiegeNpc extends Npc, carrying a siege-location id + siege race used
/// for hostility checks between opposing siege spawns. This port's <see cref="Npc"/> is sealed (the
/// project favors composition over Java's class-per-behavior hierarchy), so SiegeNpc wraps an Npc
/// instance instead of extending it.
/// note: nothing constructs this yet — the siege spawn engine (SpawnGroup2 filtered by
/// SiegeSpawnTemplate race/modtype, Java SiegeService.spawnNpcs/deSpawnNpcs) is P2. This type exists
/// so P2 has a ready-made siege-NPC identity to attach to spawned Npc instances.
/// </summary>
public sealed class SiegeNpc(Npc npc, int siegeId, SiegeRace siegeRace)
{
    public Npc Npc { get; } = npc;
    public int SiegeId { get; } = siegeId;
    public SiegeRace SiegeRace { get; } = siegeRace;
}
