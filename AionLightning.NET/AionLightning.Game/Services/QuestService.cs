using AionLightning.Game.Dao;
using AionLightning.Game.DataHolders;
using AionLightning.Game.Model;
using AionLightning.Game.Model.Quest;
using AionLightning.Game.Model.Templates.Quest;
using AionLightning.Game.Network.Aion;
using AionLightning.Game.Network.Aion.ServerPackets;
using AionLightning.Game.QuestEngine.Model;
using QuestEngineType = AionLightning.Game.QuestEngine.QuestEngine;

namespace AionLightning.Game.Services;

/// <summary>Handles quest events: NPC kills, progress updates, and SM_QUEST_ACTION broadcasting.</summary>
public sealed class QuestService
{
    private readonly IQuestDao                _questDao;
    private readonly IDataManager             _dataManager;
    private readonly PlayerConnectionRegistry _connRegistry;
    private readonly QuestEngineType          _questEngine;

    public QuestService(IQuestDao questDao, IDataManager dataManager, PlayerConnectionRegistry connRegistry, QuestEngineType questEngine)
    {
        _questDao     = questDao;
        _dataManager  = dataManager;
        _connRegistry = connRegistry;
        _questEngine  = questEngine;
    }

    /// <summary>
    /// Called when a player kills an NPC. Awards kill credit to the killer and to all group
    /// members online in the same zone, matching Java's group-kill-share behaviour.
    /// </summary>
    public async ValueTask HandleNpcKillAsync(Player player, Npc deadNpc, GsClientConnection conn, CancellationToken ct)
    {
        await ProcessKillForPlayerAsync(player, deadNpc, conn, ct);

        if (player.Group is null) return;

        int npcWorldId = deadNpc.Position.WorldId;
        foreach (var member in player.Group.Members)
        {
            if (member.ObjectId == player.ObjectId) continue;
            if (member.Position.WorldId != npcWorldId) continue;

            var memberConn = _connRegistry.Get(member.ObjectId);
            if (memberConn?.ActivePlayer is null) continue;

            await ProcessKillForPlayerAsync(member, deadNpc, memberConn, ct);
        }
    }

    private async ValueTask ProcessKillForPlayerAsync(Player player, Npc deadNpc, GsClientConnection conn, CancellationToken ct)
    {
        // Data-driven templates (registered against this NPC's OnKill index) get first crack;
        // the legacy loop below then skips any quest id the engine already owns.
        await _questEngine.OnKillAsync(new QuestEnv(deadNpc, player, 0, 0), conn, ct);

        foreach (var entry in player.Quests.Active)
        {
            if (entry.Status != QuestStatus.START) continue;
            if (_questEngine.HasHandler(entry.QuestId)) continue;

            var template = _dataManager.Quests.GetTemplate(entry.QuestId);
            if (template is null || template.QuestKills.Count == 0) continue;

            bool updated = false;
            foreach (var kill in template.QuestKills)
            {
                if (!kill.NpcIds.Contains(deadNpc.Template.NpcId)) continue;
                int cur = entry.GetVar(kill.Seq);
                if (cur >= kill.Count) continue;

                entry.SetVar(kill.Seq, cur + 1);
                updated = true;
            }

            if (!updated) continue;

            if (IsRewardReady(entry, template, player))
                entry.Status = QuestStatus.REWARD;

            await _questDao.UpsertAsync(player.ObjectId, entry, ct);
            try
            {
                await conn.SendAsync(new SM_QUEST_ACTION(entry.QuestId,
                    SM_QUEST_ACTION.ActionType.StepUpdate, (byte)entry.Status, entry.Step), ct);
                if (entry.Status == QuestStatus.REWARD)
                    await conn.SendAsync(new SM_QUEST_LIST(player.Quests.Active), ct);
            }
            catch { }
        }
    }

    /// <summary>
    /// Called when a player acquires an item (loot, buy, craft, etc.).
    /// Checks all active START-state quests with collect requirements and transitions to REWARD
    /// if all objectives are now satisfied.
    /// </summary>
    public async ValueTask HandleItemAcquiredAsync(Player player, int itemId, GsClientConnection conn, CancellationToken ct)
    {
        await _questEngine.OnItemGetAsync(player, itemId, conn, ct);

        foreach (var entry in player.Quests.Active)
        {
            if (entry.Status != QuestStatus.START) continue;
            if (_questEngine.HasHandler(entry.QuestId)) continue;

            var template = _dataManager.Quests.GetTemplate(entry.QuestId);
            if (template?.CollectItems is null or { Items.Count: 0 }) continue;

            // Only process if this item is relevant to the quest
            if (!template.CollectItems.Items.Any(r => r.ItemId == itemId)) continue;

            if (!IsRewardReady(entry, template, player)) continue;

            entry.Status = QuestStatus.REWARD;
            await _questDao.UpsertAsync(player.ObjectId, entry, ct);
            await conn.SendAsync(new SM_QUEST_ACTION(entry.QuestId,
                SM_QUEST_ACTION.ActionType.StepUpdate, (byte)entry.Status, entry.Step), ct);
            await conn.SendAsync(new SM_QUEST_LIST(player.Quests.Active), ct);
        }
    }

    /// <summary>
    /// Returns true when all kill slots and all collect_item requirements are met. Internal so the
    /// quest engine's data-driven handlers (e.g. ItemCollectingHandler) can reuse the same check
    /// instead of duplicating it.
    /// </summary>
    internal static bool IsRewardReady(QuestEntry entry, QuestTemplate template, Player player)
    {
        foreach (var kill in template.QuestKills)
            if (entry.GetVar(kill.Seq) < kill.Count) return false;

        if (template.CollectItems is { Items.Count: > 0 })
            foreach (var req in template.CollectItems.Items)
                if ((player.Inventory.FindByItemId(req.ItemId)?.Count ?? 0) < req.Count) return false;

        return true;
    }
}
