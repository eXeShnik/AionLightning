// Port of Java data/scripts/system/handlers/quest/theobomos/_3090InSearchOfPippiThePorgus.java
// (Cheatkiller). Accept at 798182; talk 798193 (var0 0->1, and later a 10000-kinah purchase of item
// 182208050 advances var0 2->3). While var0==1 the player must BOTH approach 206085 (onAtDistance,
// sets var1=1) AND talk 700420 (sets var2=1); once both flags are set, changeStep flips var0 1->2.
// Then use 700421 (SET_SUCCEED, var0 3->reward): swaps item 182208050 for 182208051. Turn in at 798182,
// consuming 182208051.
// Skips vs Java (cosmetic, state kept): the two SM_SYSTEM_MESSAGE progress notices (1111006/1111007)
// and the 700421 despawn (scheduleRespawn + onDelete) are dropped with notes.
using System.Threading;
using System.Threading.Tasks;
using AionLightning.Game.Dao;
using AionLightning.Game.DataHolders;
using AionLightning.Game.Model.Quest;
using AionLightning.Game.Network.Aion;
using AionLightning.Game.QuestEngine;
using AionLightning.Game.QuestEngine.Handlers;
using AionLightning.Game.QuestEngine.Model;
using AionLightning.Game.Services;

namespace Quest.Theobomos;

public sealed class _3090InSearchOfPippiThePorgus : QuestHandlerBase
{
    private const int QuestIdConst = 3090;
    private const int StartNpc     = 798182;
    private const int Npc798193    = 798193;
    private const int Npc700420    = 700420;
    private const int Npc700421    = 700421;
    private const int DistanceNpc  = 206085;
    private const int KinahItemId  = 182400001;
    private const int PorgusItemA  = 182208050;
    private const int PorgusItemB  = 182208051;

    private readonly IItemDao _itemDao;

    public _3090InSearchOfPippiThePorgus(IDataManager dataManager, IQuestDao questDao, QuestRewardService rewardService, IItemDao itemDao)
        : base(QuestIdConst, dataManager, questDao, rewardService)
    {
        _itemDao = itemDao;
    }

    public override void Register(QuestEngine engine)
    {
        engine.RegisterQuestNpc(StartNpc).OnQuestStart.Add(QuestId);
        engine.RegisterQuestNpc(StartNpc).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(Npc798193).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(Npc700420).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(Npc700421).OnTalk.Add(QuestId);
        RegisterOnAtDistance(engine, DistanceNpc);
    }

    public override async ValueTask<bool> OnDialogAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var player      = env.Player;
        var entry       = player.Quests.Get(QuestId);
        int targetId    = env.TargetId;
        int targetObjId = env.Target?.ObjectId ?? 0;
        var dialog      = DialogActionLookup.FromId(env.DialogId);

        if (entry is null || entry.Status == QuestStatus.NONE)
        {
            if (targetId == StartNpc)
            {
                if (dialog == DialogAction.QUEST_SELECT)
                    return await SendQuestDialogAsync(conn, targetObjId, 4762, ct);
                return await SendQuestStartDialogAsync(env, conn, ct);
            }
        }
        else if (entry.Status == QuestStatus.START)
        {
            if (targetId == Npc798193)
            {
                if (dialog == DialogAction.QUEST_SELECT)
                {
                    if (entry.GetVar(0) == 0)
                        return await SendQuestDialogAsync(conn, targetObjId, 1011, ct);
                    if (entry.GetVar(0) == 2)
                        return await SendQuestDialogAsync(conn, targetObjId, 1693, ct);
                }
                else if (dialog == DialogAction.SETPRO1)
                    return await DefaultCloseDialogAsync(env, conn, 0, 1, ct);
                else if (dialog == DialogAction.SETPRO3)
                {
                    long kinah = player.Inventory.FindByItemId(KinahItemId)?.Count ?? 0;
                    if (kinah >= 10000)
                    {
                        await GiveQuestItemAsync(player, conn, _itemDao, PorgusItemA, 1, ct);
                        await RemoveQuestItemAsync(player, conn, _itemDao, KinahItemId, 10000, ct);
                        return await DefaultCloseDialogAsync(env, conn, 2, 3, ct);
                    }
                    return await SendQuestDialogAsync(conn, targetObjId, 1779, ct);
                }
                else if (dialog == DialogAction.SELECT_ACTION_1779)
                    return await SendQuestDialogAsync(conn, targetObjId, 1779, ct);
            }
            if (targetId == Npc700420)
            {
                if (entry.GetVar(0) == 1 && entry.GetVar(2) == 0)
                {
                    await ChangeQuestStepAsync(conn, entry, 2, 1, toReward: false, ct);
                    // note: SM_SYSTEM_MESSAGE 1111007 cosmetic progress notice dropped
                    await ChangeStepAsync(entry, conn, ct);
                    return true;
                }
            }
            if (targetId == Npc700421)
            {
                if (dialog == DialogAction.USE_OBJECT)
                {
                    if (entry.GetVar(0) == 3)
                        return await SendQuestDialogAsync(conn, targetObjId, 2034, ct);
                }
                else if (dialog == DialogAction.SET_SUCCEED)
                {
                    // note: Java despawns the object here (scheduleRespawn + onDelete) - cosmetic, dropped.
                    await RemoveQuestItemAsync(player, conn, _itemDao, PorgusItemA, 1, ct);
                    await GiveQuestItemAsync(player, conn, _itemDao, PorgusItemB, 1, ct);
                    return await DefaultCloseDialogAsync(env, conn, 3, 3, reward: true, sameNpc: false, ct);
                }
            }
        }
        else if (entry.Status == QuestStatus.REWARD)
        {
            if (targetId == StartNpc)
            {
                if (dialog == DialogAction.USE_OBJECT)
                    return await SendQuestDialogAsync(conn, targetObjId, 10002, ct);
                await RemoveQuestItemAsync(player, conn, _itemDao, PorgusItemB, 1, ct);
                return await SendQuestEndDialogAsync(env, conn, ct);
            }
        }
        return false;
    }

    public override async ValueTask<bool> OnAtDistanceAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var entry = env.Player.Quests.Get(QuestId);
        if (entry is not null && entry.Status == QuestStatus.START
            && entry.GetVar(0) == 1 && entry.GetVar(1) == 0)
        {
            await ChangeQuestStepAsync(conn, entry, 1, 1, toReward: false, ct);
            // note: SM_SYSTEM_MESSAGE 1111006 cosmetic progress notice dropped
            await ChangeStepAsync(entry, conn, ct);
            return true;
        }
        return false;
    }

    private async ValueTask ChangeStepAsync(QuestEntry entry, GsClientConnection conn, CancellationToken ct)
    {
        if (entry.GetVar(1) == 1 && entry.GetVar(2) == 1)
        {
            entry.SetVar(1, 0);
            entry.SetVar(2, 0);
            entry.SetVar(0, 2);
            await UpdateQuestStatusAsync(conn, entry, ct);
        }
    }
}
