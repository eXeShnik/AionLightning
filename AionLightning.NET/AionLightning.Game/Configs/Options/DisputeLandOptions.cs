namespace AionLightning.Game.Configs.Options;

/// <summary>
/// Disputed Land subsystem (Java CustomConfig gameserver.dispute.*). Five daily cron windows roll a
/// chance (higher on weekends) to flip open-world PvP on for Tiamaranta (600030000) — see
/// <see cref="Services.DisputeLandService"/>'s doc comment on the world-600020001 skip quirk it ports
/// verbatim from Java. One window (16h-21h, <see cref="FixedSchedule"/>) is always active, no roll.
/// Default off (<see cref="Enable"/> = false): matches every other gated subsystem in this port —
/// nothing about the byte-verified login packet sequence changes unless this is explicitly turned on.
/// </summary>
public sealed record DisputeLandOptions
{
    public bool Enable { get; init; } = false;

    /// <summary>Java gameserver.dispute.random.chance — percent chance to activate on a weekday roll.</summary>
    public int RandomChance { get; init; } = 50;

    /// <summary>Java gameserver.dispute.weekend.random.chance — percent chance to activate on a weekend roll.</summary>
    public int WeekendRandomChance { get; init; } = 75;

    /// <summary>Java gameserver.dispute.random.schedule — 11h roll window.</summary>
    public string RandomSchedule { get; init; } = "0 0 11 ? * *";

    /// <summary>Java gameserver.dispute.random2.schedule — 21h roll window.</summary>
    public string Random2Schedule { get; init; } = "0 0 21 ? * *";

    /// <summary>Java gameserver.dispute.random3.schedule — 2h roll window.</summary>
    public string Random3Schedule { get; init; } = "0 0 2 ? * *";

    /// <summary>Java gameserver.dispute.random4.schedule — 7h roll window.</summary>
    public string Random4Schedule { get; init; } = "0 0 7 ? * *";

    /// <summary>Java gameserver.dispute.fixed.schedule — 16h window, always activates (no roll).</summary>
    public string FixedSchedule { get; init; } = "0 0 16 ? * *";

    /// <summary>Java gameserver.dispute.time — hours the dispute stays active once triggered.</summary>
    public int DisputeLandTimeHours { get; init; } = 5;
}
