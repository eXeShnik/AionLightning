namespace AionLightning.Game.Configs.Options;

/// <summary>
/// Siege subsystem gate. When false (default), siege data/persistence/getters/lifecycle objects
/// (Services.Siege.*) are still loaded and constructed, but no cron is armed
/// (SiegeService.ScheduleSieges no-ops), no spawn happens, and no SM_SIEGE_*/SM_FORTRESS_*/
/// SM_INFLUENCE_RATIO/SM_ABYSS_ARTIFACT_INFO/SM_SHIELD_EFFECT packet is ever sent to a client — the
/// enter-world/login flow is byte-verified against a real client, and broadcasting an unverified
/// 4.5-era siege opcode there could wedge it. Flip to true only after the opcodes are confirmed
/// against a live 4.6 client capture (see the per-packet TODO comments).
/// </summary>
public sealed record SiegeOptions
{
    public bool Enable { get; init; } = false;

    /// <summary>Java SiegeConfig.SIEGE_MEDAL_RATE — multiplier applied to siege/legion reward item counts.</summary>
    public int MedalRate { get; init; } = 1;

    /// <summary>Java SiegeConfig.RACE_PROTECTOR_SPAWN_SCHEDULE — daily cron that starts a siege for every
    /// outpost currently eligible (Java SiegeService.initSieges' "Outpost siege start" cron block).</summary>
    public string RaceProtectorSpawnCron { get; init; } = "0 0 21 ? * *";

    // note: the remaining Java SiegeConfig keys (BALAUR_AUTO_ASSAULT/BALAUR_ASSAULT_RATE, the Sunayaka/
    // Moltenus world-boss spawn schedules, SIEGE_HEALTH_MOD_ENABLED/MULTIPLIER, SIEGE_SHIELD_ENABLED,
    // SIEGE_IDA_ENABLED) belong to subsystems not ported in this phase (Balaur assault service, world
    // boss spawns, legendary-npc health scaling, geo shields) — adding config for them now would be dead
    // configuration until whichever phase ports those subsystems.
}
