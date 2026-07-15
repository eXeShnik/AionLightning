using AionLightning.Game.Dao;
using AionLightning.Game.Model;
using AionLightning.Game.Network.Aion.ServerPackets;
using Microsoft.Extensions.Logging;

namespace AionLightning.Game.Services;

/// <summary>
/// Port of Java <c>services.AnnouncementService</c> — loads the <c>announcements</c> table at boot and,
/// for each row, broadcasts its text to every matching online player (faction filter: ALL/ELYOS/ASMODIANS)
/// on a fixed repeating interval (<see cref="Announcement.DelaySeconds"/>). Java scheduled one
/// <c>ThreadPoolManager.scheduleAtFixedRate(delay, delay)</c> task per row; this port's
/// <see cref="AnnouncementServiceHostedService"/> spawns one <see cref="PeriodicTimer"/> loop per row
/// instead, which is the direct .NET analogue (first fire after <see cref="Announcement.DelaySeconds"/>,
/// repeating every <see cref="Announcement.DelaySeconds"/> — no immediate fire on boot, matching Java).
/// </summary>
public sealed class AnnouncementService(
    IAnnouncementDao dao,
    PlayerConnectionRegistry connRegistry,
    ILogger<AnnouncementService> log)
{
    /// <summary>Java hardcoded <c>new SM_MESSAGE(1, ...)</c> — a fixed "system" sender id, not a real player.</summary>
    private const int SystemSenderObjectId = 1;

    public async Task<List<Announcement>> LoadAsync(CancellationToken ct = default)
    {
        var announcements = await dao.LoadAllAsync(ct);
        log.LogInformation("Loaded {Count} announcements", announcements.Count);
        return announcements;
    }

    /// <summary>Java <c>AnnouncementService.load()</c>'s inner <c>Runnable</c> — sends one announcement to
    /// every online player whose race matches its faction filter (null <see cref="Announcement.FactionRace"/>
    /// means ALL, matching everyone).</summary>
    public async Task BroadcastAsync(Announcement announcement, CancellationToken ct = default)
    {
        var chatType = MapChatType(announcement.ChatType);
        var factionRace = announcement.FactionRace;

        // Java prefixes the text with "<Faction> Announcement: " for non-shout/non-orange chat types (the
        // header itself is only used as the packet's unread senderName for shout/orange, which the client
        // renders separately from the message body).
        bool isPrefixed = chatType != SM_MESSAGE.ChatType.Shout && chatType != SM_MESSAGE.ChatType.GroupLeader;
        string header = factionRace switch
        {
            Race.ELYOS => "Elyos Announcement",
            Race.ASMODIANS => "Asmodian Announcement",
            _ => "Announcement",
        };
        string text = isPrefixed ? $"{header}: {announcement.Text}" : announcement.Text;
        var packet = new SM_MESSAGE(SystemSenderObjectId, header, text, chatType);

        foreach (var conn in connRegistry.GetAll())
        {
            var player = conn.ActivePlayer;
            if (player is null) continue;
            if (factionRace is { } race && player.Race != race) continue;

            try { await conn.SendAsync(packet, ct); }
            catch { /* best-effort broadcast — a dropped connection shouldn't abort the sweep for everyone else */ }
        }
    }

    /// <summary>
    /// Java's <see cref="Model.Announcement"/>.getChatType() mapped SHOUT/ORANGE to their own colored chat
    /// types and everything else (SYSTEM/YELLOW/WHITE) to distinct golden/white/yellow "center" colors that
    /// have no equivalent in the ported <see cref="SM_MESSAGE.ChatType"/> enum (no colored-text chat types
    /// exist yet — see that enum). SHOUT and ORANGE (→ GroupLeader, matching Java's check of
    /// <c>chatType == SHOUT || chatType == GROUP_LEADER</c>) map directly; the remaining three all fall
    /// back to <see cref="SM_MESSAGE.ChatType.Command"/>, the same system-wide broadcast channel
    /// <see cref="WeddingService"/> already uses for its marriage announcement.
    /// </summary>
    private static SM_MESSAGE.ChatType MapChatType(string rawChatType) => rawChatType.ToUpperInvariant() switch
    {
        "SHOUT" => SM_MESSAGE.ChatType.Shout,
        "ORANGE" => SM_MESSAGE.ChatType.GroupLeader,
        _ => SM_MESSAGE.ChatType.Command,
    };
}
