// Port of Java data/scripts/system/handlers/quest/heiron/_1559WhatsintheBox.java.
// Starts from an item interaction (no NPC target, QUEST_ACCEPT_1) which also hands over item
// 182201823 via 700513; progress through 204571 (var1->2) and 798013 (var2->3, gives item
// 182201824, flips to REWARD); turn in at 798072.
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

namespace Quest.Heiron;

public sealed class _1559WhatsintheBox : QuestHandlerBase
{
    private const int QuestIdConst = 1559;
    private const int BoxNpc      = 700513;
    private const int TurnInNpc   = 798072;
    private const int MidNpc      = 204571;
    private const int FinalNpc    = 798013;
    private const int ContainerItem = 182201823;
    private const int RewardItem    = 182201824;

    private readonly IItemDao _itemDao;

    public _1559WhatsintheBox(IDataManager dataManager, IQuestDao questDao, QuestRewardService rewardService, IItemDao itemDao)
        : base(QuestIdConst, dataManager, questDao, rewardService)
    {
        _itemDao = itemDao;
    }

    public override void Register(QuestEngine engine)
    {
        engine.RegisterQuestNpc(BoxNpc).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(TurnInNpc).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(MidNpc).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(FinalNpc).OnTalk.Add(QuestId);
    }

    public override async ValueTask<bool> OnDialogAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var player = env.Player;
        int targetId = env.TargetId;
        int targetObjId = env.Target?.ObjectId ?? 0;
        var entry  = player.Quests.Get(QuestId);
        var dialog = DialogActionLookup.FromId(env.DialogId);

        if (targetId == 0)
        {
            if (dialog == DialogAction.QUEST_ACCEPT_1)
            {
                await StartMissionAsync(conn, player, QuestStatus.START, ct);
                await conn.SendAsync(new SM_DIALOG_WINDOW(0, 0), ct);
                return true;
            }
        }
        else if (targetId == BoxNpc)
        {
            if (entry is null || entry.Status == QuestStatus.NONE)
            {
                if (dialog == DialogAction.USE_OBJECT && player.Inventory.FindByItemId(ContainerItem) is null or { Count: 0 })
                    return await GiveQuestItemAsync(player, conn, _itemDao, ContainerItem, 1, ct);
            }
        }

        if (entry is null) return false;

        int var = entry.GetVar(0);
        if (entry.Status == QuestStatus.REWARD)
        {
            if (targetId == TurnInNpc)
            {
                if (dialog == DialogAction.USE_OBJECT)
                    return await SendQuestDialogAsync(conn, targetObjId, 2375, ct);
                if (dialog == DialogAction.SELECT_QUEST_REWARD)
                    return await SendQuestDialogAsync(conn, targetObjId, 5, ct);
                return await SendQuestEndDialogAsync(env, conn, ct);
            }
            return false;
        }
        if (entry.Status != QuestStatus.START) return false;

        if (targetId == TurnInNpc)
        {
            if (dialog == DialogAction.QUEST_SELECT && var == 0)
                return await SendQuestDialogAsync(conn, targetObjId, 1352, ct);
            if (dialog == DialogAction.SETPRO1 && var == 0)
                return await DefaultCloseDialogAsync(env, conn, 0, 1, ct);
        }
        else if (targetId == MidNpc)
        {
            if (dialog == DialogAction.QUEST_SELECT && var == 1)
                return await SendQuestDialogAsync(conn, targetObjId, 1693, ct);
            if (dialog == DialogAction.SETPRO2 && var == 1)
                return await DefaultCloseDialogAsync(env, conn, 1, 2, ct);
        }
        else if (targetId == FinalNpc)
        {
            if (dialog == DialogAction.QUEST_SELECT && var == 2)
                return await SendQuestDialogAsync(conn, targetObjId, 2034, ct);
            if (dialog == DialogAction.SETPRO3 && var == 2)
            {
                entry.SetVar(0, 3);
                entry.Status = QuestStatus.REWARD;
                await UpdateQuestStatusAsync(conn, entry, ct);
                await GiveQuestItemAsync(player, conn, _itemDao, RewardItem, 1, ct);
                await conn.SendAsync(new SM_DIALOG_WINDOW(targetObjId, 10), ct);
                return true;
            }
        }
        return false;
    }
}
