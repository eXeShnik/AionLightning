// Port of Java data/scripts/system/handlers/quest/poeta/_1111InsomniaMedicine.java (MrPoke).
// Melanalu (203075) starts it; collect ingredients checked at Tia (203061); pick one of two
// medicine recipes (SETPRO1→vial 182200222/var 2, SETPRO2→sleep aid 182200221/var 3), then
// return to Melanalu and choose the matching reward tier (var 0 - 2 → reward index).
// Uses FinishQuestAsync (Batch 0.1) for the reward-index turn-in.
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AionLightning.Game.Dao;
using AionLightning.Game.DataHolders;
using AionLightning.Game.Model.Quest;
using AionLightning.Game.Network.Aion;
using AionLightning.Game.Network.Aion.ServerPackets;
using AionLightning.Game.QuestEngine;
using AionLightning.Game.QuestEngine.Handlers;
using AionLightning.Game.QuestEngine.Model;
using AionLightning.Game.Services;

namespace Quest.Poeta;

public sealed class _1111InsomniaMedicine : QuestHandlerBase
{
    private const int QuestIdConst = 1111;
    private const int MelanaluNpc  = 203075;
    private const int TiaNpc        = 203061;
    private const int VialItemId   = 182200222;
    private const int SleepAidItem = 182200221;

    private readonly IItemDao _itemDao;

    public _1111InsomniaMedicine(IDataManager dataManager, IQuestDao questDao, QuestRewardService rewardService, IItemDao itemDao)
        : base(QuestIdConst, dataManager, questDao, rewardService)
    {
        _itemDao = itemDao;
    }

    public override void Register(QuestEngine engine)
    {
        engine.RegisterQuestNpc(MelanaluNpc).OnQuestStart.Add(QuestId);
        engine.RegisterQuestNpc(MelanaluNpc).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(TiaNpc).OnTalk.Add(QuestId);
    }

    public override async ValueTask<bool> OnDialogAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var player = env.Player;
        var entry  = player.Quests.Get(QuestId);
        int targetId = env.TargetId;
        int targetObjId = env.Target?.ObjectId ?? 0;
        var dialog = DialogActionLookup.FromId(env.DialogId);

        if (targetId == MelanaluNpc)
        {
            if (entry is null)
            {
                if (dialog == DialogAction.QUEST_SELECT)
                    return await SendQuestDialogAsync(conn, targetObjId, 1011, ct);
                return await SendQuestStartDialogAsync(env, conn, ct);
            }

            if (entry.Status == QuestStatus.REWARD)
            {
                int var = entry.GetVar(0);
                if (dialog == DialogAction.USE_OBJECT)
                {
                    if (var == 2)
                    {
                        await RemoveQuestItemAsync(player, conn, _itemDao, VialItemId, 1, ct);
                        return await SendQuestDialogAsync(conn, targetObjId, 2375, ct);
                    }
                    if (var == 3)
                    {
                        await RemoveQuestItemAsync(player, conn, _itemDao, SleepAidItem, 1, ct);
                        return await SendQuestDialogAsync(conn, targetObjId, 2716, ct);
                    }
                    return false;
                }
                if (env.DialogId == (int)DialogAction.SELECT_QUEST_REWARD)
                    return await SendQuestDialogAsync(conn, targetObjId, var + 3, ct);
                if (env.DialogId == (int)DialogAction.SELECTED_QUEST_NOREWARD)
                {
                    await FinishQuestAsync(conn, player, var - 2, ct);
                    await conn.SendAsync(new SM_DIALOG_WINDOW(targetObjId, 10), ct);
                    return true;
                }
            }
            return false;
        }

        if (targetId == TiaNpc && entry is not null)
        {
            int var = entry.GetVar(0);
            if (dialog == DialogAction.QUEST_SELECT)
            {
                if (var == 0) return await SendQuestDialogAsync(conn, targetObjId, 1352, ct);
                if (var == 1) return await SendQuestDialogAsync(conn, targetObjId, 1353, ct);
                return false;
            }
            if (env.DialogId == (int)DialogAction.CHECK_USER_HAS_QUEST_ITEM)
            {
                return await CheckQuestItemsAsync(env, conn, _itemDao,
                    step: var, nextStep: var + 1, reward: false, checkOkId: 1353, checkFailId: 1693,
                    giveItemId: 0, giveItemCount: 0, ct);
            }
            if (dialog == DialogAction.SETPRO1 && entry.Status != QuestStatus.COMPLETE && entry.Status != QuestStatus.NONE)
            {
                if (!await GiveQuestItemAsync(player, conn, _itemDao, VialItemId, 1, ct)) return true;
                entry.SetVar(0, 2);
                entry.Status = QuestStatus.REWARD;
                await UpdateQuestStatusAsync(conn, entry, ct);
                await conn.SendAsync(new SM_DIALOG_WINDOW(targetObjId, 10), ct);
                return true;
            }
            if (dialog == DialogAction.SETPRO2 && entry.Status != QuestStatus.COMPLETE && entry.Status != QuestStatus.NONE)
            {
                if (!await GiveQuestItemAsync(player, conn, _itemDao, SleepAidItem, 1, ct)) return true;
                entry.SetVar(0, 3);
                entry.Status = QuestStatus.REWARD;
                await UpdateQuestStatusAsync(conn, entry, ct);
                await conn.SendAsync(new SM_DIALOG_WINDOW(targetObjId, 10), ct);
                return true;
            }
        }
        return false;
    }
}
