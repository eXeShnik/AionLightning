using AionLightning.Commons.Services;
using AionLightning.Game.Configs.Options;
using AionLightning.Game.Dao;
using AionLightning.Game.DataHolders;
using AionLightning.Game.Model;
using AionLightning.Game.Model.Quest;
using AionLightning.Game.Model.Templates.Event;
using AionLightning.Game.Network.Aion;
using AionLightning.Game.Network.Aion.ServerPackets;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AionLightning.Game.Services;

/// <summary>
/// Java services.EventService — the seasonal-event scheduler (Lunar Festival, Daeva Day, etc.).
/// <see cref="CheckEvents"/> (armed on a ~5-minute cron by <see cref="EventServiceHostedService"/>, and
/// also run once at startup) starts any event whose [start, end) date window has just opened — spawning
/// its tagged NPC set via <see cref="SpawnService.SpawnEvent"/> — and stops any previously-active event
/// whose window has closed or that was removed from events_config.xml's &lt;active&gt; list — despawning
/// via <see cref="SpawnService.DespawnEvent"/>. <see cref="OnPlayerLoginAsync"/> mirrors Java's
/// onPlayerLogin: auto-starts each currently-active event's "startable" quests for the logging-in player
/// (same level/race gate CM_DIALOG_SELECT applies to a manually-accepted quest) and re-notifies them of
/// any "maintainable" quest they already hold.
/// note: Java's EventTemplate.Start()/Stop() also drove a periodic per-player inventory_drop item grant
/// and a survey/GuideTemplate activation toggle. Neither an ItemService.dropItemToInventory equivalent
/// nor a guide/HTML-survey subsystem exists in this port (see CLAUDE.md — quest engine is basic
/// XML-driven only), so <see cref="Model.Templates.Event.InventoryDrop"/>/<see cref="EventDrop"/> data
/// loads faithfully but is not wired to a runtime effect here.
/// Entirely gated behind <see cref="EventOptions.Enable"/> (default false): data always loads and
/// <see cref="EventTemplate.IsActive"/>/<see cref="CheckQuestIsActive"/> remain callable regardless, but
/// no NPC ever spawns/despawns and no event quest is ever started/maintained until the flag is flipped on
/// a verified data set.
/// </summary>
public sealed class EventService
{
    private readonly IDataManager _dataManager;
    private readonly SpawnService _spawnService;
    private readonly IQuestDao _questDao;
    private readonly CronService _cronService;
    private readonly IOptions<EventOptions> _options;
    private readonly ILogger<EventService> _log;

    private readonly object _lock = new();
    private readonly List<EventTemplate> _activeEvents = new();

    /// <summary>Java EventTemplate.isStarted — relocated here (see this class's doc comment on why the
    /// template itself stays a pure DTO) so repeat <see cref="StartEvent"/>/<see cref="StopEvent"/> calls
    /// for an event still in its window (every <see cref="CheckEvents"/> tick) are cheap no-ops instead of
    /// despawning and re-spawning the same NPC group every cycle.</summary>
    private readonly HashSet<string> _startedEventNames = new(StringComparer.Ordinal);

    public EventService(IDataManager dataManager, SpawnService spawnService, IQuestDao questDao,
        CronService cronService, IOptions<EventOptions> options, ILogger<EventService> log)
    {
        _dataManager  = dataManager;
        _spawnService = spawnService;
        _questDao     = questDao;
        _cronService  = cronService;
        _options      = options;
        _log          = log;
    }

    /// <summary>Java EventService.getActiveEvents() — the events currently considered active by the last
    /// <see cref="CheckEvents"/> pass.</summary>
    public IReadOnlyList<EventTemplate> GetActiveEvents()
    {
        lock (_lock) return _activeEvents.ToList();
    }

    /// <summary>Java EventService.checkQuestIsActive(questId) — true if any currently-active event lists
    /// this quest id as startable or maintainable.</summary>
    public bool CheckQuestIsActive(int questId)
    {
        lock (_lock)
            return _activeEvents.Any(e => e.StartableQuests.Contains(questId) || e.MaintainableQuests.Contains(questId));
    }

    /// <summary>Java EventService.start() — arms the ~5-minute recheck cron. Called once at startup by
    /// <see cref="EventServiceHostedService"/>, after an initial <see cref="CheckEvents"/> pass; a no-op
    /// while <see cref="EventOptions.Enable"/> is false.</summary>
    public async Task ScheduleAsync(CancellationToken ct = default)
    {
        if (!_options.Value.Enable)
        {
            _log.LogInformation("EventService: event engine disabled (GameServer:Event:Enable=false) — cron not armed.");
            return;
        }

        await _cronService.Schedule(CheckEvents, _options.Value.CheckCron, longRunningTask: true);
    }

    /// <summary>
    /// Java EventService.checkEvents() — iterates every configured event template (not just the ones
    /// named in the config's &lt;active&gt; list — Java's own checkEvents() ignores that list for the
    /// start decision, only consulting it via <see cref="EventData.Contains"/> for the stop condition
    /// below), starts any whose date window has just opened, then stops any previously-active event that
    /// has since expired or been pulled from the active-name list. A no-op while
    /// <see cref="EventOptions.Enable"/> is false.
    /// </summary>
    public void CheckEvents()
    {
        if (!_options.Value.Enable) return;

        var allEvents   = _dataManager.SeasonalEvents.GetAllEvents();
        var newlyActive = allEvents.Where(e => e.IsActive()).ToList();

        List<EventTemplate> toStop;
        lock (_lock)
            toStop = _activeEvents.Where(e => e.IsExpired() || !_dataManager.SeasonalEvents.Contains(e.Name)).ToList();

        foreach (var stale in toStop)
            StopEvent(stale);

        foreach (var template in newlyActive)
            StartEvent(template);

        lock (_lock)
        {
            _activeEvents.Clear();
            _activeEvents.AddRange(newlyActive);
        }
    }

    /// <summary>Java EventTemplate.Start() — spawns the event's tagged NPC set via
    /// <see cref="SpawnService.SpawnEvent"/>. Idempotent (matches Java's own isStarted guard): a no-op if
    /// this event is already started, has no spawn data, or <see cref="EventOptions.Enable"/> is false.</summary>
    public void StartEvent(EventTemplate template)
    {
        if (!_options.Value.Enable) return;

        lock (_lock)
        {
            if (!_startedEventNames.Add(template.Name)) return;
        }

        var points = template.GetSpawnPoints().ToList();
        if (points.Count == 0) return;

        var spawned = _spawnService.SpawnEvent(template.Name, points);
        _log.LogInformation("EventService: started event '{Event}' ({Count} NPCs)", template.Name, spawned.Count);
    }

    /// <summary>Java EventTemplate.Stop() — despawns the event's tagged NPC set via
    /// <see cref="SpawnService.DespawnEvent"/>. Idempotent: a no-op if this event was never started or has
    /// already been stopped.</summary>
    public void StopEvent(EventTemplate template)
    {
        lock (_lock)
        {
            if (!_startedEventNames.Remove(template.Name)) return;
        }

        int removed = _spawnService.DespawnEvent(template.Name);
        if (removed > 0)
            _log.LogInformation("EventService: stopped event '{Event}' ({Count} NPCs despawned)", template.Name, removed);
    }

    /// <summary>
    /// Java EventService.onPlayerLogin — for every currently-active event, auto-starts each "startable"
    /// quest the player doesn't already hold (subject to the same level/race gate
    /// CM_DIALOG_SELECT applies to a manually-accepted quest) and re-sends the current status of any
    /// "maintainable" quest they already hold. A no-op while <see cref="EventOptions.Enable"/> is false.
    /// </summary>
    public async ValueTask OnPlayerLoginAsync(Player player, GsClientConnection conn, CancellationToken ct = default)
    {
        if (!_options.Value.Enable) return;

        List<EventTemplate> active;
        lock (_lock) active = _activeEvents.Where(e => e.IsActive()).ToList();
        if (active.Count == 0) return;

        bool anyStarted = false;
        foreach (var template in active)
            foreach (int questId in template.StartableQuests)
                anyStarted |= await TryStartQuestAsync(player, conn, questId, ct);

        foreach (var template in active)
            foreach (int questId in template.MaintainableQuests)
                await MaintainQuestAsync(player, conn, questId, ct);

        if (anyStarted)
            try { await conn.SendAsync(new SM_QUEST_LIST(player.Quests.Active), ct); } catch { }
    }

    private async ValueTask<bool> TryStartQuestAsync(Player player, GsClientConnection conn, int questId, CancellationToken ct)
    {
        if (player.Quests.Contains(questId)) return false;

        var template = _dataManager.Quests.GetTemplate(questId);
        if (template is null) return false;
        if (player.Level < template.MinLevel) return false;
        if (template.Race != "PC_ALL" && !string.Equals(template.Race, player.Race.ToString(), StringComparison.OrdinalIgnoreCase))
            return false;

        var entry = new QuestEntry { QuestId = questId, Status = QuestStatus.START };
        player.Quests.Add(entry);
        await _questDao.UpsertAsync(player.ObjectId, entry, ct);

        try
        {
            await conn.SendAsync(new SM_QUEST_ACTION(entry.QuestId,
                SM_QUEST_ACTION.ActionType.Accept, (byte)entry.Status, entry.Step), ct);
        }
        catch { }

        return true;
    }

    private static async ValueTask MaintainQuestAsync(Player player, GsClientConnection conn, int questId, CancellationToken ct)
    {
        var entry = player.Quests.Get(questId);
        if (entry is null) return; // maintain-only quests are never auto-started, only kept alive

        try
        {
            await conn.SendAsync(new SM_QUEST_ACTION(entry.QuestId,
                SM_QUEST_ACTION.ActionType.StepUpdate, (byte)entry.Status, entry.Step), ct);
        }
        catch { }
    }
}
