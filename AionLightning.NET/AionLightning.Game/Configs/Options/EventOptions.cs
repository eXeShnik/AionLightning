namespace AionLightning.Game.Configs.Options;

/// <summary>
/// EventService (seasonal events — Lunar Festival, Daeva Day, etc.) subsystem gate — mirrors
/// <see cref="BaseOptions"/>'s/<see cref="SiegeOptions"/>'s pattern exactly. When false (default), event
/// data still loads and <see cref="AionLightning.Game.Model.Templates.Event.EventTemplate.IsActive"/> /
/// <see cref="AionLightning.Game.Services.EventService.CheckQuestIsActive"/> remain callable, but the
/// recheck cron is never armed, no event NPC is ever spawned or despawned, and no event quest is ever
/// auto-started or re-maintained on player login. Flip to true only once the event NPC ids/spawn spots
/// in events_config.xml have been validated against a live 4.6 client.
/// </summary>
public sealed record EventOptions
{
    public bool Enable { get; init; } = false;

    /// <summary>Java EventService.CHECK_TIME_PERIOD (1000*60*5 — every 5 minutes) expressed as a Quartz
    /// cron expression.</summary>
    public string CheckCron { get; init; } = "0 0/5 * ? * *";
}
