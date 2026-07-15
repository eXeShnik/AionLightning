namespace AionLightning.Game.Model;

/// <summary>
/// Port of Java <c>model.Announcement</c> — one row from the <c>announcements</c> table: a piece of text
/// broadcast on a repeating timer (<see cref="DelaySeconds"/>) to online players, optionally restricted to
/// one faction. See <c>Services/AnnouncementService.cs</c> for the broadcast loop and chat-type mapping.
/// </summary>
public sealed class Announcement(int id, string text, string faction, string chatType, int delaySeconds)
{
    public int Id { get; } = id;
    public string Text { get; } = text;

    /// <summary>Raw DB value: <c>ALL</c>, <c>ELYOS</c>, or <c>ASMODIANS</c>.</summary>
    public string Faction { get; } = faction;

    /// <summary>Raw DB value: <c>SHOUT</c>, <c>ORANGE</c>, <c>YELLOW</c>, <c>WHITE</c>, or <c>SYSTEM</c>.</summary>
    public string ChatType { get; } = chatType;

    public int DelaySeconds { get; } = delaySeconds;

    /// <summary>Java <c>Announcement.getFactionEnum()</c> — null means "ALL" (every online player, regardless
    /// of race, receives the broadcast).</summary>
    public Race? FactionRace => Faction.ToUpperInvariant() switch
    {
        "ELYOS" => Race.ELYOS,
        "ASMODIANS" => Race.ASMODIANS,
        _ => null,
    };
}
