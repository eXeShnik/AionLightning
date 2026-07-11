using AionLightning.Game.Model;
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
}
