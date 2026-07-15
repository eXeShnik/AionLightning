namespace AionLightning.Game.Configs.Options;

/// <summary>
/// VortexService (Dimensional Vortex — Brusthonin/Theobomos invasion) subsystem gate, mirroring Java
/// CustomConfig.VORTEX_ENABLED/VORTEX_DURATION/VORTEX_BRUSTHONIN_SCHEDULE/VORTEX_THEOBOMOS_SCHEDULE
/// (gameserver.vortex.* in this repo's AL-Game/config/main/custom.properties). When false (default),
/// vortex location/spawn data still loads and every VortexService API remains callable, but
/// VortexServiceHostedService's boot-time peace-state spawn + cron arming both no-op, no invasion NPC
/// is ever spawned, and interacting with a would-be vortex portal NPC falls through to normal dialog
/// handling — mirroring RiftOptions'/SiegeOptions' rationale: the shared SM_RIFT_ANNOUNCE isVortex path
/// this reuses hasn't been byte-verified against a live 4.6 client capture yet.
/// </summary>
public sealed record VortexOptions
{
    public bool Enable { get; init; } = false;

    /// <summary>Java CustomConfig.VORTEX_DURATION (gameserver.vortex.duration, default 2) — hour
    /// multiplier for how long an invasion stays open before VortexService's auto-end timer fires
    /// (unless the generator boss dies first — see Combat.Handlers.VortexGeneratorDeathHandler).</summary>
    public int DurationHours { get; init; } = 2;

    /// <summary>Java CustomConfig.VORTEX_BRUSTHONIN_SCHEDULE (gameserver.vortex.brusthonin.schedule) —
    /// cron for vortex location id 1 (Elyos invade Asmodian Brusthonin via the Kaisinel Academy portal).</summary>
    public string BrusthoninCron { get; init; } = "0 0 22 ? * SAT";

    /// <summary>Java CustomConfig.VORTEX_THEOBOMOS_SCHEDULE (gameserver.vortex.theobomos.schedule) —
    /// cron for vortex location id 0 (Asmodians invade Elyos Theobomos via the Marchutan Priory portal).</summary>
    public string TheobomosCron { get; init; } = "0 0 22 ? * SUN";

    /// <summary>Java services.rift.RiftEnum's two isVortex()=true entries (KAISINEL_AM id 1170,
    /// MARCHUTAN_AM id 1280) both use these same three values — the entry portal's max concurrent
    /// entries and accepted level range. This port collapses the two into one config knob since every
    /// vortex location uses identical numbers in the 4.6 data set.</summary>
    public int MaxEntries { get; init; } = 24;
    public int MinLevel { get; init; } = 45;
    public int MaxLevel { get; init; } = 60;
}
