using AionLightning.Game.Dao;
using AionLightning.Game.DataHolders;
using AionLightning.Game.Model;
using AionLightning.Game.Network.Aion;
using AionLightning.Game.Network.Aion.ServerPackets;

namespace AionLightning.Game.Services;

/// <summary>Handles quest events: NPC kills, progress updates, and SM_QUEST_ACTION broadcasting.</summary>
public sealed class QuestService
{
    private readonly IQuestDao    _questDao;
    private readonly IDataManager _dataManager;

    public QuestService(IQuestDao questDao, IDataManager dataManager)
    {
        _questDao    = questDao;
        _dataManager = dataManager;
    }

    /// <summary>
    /// Called when a player kills an NPC. Increments kill-progress vars for all active quests
    /// that list the NPC in a quest_kill element, then notifies the client via SM_QUEST_ACTION.
    /// </summary>
    public async ValueTask HandleNpcKillAsync(Player player, Npc deadNpc, GsClientConnection conn, CancellationToken ct)
    {
        foreach (var entry in player.Quests.Active)
        {
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

            await _questDao.UpsertAsync(player.ObjectId, entry, ct);
            await conn.SendAsync(new SM_QUEST_ACTION(entry.QuestId,
                SM_QUEST_ACTION.ActionType.StepUpdate, (byte)entry.Status, entry.Step), ct);
        }
    }
}
