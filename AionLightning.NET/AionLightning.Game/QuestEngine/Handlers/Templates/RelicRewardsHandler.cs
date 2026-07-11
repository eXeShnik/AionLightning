using AionLightning.Game.Dao;
using AionLightning.Game.DataHolders;
using AionLightning.Game.Model;
using AionLightning.Game.Model.Item;
using AionLightning.Game.Model.Quest;
using AionLightning.Game.Model.Templates.Quest.Script;
using AionLightning.Game.Network.Aion;
using AionLightning.Game.Network.Aion.ServerPackets;
using AionLightning.Game.QuestEngine.Model;
using AionLightning.Game.Services;

namespace AionLightning.Game.QuestEngine.Handlers.Templates;

/// <summary>
/// Data-driven "turn in one of 4 relic item types for an AP reward" exchange handler (Java
/// <c>questEngine.handlers.template.RelicRewards</c> port) — covers &lt;relic_rewards&gt; entries
/// (30 on disk). Each entry's quest_data.xml template declares 4 sibling &lt;rewards
/// reward_abyss_point="..."/&gt; blocks, one per relic type/tier.
/// </summary>
/// <remarks>
/// Java lets the player pick which relic type to submit via 4 distinct dialog actions
/// (<c>SELECT_ACTION_1011/1352/1693/2034</c>) shown on a selection page. None of those ids are
/// routed from <c>CM_DIALOG_SELECT</c> to the quest engine (only <c>QUEST_SELECT</c>/
/// <c>QUEST_ACCEPT(_1)</c>/<c>SELECT_QUEST_REWARD</c> are — the same routed subset every other
/// template handler in this engine already relies on), so this port collapses relic selection into
/// one <c>SELECT_QUEST_REWARD</c> click that auto-picks the first relic type the player holds
/// enough of — a documented behavioral simplification, not a 1:1 dialog-id port. Var 0 stores which
/// relic slot (1-4) was submitted; on the follow-up claim click, <c>rewardIndex = var - 1</c>
/// selects the matching <c>&lt;rewards&gt;</c> tier via <see cref="QuestRewardService.GrantAndCompleteAsync"/>
/// (mirrors Java's <c>QuestService.finishQuest(env, qs.getQuestVars().getQuestVars() - 1)</c>).
/// Also not ported: quest-repeat (<c>max_repeat_count="255"</c>, Java's <c>canRepeat()</c>) — the
/// same pre-existing gap already noted for every template handler in this engine (see
/// <c>QuestEngine.ComputeNearbyQuests</c> and <c>WorkOrdersHandler</c> remarks); a completed relic
/// exchange can't currently be re-accepted through this engine.
/// </remarks>
public sealed class RelicRewardsHandler : QuestHandlerBase
{
    private readonly HashSet<int> _startNpcs;
    private readonly int[]        _relicItemIds; // index 0..3 <=> relic_var1..4
    private readonly int          _relicCount;
    private readonly IItemDao     _itemDao;

    public RelicRewardsHandler(RelicRewardsScriptEntry data, IDataManager dataManager,
        IQuestDao questDao, QuestRewardService rewardService, IItemDao itemDao)
        : base(data.Id, dataManager, questDao, rewardService)
    {
        _startNpcs    = data.StartNpcIds;
        _relicItemIds = [data.RelicVar1, data.RelicVar2, data.RelicVar3, data.RelicVar4];
        _relicCount   = data.RelicCount > 0 ? data.RelicCount : 1;
        _itemDao      = itemDao;
    }

    public override void Register(QuestEngine engine)
    {
        foreach (int npcId in _startNpcs)
        {
            var npc = engine.RegisterQuestNpc(npcId);
            npc.OnQuestStart.Add(QuestId);
            npc.OnTalk.Add(QuestId);
        }
    }

    public override async ValueTask<bool> OnDialogAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var template = Template;
        if (template is null) return false;

        var player      = env.Player;
        int targetId     = env.TargetId;
        int targetObjId  = env.Target?.ObjectId ?? 0;
        if (!_startNpcs.Contains(targetId)) return false;

        var entry  = player.Quests.Get(QuestId);
        var status = entry?.Status ?? QuestStatus.NONE;
        var dialog = DialogActionLookup.FromId(env.DialogId);

        switch (status)
        {
            case QuestStatus.NONE:
                if (player.Level < template.MinLevel) return false;

                return dialog switch
                {
                    DialogAction.QUEST_SELECT => await SendQuestDialogAsync(conn, targetObjId,
                        HasAnyRelic(player) ? 1011 : 3398, ct),
                    DialogAction.QUEST_ACCEPT or DialogAction.QUEST_ACCEPT_1
                        or DialogAction.QUEST_REFUSE or DialogAction.QUEST_REFUSE_1
                        or DialogAction.QUEST_REFUSE_2 or DialogAction.QUEST_REFUSE_SIMPLE
                        => await SendQuestStartDialogAsync(env, conn, ct),
                    _ => false,
                };

            case QuestStatus.START:
                return dialog switch
                {
                    DialogAction.QUEST_SELECT        => await SendQuestDialogAsync(conn, targetObjId, 1011, ct),
                    DialogAction.SELECT_QUEST_REWARD => await TrySelectRelicAsync(env, conn, ct),
                    _ => false,
                };

            case QuestStatus.REWARD:
            {
                int relicSlot = entry!.GetVar(0); // 1-4, set by TrySelectRelicAsync
                return dialog switch
                {
                    DialogAction.QUEST_SELECT        => await SendQuestDialogAsync(conn, targetObjId, 4 + relicSlot, ct),
                    DialogAction.SELECT_QUEST_REWARD => await FinishAsync(env, conn, entry, ct),
                    _ => false,
                };
            }

            default:
                return false;
        }
    }

    private bool HasAnyRelic(Player player) => _relicItemIds.Any(id => id != 0 && CountItem(player, id) > 0);

    private static long CountItem(Player player, int itemId)
        => player.Inventory.All.Where(i => i.ItemId == itemId && !i.IsEquipped).Sum(i => i.Count);

    /// <summary>
    /// Picks the first relic type the player holds at least <see cref="_relicCount"/> of, consumes
    /// it, and transitions to REWARD, recording which slot (1-4) was chosen in var 0.
    /// </summary>
    private async ValueTask<bool> TrySelectRelicAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var player      = env.Player;
        var entry       = player.Quests.Get(QuestId)!;
        int targetObjId = env.Target?.ObjectId ?? 0;

        for (int i = 0; i < _relicItemIds.Length; i++)
        {
            int itemId = _relicItemIds[i];
            if (itemId == 0 || CountItem(player, itemId) < _relicCount) continue;

            await RemoveItemsAsync(player, itemId, _relicCount, conn, ct);

            entry.SetVar(0, i + 1);
            entry.Status        = QuestStatus.REWARD;
            entry.CompleteCount = 0;
            await UpdateQuestStatusAsync(conn, entry, ct);
            return await SendQuestDialogAsync(conn, targetObjId, 5 + i, ct);
        }

        return await SendQuestDialogAsync(conn, targetObjId, 1009, ct);
    }

    /// <summary>Grants the reward tier matching the submitted relic slot (Java: <c>reward = var - 1</c>) and completes the quest.</summary>
    private async ValueTask<bool> FinishAsync(QuestEnv env, GsClientConnection conn, QuestEntry entry, CancellationToken ct)
    {
        var player      = env.Player;
        int targetObjId = env.Target?.ObjectId ?? 0;
        var template    = Template!;

        int rewardIndex = entry.GetVar(0) - 1;
        if (!await RewardService.GrantAndCompleteAsync(conn, player, entry, template, rewardIndex, ct))
            return false;

        return await SendQuestDialogAsync(conn, targetObjId, 10, ct);
    }

    private async ValueTask RemoveItemsAsync(Player player, int itemId, long count, GsClientConnection conn, CancellationToken ct)
    {
        long remaining = count;
        var removedIds = new List<long>();
        var updated    = new List<Item>();

        foreach (var item in player.Inventory.All.Where(i => i.ItemId == itemId && !i.IsEquipped).ToList())
        {
            if (remaining <= 0) break;
            long take = Math.Min(remaining, item.Count);
            item.Count -= take;
            remaining  -= take;

            if (item.Count <= 0)
            {
                player.Inventory.Remove(item.UniqueId);
                await _itemDao.DeleteAsync(item.UniqueId, ct);
                removedIds.Add(item.UniqueId);
            }
            else
            {
                updated.Add(item);
            }
        }

        await _itemDao.SaveAllAsync(player.ObjectId, player.Inventory.All, ct);
        foreach (long uid in removedIds) await conn.SendAsync(new SM_DELETE_ITEM(uid), ct);
        if (updated.Count > 0) await conn.SendAsync(new SM_INVENTORY_ADD_ITEM(updated), ct);
    }
}
