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
