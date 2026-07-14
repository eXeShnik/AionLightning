using AionLightning.Game.Model.Siege;
using AionLightning.Game.Model.Templates.Base;

namespace AionLightning.Game.Model.Base;

/// <summary>
/// Java model.base.BaseLocation — runtime state for one capturable Balaurea outpost. Java's own
/// model.base.Base was dead code (an empty-bodied stub never wired to anything — every method either
/// returned a field never assigned by its own constructor or was a no-op); the real per-instance engine
/// lived in services.base.Base, whose id/race/spawned-npc bookkeeping this port folds into
/// <see cref="AionLightning.Game.Services.BaseService"/> + <see cref="Model.GameObjects.Base.BaseNpc"/>
/// instead — the same split <see cref="SiegeLocation"/>/<see cref="AionLightning.Game.Services.SiegeService"/>
/// already use for sieges.
/// note: Java default-owned every base by the neutral Lepharist faction (<c>Race.NPC</c>, from
/// model.Race — a Java enum with many NPC sub-races). This port's player-facing <see cref="Model.Race"/>
/// only has ELYOS/ASMODIANS/PC_ALL, so neutral/Lepharist ownership is represented here by
/// <see cref="SiegeRace.BALAUR"/> instead — the same three-way ownership convention already used for
/// sieges, and (unlike Java's own persisted `base.race` column, whose enum literals never actually
/// matched what model.Race.toString() would produce — see Dao.BaseDaoImpl's doc comment) one that
/// round-trips correctly through persistence.
/// </summary>
public sealed class BaseLocation
{
    public BaseTemplate Template { get; }
    public int LocationId { get; }
    public int WorldId { get; }
    public int NameId { get; }
    public string Name { get; }

    public SiegeRace Race { get; set; } = SiegeRace.BALAUR;
    public DateTime? LastCaptureTime { get; set; }

    public BaseLocation(BaseTemplate template)
    {
        Template = template;
        LocationId = template.Id;
        WorldId = template.World;
        NameId = template.NameId;
        Name = template.Name;
    }
}
