namespace AionLightning.Game.Configs.Options;

/// <summary>
/// WeatherService gate. Weather data always loads and rotates on schedule regardless of this flag
/// (<see cref="Services.WeatherService.GetWeatherEntries"/> stays callable/current) — only the client
/// SM_WEATHER broadcast is gated, since this port's SM_WEATHER opcode (0x43) collides with the existing
/// SM_GROUP_INFO packet in this codebase's opcode table (see SM_WEATHER's own doc comment) and hasn't
/// been byte-verified against a live 4.6 client capture.
/// </summary>
public sealed record WeatherOptions
{
    public bool SendToClients { get; init; } = false;

    /// <summary>Java WeatherService.checkWeathersTime — how often each map's weather is rolled forward.
    /// Java fired this on every in-game day-time change; no GameTime/DayTime system is ported here (see
    /// migration_plan.md), so this is approximated as a fixed cron interval instead.</summary>
    public string RotationCron { get; init; } = "0 0 * ? * *";
}
