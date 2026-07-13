using AionLightning.Game.DataHolders;
using AionLightning.Game.Model;
using AionLightning.Game.Model.Quest;
using AionLightning.Game.Network.Aion;
using AionLightning.Game.QuestEngine.Handlers;
using AionLightning.Game.QuestEngine.Model;
using Microsoft.Extensions.Logging;

namespace AionLightning.Game.QuestEngine;

/// <summary>
/// Singleton npc/item → quest index and dialog/kill/item-get dispatcher (Java
/// <c>questEngine.QuestEngine</c> port). Template and hand-written handlers register themselves
/// here via <see cref="AddQuestHandler"/>; callers (packet handlers, <c>QuestService</c>) try the
/// engine first and fall back to legacy inline logic when a dispatcher returns false/unhandled.
/// </summary>
public sealed class QuestEngine
{
    private readonly Dictionary<int, QuestNpc>      _questNpcs = new();
    private readonly Dictionary<int, IQuestHandler> _handlers  = new();
    private readonly Dictionary<int, List<int>>     _itemGetIndex = new();
    private readonly Dictionary<int, List<int>>     _itemUseIndex = new();
    private readonly Dictionary<int, List<int>>     _skillUseIndex = new();
    private readonly Dictionary<int, List<int>>     _killInWorldIndex = new();
    private readonly List<int>                      _levelUpIndex = new();
    private readonly List<int>                      _zoneMissionEndIndex = new();
    private readonly Dictionary<int, List<int>>     _movieEndIndex = new();
    private readonly List<int>                      _enterWorldIndex = new();
    private readonly List<int>                      _questTimerEndIndex = new();
    private readonly List<int>                      _onDieIndex = new();
    private readonly List<int>                      _onLogOutIndex = new();
    private readonly HashSet<int>                   _atDistanceNpcIds = new();
    private readonly List<int>                      _reachTargetIndex = new();
    private readonly List<int>                      _lostTargetIndex = new();
    private readonly Dictionary<string, List<int>>  _zoneEnterIndex = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, List<int>>  _zoneLeaveIndex = new(StringComparer.OrdinalIgnoreCase);
    // npcId -> side quest-item drops (Java addHandlerSideQuestDrop)
    private readonly Dictionary<int, List<SideQuestDrop>> _sideDropIndex = new();
    private readonly ILogger<QuestEngine>           _log;

    // worldId -> quests startable there (Java parity: WorldMapInstance.questIds, populated
    // dynamically as quest-start NPCs spawn into a map instance). Built once at startup from the
    // static NPC spawn data joined against each NPC's OnQuestStart index; swapped in as a whole
    // dictionary reference so concurrent reads never observe a partially built index.
    private IReadOnlyDictionary<int, List<WorldQuestEntry>> _worldQuests =
        new Dictionary<int, List<WorldQuestEntry>>();

    private readonly record struct WorldQuestEntry(int QuestId, byte MinLevel, Race RacePermitted);

    public QuestEngine(ILogger<QuestEngine> log) => _log = log;

    public int HandlerCount => _handlers.Count;

    /// <summary>Returns true when a data-driven handler owns this quest id, so legacy fallback logic should skip it.</summary>
    public bool HasHandler(int questId) => _handlers.ContainsKey(questId);

    /// <summary>Gets or creates the per-NPC index entry (Java registerQuestNpc).</summary>
    public QuestNpc RegisterQuestNpc(int npcId)
    {
        if (!_questNpcs.TryGetValue(npcId, out var npc))
        {
            npc = new QuestNpc(npcId);
            _questNpcs[npcId] = npc;
        }
        return npc;
    }

    /// <summary>Reads the per-NPC index without registering it (Java getQuestNpc — returns an empty instance if absent).</summary>
    public QuestNpc GetQuestNpc(int npcId) => _questNpcs.GetValueOrDefault(npcId) ?? new QuestNpc(npcId);

    /// <summary>Registers an item id as relevant to a quest's onItemGet event (Java registerGetingItem).</summary>
    public void RegisterItemGet(int itemId, int questId)
    {
        if (!_itemGetIndex.TryGetValue(itemId, out var quests))
        {
            quests = [];
            _itemGetIndex[itemId] = quests;
        }
        if (!quests.Contains(questId)) quests.Add(questId);
    }

    /// <summary>Registers a quest item whose use fires the quest's onItemUse event (Java registerQuestItem).</summary>
    public void RegisterQuestItem(int itemId, int questId)
    {
        if (!_itemUseIndex.TryGetValue(itemId, out var quests))
        {
            quests = [];
            _itemUseIndex[itemId] = quests;
        }
        if (!quests.Contains(questId)) quests.Add(questId);
    }

    /// <summary>Registers a quest for onQuestTimerEnd notifications (Java registerOnQuestTimerEnd).</summary>
    public void RegisterOnQuestTimerEnd(int questId)
    {
        if (!_questTimerEndIndex.Contains(questId)) _questTimerEndIndex.Add(questId);
    }

    /// <summary>Registers a mob-kill quest-item drop (Java addHandlerSideQuestDrop).</summary>
    public void RegisterQuestDrop(int npcId, SideQuestDrop drop)
    {
        if (!_sideDropIndex.TryGetValue(npcId, out var drops))
        {
            drops = [];
            _sideDropIndex[npcId] = drops;
        }
        drops.Add(drop);
    }

    public IReadOnlyList<SideQuestDrop> GetSideDrops(int npcId)
        => _sideDropIndex.TryGetValue(npcId, out var drops) ? drops : System.Array.Empty<SideQuestDrop>();

    /// <summary>Registers a skill id as relevant to a quest's onUseSkill event (Java registerQuestSkill).</summary>
    public void RegisterSkillUse(int skillId, int questId)
    {
        if (!_skillUseIndex.TryGetValue(skillId, out var quests))
        {
            quests = [];
            _skillUseIndex[skillId] = quests;
        }
        if (!quests.Contains(questId)) quests.Add(questId);
    }

    /// <summary>Registers a world id as relevant to a kill_in_world quest (Java registerOnKillInWorld).</summary>
    public void RegisterKillInWorld(int worldId, int questId)
    {
        if (!_killInWorldIndex.TryGetValue(worldId, out var quests))
        {
            quests = [];
            _killInWorldIndex[worldId] = quests;
        }
        if (!quests.Contains(questId)) quests.Add(questId);
    }

    /// <summary>Registers a quest against a named zone region for onEnterZone notifications (Java registerOnEnterZone).</summary>
    public void RegisterOnEnterZone(string zoneName, int questId)
    {
        if (!_zoneEnterIndex.TryGetValue(zoneName, out var quests))
        {
            quests = [];
            _zoneEnterIndex[zoneName] = quests;
        }
        if (!quests.Contains(questId)) quests.Add(questId);
    }

    /// <summary>Registers a quest against a named zone region for onLeaveZone notifications (Java registerOnLeaveZone).</summary>
    public void RegisterOnLeaveZone(string zoneName, int questId)
    {
        if (!_zoneLeaveIndex.TryGetValue(zoneName, out var quests))
        {
            quests = [];
            _zoneLeaveIndex[zoneName] = quests;
        }
        if (!quests.Contains(questId)) quests.Add(questId);
    }

    /// <summary>Registers a quest for level-up re-checks (Java registerOnLevelUp).</summary>
    public void RegisterOnLevelUp(int questId)
    {
        if (!_levelUpIndex.Contains(questId)) _levelUpIndex.Add(questId);
    }

    /// <summary>Registers a quest for zone-mission-end pokes (Java registerOnEnterZoneMissionEnd).</summary>
    public void RegisterOnZoneMissionEnd(int questId)
    {
        if (!_zoneMissionEndIndex.Contains(questId)) _zoneMissionEndIndex.Add(questId);
    }

    /// <summary>Registers a quest for player-death notifications (Java registerOnDie).</summary>
    public void RegisterOnDie(int questId)
    {
        if (!_onDieIndex.Contains(questId)) _onDieIndex.Add(questId);
    }

    /// <summary>
    /// Registers a quest against an NPC id for the at-distance hook (Java
    /// <c>registerQuestNpc(npcId).addOnAtDistanceEvent</c>): the quest is notified when the player
    /// moves near a live NPC of that id. The npc id is also tracked so the move-time scan can skip
    /// work when no at-distance quests are registered.
    /// </summary>
    public void RegisterOnAtDistance(int npcId, int questId)
    {
        var list = RegisterQuestNpc(npcId).OnAtDistance;
        if (!list.Contains(questId)) list.Add(questId);
        _atDistanceNpcIds.Add(npcId);
    }

    /// <summary>NPC ids that at least one quest watches for the at-distance hook (empty ⇒ skip the move scan).</summary>
    public IReadOnlySet<int> AtDistanceNpcIds => _atDistanceNpcIds;

    /// <summary>Registers a quest for the escort reach-target hook (Java registerAddOnReachTargetEvent).</summary>
    public void RegisterOnReachTarget(int questId)
    {
        if (!_reachTargetIndex.Contains(questId)) _reachTargetIndex.Add(questId);
    }

    /// <summary>Registers a quest for the escort lost-target hook (Java registerAddOnLostTargetEvent).</summary>
    public void RegisterOnLostTarget(int questId)
    {
        if (!_lostTargetIndex.Contains(questId)) _lostTargetIndex.Add(questId);
    }

    /// <summary>Registers a quest for player-logout notifications (Java registerOnLogOut).</summary>
    public void RegisterOnLogOut(int questId)
    {
        if (!_onLogOutIndex.Contains(questId)) _onLogOutIndex.Add(questId);
    }

    /// <summary>
    /// Registers a quest for enter-world notifications (Java registerOnEnterWorld). Not one of
    /// this batch's 3 required hooks, but added alongside them because the golden
    /// <c>_1000Prologue</c> exemplar needs it (Java's own onEnterWorldEvent starts the quest and
    /// plays its intro movie the first time an Elyos player logs in) — see migration_plan.md.
    /// </summary>
    public void RegisterOnEnterWorld(int questId)
    {
        if (!_enterWorldIndex.Contains(questId)) _enterWorldIndex.Add(questId);
    }

    /// <summary>Registers a quest as wanting a callback once <paramref name="movieId"/> finishes playing (Java registerOnMovieEndQuest).</summary>
    public void RegisterOnQuestMovieEnd(int movieId, int questId)
    {
        if (!_movieEndIndex.TryGetValue(movieId, out var quests))
        {
            quests = [];
            _movieEndIndex[movieId] = quests;
        }
        if (!quests.Contains(questId)) quests.Add(questId);
    }

    /// <summary>Registers a handler (calling its own <see cref="IQuestHandler.Register"/> first).</summary>
    public void AddQuestHandler(IQuestHandler handler)
    {
        handler.Register(this);
        if (!_handlers.TryAdd(handler.QuestId, handler))
            _log.LogWarning("QuestEngine: duplicate handler registered for quest {QuestId}", handler.QuestId);
    }

    /// <summary>
    /// Dispatches a dialog event. When <see cref="QuestEnv.QuestId"/> is known, looks up that quest's
    /// handler directly; otherwise iterates the target NPC's OnTalk index (Java onDialog).
    /// </summary>
    public async ValueTask<bool> OnDialogAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        try
        {
            if (env.QuestId != 0)
                return _handlers.TryGetValue(env.QuestId, out var handler)
                    && await handler.OnDialogAsync(env, conn, ct);

            foreach (int questId in GetQuestNpc(env.TargetId).OnTalk)
            {
                if (!_handlers.TryGetValue(questId, out var handler)) continue;
                if (await handler.OnDialogAsync(env with { QuestId = questId }, conn, ct))
                    return true;
            }
            return false;
        }
        catch (Exception ex)
        {
            _log.LogError(ex, "QuestEngine: exception in OnDialogAsync (questId={QuestId})", env.QuestId);
            return false;
        }
    }

    /// <summary>
    /// Dispatches an NPC-kill event to every quest registered against this NPC's OnKill index
    /// (Java onKill). Returns true when at least one handler exists for this NPC, so callers can
    /// skip their own fallback processing for the quests the engine already owns.
    /// </summary>
    public async ValueTask<bool> OnKillAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        try
        {
            bool any = false;
            foreach (int questId in GetQuestNpc(env.TargetId).OnKill)
            {
                if (!_handlers.TryGetValue(questId, out var handler)) continue;
                any = true;
                await handler.OnKillAsync(env with { QuestId = questId }, conn, ct);
            }
            return any;
        }
        catch (Exception ex)
        {
            _log.LogError(ex, "QuestEngine: exception in OnKillAsync");
            return false;
        }
    }

    /// <summary>
    /// Dispatches a zone-enter event (Java <c>ZoneInstance.onEnter</c> -&gt;
    /// <c>PlayerController.onEnterZone</c> -&gt; <c>QuestEngine.onEnterZone</c>) to every quest
    /// registered against this zone name via <see cref="RegisterOnEnterZone"/>. Returns true when at
    /// least one handler exists for this zone, mirroring <see cref="OnKillAsync"/>'s "any" contract.
    /// </summary>
    public async ValueTask<bool> OnEnterZoneAsync(Player player, string zoneName, GsClientConnection conn, CancellationToken ct)
    {
        try
        {
            if (!_zoneEnterIndex.TryGetValue(zoneName, out var questIds)) return false;

            bool any = false;
            foreach (int questId in questIds)
            {
                if (!_handlers.TryGetValue(questId, out var handler)) continue;
                any = true;
                await handler.OnEnterZoneAsync(new QuestEnv(null, player, questId, 0), zoneName, conn, ct);
            }
            return any;
        }
        catch (Exception ex)
        {
            _log.LogError(ex, "QuestEngine: exception in OnEnterZoneAsync (zone={ZoneName})", zoneName);
            return false;
        }
    }

    /// <summary>Dispatches onLeaveZone (Java onLeaveZone) to quests registered against a zone the player just left.</summary>
    public async ValueTask OnLeaveZoneAsync(Player player, string zoneName, GsClientConnection conn, CancellationToken ct)
    {
        try
        {
            if (!_zoneLeaveIndex.TryGetValue(zoneName, out var questIds)) return;
            foreach (int questId in questIds)
                if (_handlers.TryGetValue(questId, out var handler))
                    await handler.OnLeaveZoneAsync(new QuestEnv(null, player, questId, 0), zoneName, conn, ct);
        }
        catch (Exception ex)
        {
            _log.LogError(ex, "QuestEngine: exception in OnLeaveZoneAsync (zone={ZoneName})", zoneName);
        }
    }

    /// <summary>Dispatches an NPC-attacked event (Java onAttack) to quests on this NPC's OnAttack index.</summary>
    public async ValueTask<bool> OnAttackAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var list = GetQuestNpc(env.TargetId).OnAttack;
        if (list.Count == 0) return false;
        bool any = false;
        foreach (int questId in list)
        {
            if (!_handlers.TryGetValue(questId, out var handler)) continue;
            any = true;
            try { await handler.OnAttackAsync(env with { QuestId = questId }, conn, ct); }
            catch (Exception ex) { _log.LogError(ex, "QuestEngine: exception in OnAttackAsync (questId={QuestId})", questId); }
        }
        return any;
    }

    /// <summary>Dispatches a quest-timer-expiry event (Java onQuestTimerEnd) to all timer-registered quests.</summary>
    public async ValueTask OnQuestTimerEndAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        foreach (int questId in _questTimerEndIndex)
        {
            if (!_handlers.TryGetValue(questId, out var handler)) continue;
            try { await handler.OnQuestTimerEndAsync(env with { QuestId = questId }, conn, ct); }
            catch (Exception ex) { _log.LogError(ex, "QuestEngine: exception in OnQuestTimerEndAsync (questId={QuestId})", questId); }
        }
    }

    /// <summary>Dispatches an item-acquired event to every quest registered for this item id (Java onItemGet).</summary>
    public async ValueTask<bool> OnItemGetAsync(Player player, int itemId, GsClientConnection conn, CancellationToken ct)
    {
        if (!_itemGetIndex.TryGetValue(itemId, out var questIds)) return false;

        bool any = false;
        foreach (int questId in questIds)
        {
            if (!_handlers.TryGetValue(questId, out var handler)) continue;
            any = true;
            await handler.OnItemGetAsync(player, itemId, conn, ct);
        }
        return any;
    }

    /// <summary>
    /// Dispatches an item-use event (Java onItemUseEvent) to every quest registered for this item id.
    /// Returns true if any registered handler consumed it (used to start/advance quest-item quests).
    /// </summary>
    public async ValueTask<bool> OnItemUseAsync(Player player, int itemId, GsClientConnection conn, CancellationToken ct)
    {
        if (!_itemUseIndex.TryGetValue(itemId, out var questIds)) return false;

        bool handled = false;
        foreach (int questId in questIds)
        {
            if (!_handlers.TryGetValue(questId, out var handler)) continue;
            try { handled |= await handler.OnItemUseAsync(player, itemId, conn, ct); }
            catch (Exception ex) { _log.LogError(ex, "QuestEngine: exception in OnItemUseAsync (questId={QuestId})", questId); }
        }
        return handled;
    }

    /// <summary>
    /// Dispatches a skill-use event (Java <c>onUseSkillEvent</c>, wired from <c>CM_CASTSPELL</c>
    /// once a cast succeeds) to every quest registered against this skill id (Java
    /// <c>registerQuestSkill</c>). Cheap no-op lookup when no quest registers the skill.
    /// </summary>
    public async ValueTask<bool> OnSkillUseAsync(Player player, int skillId, GsClientConnection conn, CancellationToken ct)
    {
        if (!_skillUseIndex.TryGetValue(skillId, out var questIds)) return false;

        bool any = false;
        foreach (int questId in questIds)
        {
            if (!_handlers.TryGetValue(questId, out var handler)) continue;
            any = true;
            await handler.OnSkillUseAsync(player, skillId, conn, ct);
        }
        return any;
    }

    /// <summary>
    /// Dispatches a PvP-kill event (Java <c>PvpService.notifyKillQuests</c> → <c>onKillInWorld</c>,
    /// wired from <c>Combat.Handlers.PvpKillHandler</c> once the AP exchange completes) to every
    /// kill_in_world quest registered against the victim's world (Java
    /// <c>registerOnKillInWorld</c>). Group/alliance member notification is Phase 2 (needs
    /// AggroList) — only the killer's own quest progress advances here.
    /// </summary>
    public async ValueTask<bool> OnPlayerKillAsync(Player killer, Player victim, GsClientConnection killerConn, CancellationToken ct)
    {
        if (!_killInWorldIndex.TryGetValue(victim.Position.WorldId, out var questIds)) return false;

        bool any = false;
        var env = new QuestEnv(victim, killer, 0, 0);
        foreach (int questId in questIds)
        {
            if (!_handlers.TryGetValue(questId, out var handler)) continue;
            any = true;
            await handler.OnPlayerKillAsync(env with { QuestId = questId }, killerConn, ct);
        }
        return any;
    }

    /// <summary>
    /// Dispatches a level-up event (Java <c>onLvlUp</c>, wired from
    /// <c>ExperienceService.HandleLevelUpAsync</c>) to every quest registered via
    /// <see cref="RegisterOnLevelUp"/> whose state isn't already COMPLETE — lets a handler
    /// re-check its mission preconditions (see <c>QuestHandlerBase.DefaultOnLvlUpEventAsync</c>)
    /// and start or (re)lock itself.
    /// </summary>
    public async ValueTask OnLevelUpAsync(Player player, GsClientConnection conn, CancellationToken ct)
    {
        try
        {
            foreach (int questId in _levelUpIndex)
            {
                var existing = player.Quests.Get(questId);
                if (existing is { Status: QuestStatus.COMPLETE }) continue;
                if (!_handlers.TryGetValue(questId, out var handler)) continue;

                await handler.OnLevelUpAsync(new QuestEnv(null, player, questId, 0), conn, ct);
            }
        }
        catch (Exception ex)
        {
            _log.LogError(ex, "QuestEngine: exception in OnLevelUpAsync");
        }
    }

    /// <summary>
    /// Fires the escort reach-target hook (Java <c>onNpcReachTargetEvent</c>) for the escort's own
    /// quest (carried in <paramref name="env"/>.QuestId), if that quest registered for it.
    /// </summary>
    public async ValueTask OnNpcReachTargetAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        try
        {
            if (!_reachTargetIndex.Contains(env.QuestId)) return;
            if (_handlers.TryGetValue(env.QuestId, out var handler))
                await handler.OnNpcReachTargetAsync(env, conn, ct);
        }
        catch (Exception ex) { _log.LogError(ex, "QuestEngine: exception in OnNpcReachTargetAsync (questId={QuestId})", env.QuestId); }
    }

    /// <summary>Fires the escort lost-target hook (Java <c>onNpcLostTargetEvent</c>) for the escort's own quest.</summary>
    public async ValueTask OnNpcLostTargetAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        try
        {
            if (!_lostTargetIndex.Contains(env.QuestId)) return;
            if (_handlers.TryGetValue(env.QuestId, out var handler))
                await handler.OnNpcLostTargetAsync(env, conn, ct);
        }
        catch (Exception ex) { _log.LogError(ex, "QuestEngine: exception in OnNpcLostTargetAsync (questId={QuestId})", env.QuestId); }
    }

    /// <summary>
    /// Dispatches the at-distance hook (Java <c>onAtDistanceEvent</c>) to every quest registered
    /// against this NPC's at-distance index. The handler self-guards on its own quest var, so firing
    /// repeatedly while the player lingers near the NPC is idempotent.
    /// </summary>
    public async ValueTask OnAtDistanceAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        try
        {
            foreach (int questId in GetQuestNpc(env.TargetId).OnAtDistance)
            {
                if (!_handlers.TryGetValue(questId, out var handler)) continue;
                await handler.OnAtDistanceAsync(env with { QuestId = questId }, conn, ct);
            }
        }
        catch (Exception ex)
        {
            _log.LogError(ex, "QuestEngine: exception in OnAtDistanceAsync");
        }
    }

    /// <summary>
    /// Dispatches the player-death hook (Java <c>onDieEvent</c>) to every quest that registered via
    /// <see cref="RegisterOnDie"/> and has an active (non-COMPLETE) entry for the dying player — lets
    /// a handler reset/fail a quest step, revert a disguise, etc. on death.
    /// </summary>
    public async ValueTask OnDieAsync(Player player, GsClientConnection conn, CancellationToken ct)
    {
        try
        {
            foreach (int questId in _onDieIndex)
            {
                var existing = player.Quests.Get(questId);
                if (existing is null or { Status: QuestStatus.COMPLETE }) continue;
                if (!_handlers.TryGetValue(questId, out var handler)) continue;

                await handler.OnDieAsync(new QuestEnv(null, player, questId, 0), conn, ct);
            }
        }
        catch (Exception ex)
        {
            _log.LogError(ex, "QuestEngine: exception in OnDieAsync");
        }
    }

    /// <summary>
    /// Dispatches the player-logout hook (Java <c>onLogOutEvent</c>) to every quest that registered
    /// via <see cref="RegisterOnLogOut"/> and has an active entry — lets a handler abandon/revert a
    /// timed or disguise quest when the player logs out. Fired during connection teardown, so there
    /// is no live connection to send packets on (conn may be null).
    /// </summary>
    public async ValueTask OnLogOutAsync(Player player, GsClientConnection? conn, CancellationToken ct)
    {
        try
        {
            foreach (int questId in _onLogOutIndex)
            {
                var existing = player.Quests.Get(questId);
                if (existing is null or { Status: QuestStatus.COMPLETE }) continue;
                if (!_handlers.TryGetValue(questId, out var handler)) continue;

                await handler.OnLogOutAsync(new QuestEnv(null, player, questId, 0), conn, ct);
            }
        }
        catch (Exception ex)
        {
            _log.LogError(ex, "QuestEngine: exception in OnLogOutAsync");
        }
    }

    /// <summary>
    /// Dispatches a zone-mission-end poke (Java <c>onEnterZoneMissionEnd</c>) for a single
    /// dependent quest id, supplied by the caller via <paramref name="env"/>.QuestId. Unlike the
    /// other dispatchers there is no generic "any quest completed" broadcast in Java — the
    /// trigger is always an explicit per-quest call made by the zone mission's own completing
    /// dialog handler (e.g. Java <c>_1100KaliosCall.onDialogEvent</c> loops a hardcoded array of
    /// dependent quest ids and calls <c>QuestEngine.onEnterZoneMissionEnd</c> once per id right
    /// before finishing itself). Golden/hand-written scripts that represent a zone mission should
    /// call this once per dependent quest id from their own <c>OnDialogAsync</c> at that same
    /// point, mirroring that pattern; see migration_plan.md for the confirmed trigger analysis.
    /// </summary>
    public async ValueTask<bool> OnZoneMissionEndAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        try
        {
            if (!_zoneMissionEndIndex.Contains(env.QuestId)) return false;
            if (!_handlers.TryGetValue(env.QuestId, out var handler)) return false;

            return await handler.OnZoneMissionEndAsync(env, conn, ct);
        }
        catch (Exception ex)
        {
            _log.LogError(ex, "QuestEngine: exception in OnZoneMissionEndAsync (questId={QuestId})", env.QuestId);
            return false;
        }
    }

    /// <summary>
    /// Dispatches a movie-finished event (Java <c>onMovieEnd</c>, wired from
    /// <c>CM_PLAY_MOVIE_END</c>) to every quest registered against this movie id via
    /// <see cref="RegisterOnQuestMovieEnd"/>, stopping at the first handler that reports it
    /// handled the event (Java parity: <c>onMovieEnd</c> returns as soon as one handler's
    /// <c>onMovieEndEvent</c> is true).
    /// </summary>
    public async ValueTask<bool> OnMovieEndAsync(Player player, int movieId, GsClientConnection conn, CancellationToken ct)
    {
        try
        {
            if (!_movieEndIndex.TryGetValue(movieId, out var questIds)) return false;

            foreach (int questId in questIds)
            {
                if (!_handlers.TryGetValue(questId, out var handler)) continue;
                if (await handler.OnMovieEndAsync(new QuestEnv(null, player, questId, 0), movieId, conn, ct))
                    return true;
            }
            return false;
        }
        catch (Exception ex)
        {
            _log.LogError(ex, "QuestEngine: exception in OnMovieEndAsync (movieId={MovieId})", movieId);
            return false;
        }
    }

    /// <summary>
    /// Dispatches the enter-world event (Java <c>onEnterWorld</c>) to every quest registered via
    /// <see cref="RegisterOnEnterWorld"/>, unconditionally (Java parity — unlike level-up, there is
    /// no COMPLETE-status pre-filter here; each handler decides for itself).
    /// </summary>
    public async ValueTask OnEnterWorldAsync(Player player, GsClientConnection conn, CancellationToken ct)
    {
        try
        {
            foreach (int questId in _enterWorldIndex)
            {
                if (!_handlers.TryGetValue(questId, out var handler)) continue;
                await handler.OnEnterWorldAsync(new QuestEnv(null, player, questId, 0), conn, ct);
            }
        }
        catch (Exception ex)
        {
            _log.LogError(ex, "QuestEngine: exception in OnEnterWorldAsync");
        }
    }

    /// <summary>
    /// Builds the worldId -> nearby-quest index (Java parity target: <c>WorldMapInstance.getQuestIds()</c>).
    /// Must run once at startup, after every quest handler has registered its NPCs via
    /// <see cref="RegisterQuestNpc"/> (see <c>QuestEngineHostedService</c>) — this joins the static
    /// spawn table's worldId -> npcId mapping against each NPC's <see cref="QuestNpc.OnQuestStart"/> list.
    /// </summary>
    public void BuildWorldQuestIndex(SpawnsData spawns, QuestData questData)
    {
        var npcIdsByWorld = new Dictionary<int, HashSet<int>>();
        foreach (var (worldId, entry) in spawns.All())
        {
            if (!npcIdsByWorld.TryGetValue(worldId, out var npcIds))
                npcIdsByWorld[worldId] = npcIds = new HashSet<int>();
            npcIds.Add(entry.NpcId);
        }

        var worldQuests = new Dictionary<int, List<WorldQuestEntry>>();
        int totalPairs = 0;

        foreach (var (worldId, npcIds) in npcIdsByWorld)
        {
            List<WorldQuestEntry>? entries = null;
            HashSet<int>? seenQuests = null;

            foreach (int npcId in npcIds)
            {
                if (!_questNpcs.TryGetValue(npcId, out var questNpc) || questNpc.OnQuestStart.Count == 0)
                    continue;

                foreach (int questId in questNpc.OnQuestStart)
                {
                    seenQuests ??= new HashSet<int>();
                    if (!seenQuests.Add(questId)) continue;

                    var template = questData.GetTemplate(questId);
                    if (template is null) continue;

                    var race = Enum.TryParse<Race>(template.Race, out var parsed) ? parsed : Race.PC_ALL;
                    (entries ??= new List<WorldQuestEntry>()).Add(new WorldQuestEntry(questId, template.MinLevel, race));
                }
            }

            if (entries is { Count: > 0 })
            {
                worldQuests[worldId] = entries;
                totalPairs += entries.Count;
            }
        }

        _worldQuests = worldQuests;
        _log.LogInformation("QuestEngine: built nearby-quest index for {Worlds} world(s) x {Pairs} world-quest pair(s)",
            worldQuests.Count, totalPairs);
    }

    /// <summary>
    /// Computes the quests to show as "nearby" for <paramref name="player"/> at their current world
    /// (Java parity: <c>PlayerController.updateNearbyQuests</c>). Filters by race, level (diff &lt;= 2,
    /// minlevel_permitted=99 meaning "no level requirement"), and excludes quests the player already
    /// has active (START/REWARD) or has completed and cannot repeat (approximated as CompleteCount &gt; 0,
    /// since the ported <see cref="Model.Templates.Quest.QuestTemplate"/> does not yet expose a
    /// repeatable-count field — see migration_plan.md for this deviation).
    /// </summary>
    public IReadOnlyList<(int QuestId, int LevelDiff)> ComputeNearbyQuests(Player player)
    {
        if (!_worldQuests.TryGetValue(player.Position.WorldId, out var entries) || entries.Count == 0)
            return [];

        var result = new List<(int, int)>();
        foreach (var entry in entries)
        {
            if (entry.RacePermitted != Race.PC_ALL && entry.RacePermitted != player.Race)
                continue;

            // Java quirk, ported exactly: ids above 0xFFFF can't fit SM_NEARBY_QUESTS' 16-bit "future
            // quest" field, so the level-requirement check (and thus the grey-icon branch) is skipped.
            int diff = entry.QuestId <= 0xFFFF
                ? (entry.MinLevel == 99 ? 0 : entry.MinLevel - player.Level)
                : 0;
            if (diff > 2) continue;

            var existing = player.Quests.Get(entry.QuestId);
            if (existing is not null)
            {
                if (existing.Status is QuestStatus.START or QuestStatus.REWARD) continue;
                if (existing.Status == QuestStatus.COMPLETE && existing.CompleteCount > 0) continue;
            }

            result.Add((entry.QuestId, diff));
        }
        return result;
    }
}

/// <summary>A quest-item drop registered against a mob kill (Java addHandlerSideQuestDrop).</summary>
public sealed record SideQuestDrop(int QuestId, int ItemId, int Amount, int Chance, int Step);
