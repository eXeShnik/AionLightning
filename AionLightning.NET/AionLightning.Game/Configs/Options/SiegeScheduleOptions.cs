namespace AionLightning.Game.Configs.Options;

/// <summary>
/// Fortress/source siege cron schedule (Java configs.schedule.SiegeSchedule, loaded at runtime from
/// config/schedule/siege_schedule.xml — an XML schedule loader has no C# port yet). The defaults below
/// are transcribed verbatim from this repo's AL-Game/config/schedule/siege_schedule.xml, keyed by
/// siege_location id; override via GameServer:Siege:Schedule:Fortresses/Sources in appsettings.json if a
/// server needs a different table.
/// </summary>
public sealed record SiegeScheduleOptions
{
    public Dictionary<int, List<string>> Fortresses { get; init; } = new()
    {
        [1011] = ["0 0 21 ? * SAT"],
        [1131] = ["0 0 11 ? * SAT"],
        [1132] = ["0 0 11 ? * SAT"],
        [1141] = ["0 0 11 ? * SAT"],
        [1211] = ["0 0 11 ? * SAT"],
        [1221] = ["0 0 11 ? * SUN"],
        [1231] = ["0 0 11 ? * SUN"],
        [1241] = ["0 0 11 ? * SUN"],
        [1251] = ["0 0 11 ? * SUN"],
        [2011] = ["0 0 21 ? * MON,WED,SUN", "0 0 10 ? * THU,SAT"],
        [2021] = ["0 0 21 ? * TUE,WED,SUN", "0 0 10 ? * FRI,SAT"],
        [3011] = ["0 0 21 ? * MON,WED,SUN", "0 0 10 ? * THU,SAT"],
        [3021] = ["0 0 21 ? * TUE,WED,SUN", "0 0 10 ? * FRI,SAT"],
        [5011] = ["0 0 21 ? * TUE,WED,SUN"],
        [6011] = ["0 0 21 ? * TUE,WED,SUN"],
        [6021] = ["0 0 21 ? * TUE,WED,SUN"],
    };

    /// <summary>
    /// note: Java routed source-siege cron fires through checkSiegeStart→startPreparations — a Tiamaranta
    /// Eye-specific sequence (reset all 4 sources to Balaur, teleport players out of the instance, wait
    /// 300s, start all 4 source sieges together, wait another 10s, clear the zone and broadcast shield/
    /// state packets) that depends on the world-map-instance + zone/teleport framework. That prep phase
    /// is out of scope for this task, so each source here starts its own siege directly on its own cron
    /// fires instead of being coordinated through the shared 4011-triggers-all-four prep flow.
    /// </summary>
    public Dictionary<int, List<string>> Sources { get; init; } = new()
    {
        [4011] = ["0 55 13 ? * *", "0 55 17 ? * *", "0 55 20 ? * *"],
        [4021] = ["0 55 13 ? * *", "0 55 17 ? * *", "0 55 20 ? * *"],
        [4031] = ["0 55 13 ? * *", "0 55 17 ? * *", "0 55 20 ? * *"],
        [4041] = ["0 55 13 ? * *", "0 55 17 ? * *", "0 55 20 ? * *"],
    };
}
