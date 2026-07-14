using AionLightning.Game.Model.Siege;

namespace AionLightning.Game.Model.GameObjects.Base;

/// <summary>
/// Java model.gameobjects.Npc + services.base.Base's ad-hoc spawned/attackers lists — this port's
/// equivalent of <see cref="AionLightning.Game.Model.GameObjects.Siege.SiegeNpc"/> for base (Balaurea
/// capturable outpost) spawns: wraps an Npc instance with the owning base id/race and whether it's the
/// location's capture boss, tagged so <see cref="AionLightning.Game.Services.BaseService"/>'s registry
/// (and <see cref="AionLightning.Game.Combat.Handlers.BaseBossDeathHandler"/>) can distinguish it from a
/// plain world NPC.
/// note: unlike <see cref="AionLightning.Game.Model.GameObjects.Siege.SiegeNpc.IsBoss"/> (always false —
/// the underlying AbyssNpcType static data was never ported), base spawns carry an explicit BOSS/ATTACKER
/// marker in their own XML data (data/static_data/spawns/Bases/*.xml), so <see cref="IsBoss"/> here is
/// reliable.
/// Constructed by <see cref="AionLightning.Game.Services.SpawnService.SpawnBaseNpc"/> and registered via
/// <see cref="AionLightning.Game.Services.BaseService.RegisterBaseNpc"/>.
/// </summary>
public sealed class BaseNpc(Npc npc, int baseId, SiegeRace baseRace, bool isBoss)
{
    public Npc Npc { get; } = npc;
    public int BaseId { get; } = baseId;
    public SiegeRace BaseRace { get; } = baseRace;
    public bool IsBoss { get; } = isBoss;
}
