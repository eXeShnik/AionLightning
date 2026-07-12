// Port of Java data/scripts/system/handlers/quest/heiron/_1055EternalRest.java.
// Talk to the Priest (204629) to start; progress through the Freidan Priest (204625), then collect
// 4 flavor items from 4 side NPCs (204628/204627/204626/204622) before combining them via 204625's
// collect-check into item 182201613; use the final object (700270) to finish var3->4, then 204625
// flips to REWARD. Mission-chain quest (no NPC quest-offer dialog), gated on 1500.
// Skip vs Java: the "instakill" flavor animation (creature.getController().onAttack(player, maxHp+1,
// true) on the targeted side NPC after handing over its item) isn't ported — no NPC-death-trigger
// controller infra exists yet (same documented limitation as UseQuestObjectAsync's dieObject param).
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

public sealed class _1055EternalRest : QuestHandlerBase
{
    private const int QuestIdConst = 1055;
    private const int PriestNpc  = 204629;
    private const int FreidanNpc = 204625;
    private const int SideNpcOne   = 204628; private const int SideItemOne   = 182201609;
    private const int SideNpcTwo   = 204627; private const int SideItemTwo   = 182201610;
    private const int SideNpcThree = 204626; private const int SideItemThree = 182201611;
    private const int SideNpcFour  = 204622; private const int SideItemFour  = 182201612;
    private const int FinalObjectNpc = 700270;
    private const int CombinedItem   = 182201613;

    private readonly IItemDao _itemDao;

    public _1055EternalRest(IDataManager dataManager, IQuestDao questDao, QuestRewardService rewardService, IItemDao itemDao)
        : base(QuestIdConst, dataManager, questDao, rewardService)
    {
        _itemDao = itemDao;
    }

    public override void Register(QuestEngine engine)
    {
        engine.RegisterOnZoneMissionEnd(QuestId);
        engine.RegisterOnLevelUp(QuestId);
        engine.RegisterQuestNpc(PriestNpc).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(FreidanNpc).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(SideNpcOne).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(SideNpcTwo).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(SideNpcThree).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(SideNpcFour).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(FinalObjectNpc).OnTalk.Add(QuestId);
    }

    public override ValueTask<bool> OnZoneMissionEndAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
        => DefaultOnZoneMissionEndEventAsync(env, conn, ct);

    public override ValueTask<bool> OnLevelUpAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
        => DefaultOnLvlUpEventAsync(env, conn, 1500, isZoneMission: true, ct);

    public override async ValueTask<bool> OnDialogAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var player = env.Player;
        var entry  = player.Quests.Get(QuestId);
        if (entry is null) return false;

        int targetId = env.TargetId;
        int targetObjId = env.Target?.ObjectId ?? 0;
        var dialog = DialogActionLookup.FromId(env.DialogId);

        if (entry.Status == QuestStatus.REWARD)
        {
            if (targetId == PriestNpc) return await SendQuestEndDialogAsync(env, conn, ct);
            return false;
        }
        if (entry.Status != QuestStatus.START) return false;

        int var = entry.GetVar(0);

        if (targetId == PriestNpc)
        {
            if (dialog == DialogAction.QUEST_SELECT)
            {
                if (var == 0) return await SendQuestDialogAsync(conn, targetObjId, 1011, ct);
                if (var == 4) return await SendQuestDialogAsync(conn, targetObjId, 1693, ct);
                return false;
            }
            if (dialog == DialogAction.SETPRO1 && var == 0)
                return await DefaultCloseDialogAsync(env, conn, 0, 1, ct);
            if (dialog == DialogAction.SETPRO2 && var == 1)
                return await DefaultCloseDialogAsync(env, conn, 1, 2, ct);
            return false;
        }

        if (targetId == FreidanNpc)
        {
            if (dialog == DialogAction.QUEST_SELECT)
            {
                if (var == 1) return await SendQuestDialogAsync(conn, targetObjId, 1352, ct);
                if (var == 2) return await SendQuestDialogAsync(conn, targetObjId, 1693, ct);
                if (var == 4) return await SendQuestDialogAsync(conn, targetObjId, 2375, ct);
                return false;
            }
            if (dialog == DialogAction.CHECK_USER_HAS_QUEST_ITEM && var == 2)
                return await CheckQuestItemsAsync(env, conn, _itemDao, 2, 3, false, 10000, 10001, CombinedItem, 1, ct);
            if (dialog == DialogAction.SETPRO2 && var == 1)
                return await DefaultCloseDialogAsync(env, conn, 1, 2, ct);
            if (dialog == DialogAction.SET_SUCCEED && var == 4)
            {
                entry.Status = QuestStatus.REWARD;
                await UpdateQuestStatusAsync(conn, entry, ct);
                await conn.SendAsync(new SM_DIALOG_WINDOW(targetObjId, 10), ct);
                return true;
            }
            return false;
        }

        if (var == 2)
        {
            if (targetId == SideNpcOne)
                return await HandleSideGhostAsync(env, conn, targetObjId, dialog, SideItemOne, 1694, ct);
            if (targetId == SideNpcTwo)
                return await HandleSideGhostAsync(env, conn, targetObjId, dialog, SideItemTwo, 1781, ct);
            if (targetId == SideNpcThree)
                return await HandleSideGhostAsync(env, conn, targetObjId, dialog, SideItemThree, 1864, ct);
            if (targetId == SideNpcFour)
                return await HandleSideGhostAsync(env, conn, targetObjId, dialog, SideItemFour, 1949, ct);
        }

        if (targetId == FinalObjectNpc && dialog == DialogAction.USE_OBJECT)
            return await UseQuestObjectAsync(env, conn, 3, 4, false, 0, 0, 0, CombinedItem, 1, 0, false, _itemDao, ct);

        return false;
    }

    private async ValueTask<bool> HandleSideGhostAsync(QuestEnv env, GsClientConnection conn, int targetObjId,
        DialogAction dialog, int flavorItemId, int questSelectDialogId, CancellationToken ct)
    {
        var player = env.Player;
        bool alreadyHasItem = player.Inventory.FindByItemId(flavorItemId) is { Count: > 0 };
        if (alreadyHasItem)
            return await CloseDialogWindowAsync(conn, targetObjId, ct);

        if (dialog == DialogAction.QUEST_SELECT)
            return await SendQuestDialogAsync(conn, targetObjId, questSelectDialogId, ct);

        if (dialog == DialogAction.SETPRO3)
        {
            if (!await GiveQuestItemAsync(player, conn, _itemDao, flavorItemId, 1, ct)) return true;
            await conn.SendAsync(new SM_DIALOG_WINDOW(targetObjId, 10), ct);
            return true;
        }
        return false;
    }
}
