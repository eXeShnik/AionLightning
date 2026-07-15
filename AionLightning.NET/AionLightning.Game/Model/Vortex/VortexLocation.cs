namespace AionLightning.Game.Model.Vortex;

/// <summary>
/// Java model.vortex.VortexLocation — runtime state for one Dimensional Vortex invasion location.
/// Java's own version also carried a live RVController reference, a list of zone-region instances, and
/// a <c>ZoneHandler</c> callback (onEnterZone/onLeaveZone) that drove invader/defender alliance
/// membership as players physically crossed the invasion zone's polygon. This port's <see
/// cref="Services.ZoneService"/> has no generic per-zone handler registration hook (it only drives
/// quest/instance callbacks — see its own doc comment), so invader/defender membership here is instead
/// driven directly by <see cref="Services.VortexService"/>'s portal-entry/invasion-start/invasion-stop
/// calls rather than real-time zone-polygon tracking — see VortexService's doc comment for the exact
/// simplification and what it costs versus Java's behavior.
/// </summary>
public sealed class VortexLocation(VortexTemplate template)
{
    public int Id => template.Id;
    public Race DefendsRace => template.DefendsRace;
    public Race OffenceRace => template.OffenceRace;
    public VortexPoint Home => template.Home;
    public VortexPoint Resurrection => template.Resurrection;
    public VortexPoint Start => template.Start;

    /// <summary>Java DimensionalVortex's started/finished pair, collapsed to one flag — true from
    /// VortexService.StartInvasion until EndInvasion runs.</summary>
    public bool IsActive { get; set; }

    /// <summary>The master (home-side, interactable) portal NPC spawned while this location is open —
    /// null while at peace. Java's RVController-wrapped rift npc (KAISINEL_AM/MARCHUTAN_AM).</summary>
    public Npc? Master { get; set; }

    /// <summary>The slave (invasion-side, arrival) portal NPC — Java's KAISINEL_AS/MARCHUTAN_AS.</summary>
    public Npc? Slave { get; set; }

    /// <summary>Java DimensionalVortex.generator — the siege-boss-like NPC whose death ends the
    /// invasion early (see Combat.Handlers.VortexGeneratorDeathHandler). Null when no invasion-state
    /// spawn data tagged a generator NPC id (see DataHolders.VortexSpawnData's doc comment on why this
    /// data doesn't ship in this repo) or while at peace.</summary>
    public Npc? Generator { get; set; }

    /// <summary>Java DimensionalVortex.generatorDestroyed — sole purpose is telling
    /// VortexService.EndInvasion's own auto-timer not to double-run the end sequence when the generator
    /// death handler already triggered it.</summary>
    public bool GeneratorDestroyed { get; set; }

    /// <summary>Every NPC (master, slave, any invasion-state spawn including <see cref="Generator"/>)
    /// currently spawned for this location — Java VortexLocation.spawned.</summary>
    public List<Npc> Spawned { get; } = [];

    /// <summary>Java RVController.passedPlayers — players who have stepped through the master portal
    /// during the current invasion.</summary>
    public HashSet<int> PassedPlayers { get; } = [];

    /// <summary>Java DimensionalVortex(Invasion).invaders, flattened from a full PlayerAlliance to a
    /// plain id set — see VortexService's doc comment on why alliance formation isn't ported.</summary>
    public HashSet<int> Invaders { get; } = [];

    /// <summary>Java DimensionalVortex(Invasion).defenders — same flattening as <see cref="Invaders"/>.</summary>
    public HashSet<int> Defenders { get; } = [];
}
