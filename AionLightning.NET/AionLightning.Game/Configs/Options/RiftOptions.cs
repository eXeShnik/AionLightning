namespace AionLightning.Game.Configs.Options;

/// <summary>
/// Rift subsystem gate (Java CustomConfig.RIFT_ENABLED/RIFT_DURATION, gameserver.rift.enable/
/// gameserver.rift.duration). When false (default), rift location/spawn data is still loaded and
/// every RiftService API remains callable, but no cron is armed (ScheduleRifts no-ops), no NPC is
/// ever spawned, no SM_RIFT_ANNOUNCE broadcast is sent, and interacting with a would-be rift portal
/// NPC falls through to normal dialog handling — mirroring SiegeOptions' rationale: the general
/// rift announce packet variants added alongside this (actionId 0/2/3/4) haven't been byte-verified
/// against a live 4.6 client capture yet.
/// </summary>
public sealed record RiftOptions
{
    public bool Enable { get; init; } = false;

    /// <summary>Java CustomConfig.RIFT_DURATION (gameserver.rift.duration) — hour multiplier for how
    /// long an opened rift stays up before RiftService's auto-close fires (see RiftService's
    /// AutoCloseSecondsPerHour doc comment for why it's 3540s/hour, not 3600s).</summary>
    public int DurationHours { get; init; } = 1;
}
