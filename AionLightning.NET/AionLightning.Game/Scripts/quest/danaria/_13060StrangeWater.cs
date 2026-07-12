// Port of Java data/scripts/system/handlers/quest/danaria/_13060StrangeWater.java.
// Starts purely via item use (182213452, targetId 0 accept dialog) - no NPC "quest start" flag in
// Java's register(). Dialog chain: 801096 (removes the starter item on SETPRO1, var0->1) ->
// 801095 (var1->2) -> 801096 again (var2->3) -> turn in at any of 800936/800937/800938 (reward,
// var stays 3 - nextStep==step so no discrepancy with the shared reward-flip helper).
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

namespace Quest.Danaria;

public sealed class _13060StrangeWater : QuestHandlerBase
{
    private const int QuestIdConst = 13060;
    private const int ItemId       = 182213452;
    private const int FirstNpc     = 801096;
    private const int SecondNpc    = 801095;
    private static readonly int[] TurnInNpcs = { 800936, 800937, 800938 };

    private readonly IItemDao _itemDao;

    public _13060StrangeWater(IDataManager dataManager, IQuestDao questDao, QuestRewardService rewardService, IItemDao itemDao)
        : base(QuestIdConst, dataManager, questDao, rewardService)
    {
        _itemDao = itemDao;
    }

    public override void Register(QuestEngine engine)
    {
        engine.RegisterQuestItem(ItemId, QuestId);
        engine.RegisterQuestNpc(FirstNpc).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(SecondNpc).OnTalk.Add(QuestId);
        foreach (int npc in TurnInNpcs)
            engine.RegisterQuestNpc(npc).OnTalk.Add(QuestId);
    }

    public override async ValueTask<bool> OnItemUseAsync(Player player, int itemId, GsClientConnection conn, CancellationToken ct)
    {
        if (itemId != ItemId) return false;
        var entry = player.Quests.Get(QuestId);
        if (entry is null || entry.Status == QuestStatus.NONE)
            await conn.SendAsync(new SM_DIALOG_WINDOW(0, 4, QuestId), ct);
        return true;
    }

    public override async ValueTask<bool> OnDialogAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var player = env.Player;
        var entry  = player.Quests.Get(QuestId);
        int targetId    = env.TargetId;
        int targetObjId = env.Target?.ObjectId ?? 0;

        if (entry is null || entry.Status == QuestStatus.NONE)
        {
            if (targetId == 0 && env.DialogId == (int)DialogAction.QUEST_ACCEPT_1)
            {
                await StartMissionAsync(conn, player, QuestStatus.START, ct);
                return await CloseDialogWindowAsync(conn, 0, ct);
            }
            return false;
        }

        var dialog = DialogActionLookup.FromId(env.DialogId);

        if (entry.Status == QuestStatus.START)
        {
            int var = entry.GetVar(0);
            if (targetId == FirstNpc)
            {
                switch (dialog)
                {
                    case DialogAction.QUEST_SELECT:
                        return var switch
                        {
                            0 => await SendQuestDialogAsync(conn, targetObjId, 1352, ct),
                            2 => await SendQuestDialogAsync(conn, targetObjId, 2034, ct),
                            _ => false
                        };
                    case DialogAction.SETPRO1:
                        await RemoveQuestItemAsync(player, conn, _itemDao, ItemId, 1, ct);
                        return await DefaultCloseDialogAsync(env, conn, 0, 1, ct);
                    case DialogAction.SETPRO3:
                        return await DefaultCloseDialogAsync(env, conn, 2, 3, ct);
                }
                return false;
            }
            if (targetId == SecondNpc)
            {
                if (dialog == DialogAction.QUEST_SELECT)
                    return await SendQuestDialogAsync(conn, targetObjId, 1693, ct);
                if (dialog == DialogAction.SETPRO2)
                    return await DefaultCloseDialogAsync(env, conn, 1, 2, ct);
                return false;
            }
            if (Array.IndexOf(TurnInNpcs, targetId) >= 0)
            {
                if (dialog == DialogAction.QUEST_SELECT)
                    return await SendQuestDialogAsync(conn, targetObjId, 2375, ct);
                if (dialog == DialogAction.SELECT_QUEST_REWARD)
                {
                    entry.Status = QuestStatus.REWARD;
                    await UpdateQuestStatusAsync(conn, entry, ct);
                    return await SendQuestDialogAsync(conn, targetObjId, 5, ct);
                }
                return false;
            }
        }
        else if (entry.Status == QuestStatus.REWARD && Array.IndexOf(TurnInNpcs, targetId) >= 0)
        {
            return await SendQuestEndDialogAsync(env, conn, ct);
        }

        return false;
    }
}
