namespace AionLightning.Game.Configs.Options;

/// <summary>One &lt;open schedule="..." spawn="..."/&gt; entry from Java's rift_schedule.xml
/// (Java model.templates.rift.OpenRift) — a cron expression that opens every not-yet-open rift in
/// the owning world id, optionally also spawning that rift's static guard/defense NPCs.</summary>
public sealed record RiftOpenSchedule(string Cron, bool SpawnGuards);

/// <summary>
/// Rift open/close cron schedule (Java configs.schedule.RiftSchedule, loaded at runtime from
/// config/schedule/rift_schedule.xml — no XML schedule loader is ported here, see
/// SiegeScheduleOptions for the same precedent). Transcribed verbatim from this repo's
/// AL-Game/config/schedule/rift_schedule.xml, keyed by world id (not rift location id — one cron
/// fire opens every currently-closed rift location in that world at once, matching Java's
/// RiftOpenRunnable(worldId, guards)).
/// </summary>
public sealed record RiftScheduleOptions
{
    public Dictionary<int, List<RiftOpenSchedule>> Worlds { get; init; } = new()
    {
        // Eltnen — every 2 hours, open for 1 hour
        [210020000] =
        [
            new("0 0 13,15,17,19,21,23,3,5,7,9,11 ? * *", false),
        ],
        // Heiron — every 4 hours, open for 1 hour
        [210040000] =
        [
            new("0 0 15,19,23,3,7,11 ? * *", false),
        ],
        // Inggison — Silentera Canyon guard-spawning schedule + plain schedule
        [210050000] =
        [
            new("0 0 1 ? * SUN", true),
            new("0 0 3 ? * SAT", true),
            new("0 0 11 ? * MON,THU", true),
            new("0 0 13 ? * SAT", true),
            new("0 0 16 ? * MON,FRI", true),
            new("0 0 19 ? * SUN", true),
            new("0 0 23 ? * MON,THU", true),
            new("0 0 5,9,15 ? * SUN", false),
            new("0 0 5,21 ? * MON", false),
            new("0 0 2,7,9,15,19 ? * TUE", false),
            new("0 0 2,7,9,15,19 ? * WED", false),
            new("0 0 2,5,19,21 ? * THU", false),
            new("0 0 5,9,15 ? * FRI", false),
            new("0 0 7,16,23 ? * SAT", false),
        ],
        // Morheim — every 2 hours, open for 1 hour
        [220020000] =
        [
            new("0 0 14,16,18,20,22,2,4,6,8,10,12 ? * *", false),
        ],
        // Beluslan — every 4 hours, open for 1 hour
        [220040000] =
        [
            new("0 0 14,18,22,2,6,10 ? * *", false),
        ],
        // Gelkmaros — Silentera Canyon guard-spawning schedule + plain schedule
        [220070000] =
        [
            new("0 0 1 ? * SAT", true),
            new("0 0 3 ? * SUN", true),
            new("0 0 11 ? * TUE,FRI", true),
            new("0 0 13 ? * SUN", true),
            new("0 0 16 ? * TUE,THU", true),
            new("0 0 19 ? * SAT", true),
            new("0 0 23 ? * TUE,FRI", true),
            new("0 0 7,16,23 ? * SUN", false),
            new("0 0 2,7,9,15,19 ? * MON", false),
            new("0 0 5,21 ? * TUE", false),
            new("0 0 5,11,16,21,23 ? * WED", false),
            new("0 0 7,9,15 ? * THU", false),
            new("0 0 2,7,19 ? * FRI", false),
            new("0 0 5,9,15 ? * SAT", false),
        ],
    };
}
