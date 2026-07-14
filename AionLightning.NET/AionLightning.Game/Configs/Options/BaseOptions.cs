namespace AionLightning.Game.Configs.Options;

/// <summary>
/// BaseService (Balaurea capturable-outpost) subsystem gate — mirrors <see cref="SiegeOptions"/>'s
/// pattern exactly. When false (default), base location/spawn data and ownership persistence still load
/// and the service's getters/capture API remain callable, but no cron is armed
/// (BaseService.ScheduleBasesAsync no-ops), no defender/boss/attacker NPC is ever spawned
/// (BaseService.StartAllAsync/StartBase no-op), and no SM_NPC_INFO/SM_DELETE/SM_ABNORMAL_EFFECT packet is
/// ever sent to a client.
/// note: Java's own base-capture feature was itself unfinished in the 4.6 source (services.base.Base
/// called a DataManager.SPAWNS_DATA2.getBaseSpawnsByLocId method that doesn't exist anywhere on the real
/// SpawnsData2 source, and its persisted `base.race` column enum couldn't actually hold the value
/// model.Race.toString() would ever produce for the default/neutral owner — see Dao/BaseDaoImpl.cs's doc
/// comment). This port fixes both gaps, but — like SiegeOptions — flip Enable to true only once base
/// spawns/NPC ids have been validated against a live 4.6 client capture.
/// </summary>
public sealed record BaseOptions
{
    public bool Enable { get; init; } = false;

    /// <summary>Java Base.delayedAssault's Rnd(15,20)*60000 randomized re-roll window, approximated as a
    /// fixed cron interval — how often BaseService rolls a temporary rival-race assault wave for each
    /// currently-owned base (see BaseService.TriggerAssaultsAsync).</summary>
    public string AssaultCron { get; init; } = "0 0/20 * ? * *";

    /// <summary>Java Base's stopAssault 5*60000 (5 min) attacker-despawn window.</summary>
    public int AssaultDurationSeconds { get; init; } = 300;

    /// <summary>Skill id applied to online players of the capturing race when a base changes hands.
    /// 0 (default) disables the buff grant entirely — Java's own base-capture flow never shipped a
    /// canonical skill id for this (the whole subsystem was unfinished; see this record's own doc
    /// comment), so this is a server-operator policy knob rather than a ported constant.</summary>
    public int CaptureBuffSkillId { get; init; } = 0;
}
