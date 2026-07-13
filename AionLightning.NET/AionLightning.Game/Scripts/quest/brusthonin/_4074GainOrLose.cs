// Port of Java data/scripts/system/handlers/quest/brusthonin/_4074GainOrLose.java.
// Repeatable gambling exchange at Bonarunerk (205181): spending a Demon's Eye (186000038) plus a
// kinah stake (1000/5000/25000) grants a random count of item 186000010 (1 / 1-3 / 1-6 respectively).
// Java bug: the chosen reward tier was held in a per-handler-instance field (`reward`) instead of
// per-player quest state -- since one handler instance is shared by every player running this
// quest, concurrent players could clobber each other's in-progress choice. Ported using the
// per-player QuestEntry var 1 instead, same fix as _1993AnotherBeginning's `choice`/`item` fields.
// Java bug: qs.canRepeat() (max_repeat_count) isn't ported -- approximated as "no active entry",
// the same simplification used throughout this port.
using System;
using System.Threading;
using System.Threading.Tasks;
using AionLightning.Game.Dao;
using AionLightning.Game.DataHolders;
using AionLightning.Game.Model;
using AionLightning.Game.Model.Quest;
using AionLightning.Game.Network.Aion;
using AionLightning.Game.Network.Aion.ServerPackets;
using AionLightning.Game.QuestEngine;
using AionLightning.Game.QuestEngine.Handlers;
using AionLightning.Game.QuestEngine.Model;
using AionLightning.Game.Services;

namespace Quest.Brusthonin;

public sealed class _4074GainOrLose : QuestHandlerBase
{
    private const int QuestIdConst   = 4074;
    private const int BonarunerkNpc  = 205181;
    private const int KinahItemId    = 182400001;
    private const int DemonsEyeItem  = 186000038;
    private const int RewardItemId   = 186000010;

    private readonly IItemDao _itemDao;

    public _4074GainOrLose(IDataManager dataManager, IQuestDao questDao, QuestRewardService rewardService, IItemDao itemDao)
        : base(QuestIdConst, dataManager, questDao, rewardService)
    {
        _itemDao = itemDao;
    }

    public override void Register(QuestEngine engine)
    {
        var npc = engine.RegisterQuestNpc(BonarunerkNpc);
        npc.OnQuestStart.Add(QuestId);
        npc.OnTalk.Add(QuestId);
    }

    public override async ValueTask<bool> OnDialogAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var player      = env.Player;
        int targetId    = env.TargetId;
        int targetObjId = env.Target?.ObjectId ?? 0;
        var entry       = player.Quests.Get(QuestId);
        var dialog      = DialogActionLookup.FromId(env.DialogId);

        if (targetId != BonarunerkNpc) return false;

        if (entry is null || entry.Status == QuestStatus.NONE)
        {
            if (dialog == DialogAction.EXCHANGE_COIN)
            {
                if (await StartMissionAsync(conn, player, QuestStatus.START, ct))
                    return await SendQuestDialogAsync(conn, targetObjId, 1011, ct);
                return await SendQuestSelectionDialogAsync(conn, targetObjId, ct);
            }
            return false;
        }

        if (entry.Status == QuestStatus.START)
        {
            long kinahAmount = player.Inventory.FindByItemId(KinahItemId)?.Count ?? 0;
            long demonsEye   = player.Inventory.FindByItemId(DemonsEyeItem)?.Count ?? 0;

            switch (dialog)
            {
                case DialogAction.EXCHANGE_COIN:
                    return await SendQuestDialogAsync(conn, targetObjId, 1011, ct);
                case DialogAction.SELECT_ACTION_1011:
                    if (kinahAmount >= 1000 && demonsEye >= 1)
                    {
                        entry.SetVar(1, 0);
                        await ChangeQuestStepAsync(conn, entry, 0, 0, toReward: true, ct);
                        return await SendQuestDialogAsync(conn, targetObjId, 5, ct);
                    }
                    return await SendQuestDialogAsync(conn, targetObjId, 1009, ct);
                case DialogAction.SELECT_ACTION_1352:
                    if (kinahAmount >= 5000 && demonsEye >= 1)
                    {
                        entry.SetVar(1, 1);
                        await ChangeQuestStepAsync(conn, entry, 0, 0, toReward: true, ct);
                        return await SendQuestDialogAsync(conn, targetObjId, 6, ct);
                    }
                    return await SendQuestDialogAsync(conn, targetObjId, 1009, ct);
                case DialogAction.SELECT_ACTION_1693:
                    if (kinahAmount >= 25000 && demonsEye >= 1)
                    {
                        entry.SetVar(1, 2);
                        await ChangeQuestStepAsync(conn, entry, 0, 0, toReward: true, ct);
                        return await SendQuestDialogAsync(conn, targetObjId, 7, ct);
                    }
                    return await SendQuestDialogAsync(conn, targetObjId, 1009, ct);
                case DialogAction.FINISH_DIALOG:
                    return await SendQuestSelectionDialogAsync(conn, targetObjId, ct);
                default:
                    return false;
            }
        }

        if (entry.Status == QuestStatus.REWARD)
        {
            if (dialog != DialogAction.SELECTED_QUEST_NOREWARD)
            {
                // Java QuestService.abandonQuest(player, questId).
                player.Quests.Remove(QuestId);
                await QuestDao.DeleteAsync(player.ObjectId, QuestId, ct);
                await conn.SendAsync(new SM_QUEST_ACTION(QuestId), ct);
                await conn.SendAsync(new SM_QUEST_LIST(player.Quests.Active), ct);
                return false;
            }

            int tier = entry.GetVar(1);
            switch (tier)
            {
                case 0:
                    if (await FinishQuestAsync(conn, player, 0, ct))
                        await GrantRewardAsync(player, conn, kinahCost: 1000, rewardCount: 1, ct);
                    break;
                case 1:
                    if (await FinishQuestAsync(conn, player, 1, ct))
                        await GrantRewardAsync(player, conn, kinahCost: 5000, rewardCount: Random.Shared.Next(1, 4), ct);
                    break;
                case 2:
                    if (await FinishQuestAsync(conn, player, 2, ct))
                        await GrantRewardAsync(player, conn, kinahCost: 25000, rewardCount: Random.Shared.Next(1, 7), ct);
                    break;
            }
            return await CloseDialogWindowAsync(conn, targetObjId, ct);
        }

        return false;
    }

    private async ValueTask GrantRewardAsync(Player player, GsClientConnection conn, long kinahCost, int rewardCount, CancellationToken ct)
    {
        await RemoveQuestItemAsync(player, conn, _itemDao, KinahItemId, kinahCost, ct);
        await RemoveQuestItemAsync(player, conn, _itemDao, DemonsEyeItem, 1, ct);
        long existing = player.Inventory.FindByItemId(RewardItemId)?.Count ?? 0;
        await GiveQuestItemAsync(player, conn, _itemDao, RewardItemId, existing + rewardCount, ct);
    }
}
