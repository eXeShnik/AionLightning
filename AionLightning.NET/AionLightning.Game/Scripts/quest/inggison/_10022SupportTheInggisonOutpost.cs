// Port of Java data/scripts/system/handlers/quest/inggison/_10022SupportTheInggisonOutpost.java.
// Secundila (798932, var0->1, later var11->reward) -> Iaetia (798996, var1->2; var0/1/2 converging
// to 3/10/10 plays movie 503 and advances var0->4; var10->11) -> kill 5 Basrasa mob ids to fill
// var1 (workers, 0->10) and var2 (vaegir/sentry, 0->10), whichever finishes last bumps var0 2->3 ->
// Diana (203786, collect-item check var4->5; var7->8; var8->9) -> Maloren (204656, var5->6) ->
// Jamanok (798176, var6->7) -> use the Inggison Drana object (700601) four times (var3 tracked
// separately, 0->4) at var9, the last use sets var0->10 -> Secundila reports back for the reward.
// Teleport-to-zone calls at Diana/Maloren/Jamanok's SETPRO steps (TeleportService2, not ported) are
// dropped — only the var transition survives, same simplification used across this port (see
// ishalgen._2002WheresRae). Java's own var3==4 branch on the Inggison Drana object updates the
// quest state but falls through to `return false` afterwards (no explicit `return true`) — ported
// faithfully since the state update packet is already sent regardless of the return value.
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

namespace Quest.Inggison;

public sealed class _10022SupportTheInggisonOutpost : QuestHandlerBase
{
    private const int QuestIdConst = 10022;
    private const int Secundila    = 798932;
    private const int Iaetia       = 798996;
    private const int Diana        = 203786;
    private const int Maloren      = 204656;
    private const int Jamanok      = 798176;
    private const int Outremus     = 798926;
    private const int InggisonDrana = 700601;
    private const int PrecedingQuestId = 10020;

    private const int WorkerMob1 = 215622;
    private const int WorkerMob2 = 216784;
    private const int VaegirMob1 = 215633;
    private const int VaegirMob2 = 216731;
    private const int SentryMob  = 215634;

    private readonly IItemDao _itemDao;

    public _10022SupportTheInggisonOutpost(IDataManager dataManager, IQuestDao questDao, QuestRewardService rewardService, IItemDao itemDao)
        : base(QuestIdConst, dataManager, questDao, rewardService)
    {
        _itemDao = itemDao;
    }

    public override void Register(QuestEngine engine)
    {
        engine.RegisterOnZoneMissionEnd(QuestId);
        engine.RegisterOnLevelUp(QuestId);
        int[] npcs = [Secundila, Iaetia, Diana, Maloren, Jamanok, Outremus, InggisonDrana];
        foreach (int npc in npcs)
            engine.RegisterQuestNpc(npc).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(WorkerMob1).OnKill.Add(QuestId);
        engine.RegisterQuestNpc(WorkerMob2).OnKill.Add(QuestId);
        engine.RegisterQuestNpc(VaegirMob1).OnKill.Add(QuestId);
        engine.RegisterQuestNpc(VaegirMob2).OnKill.Add(QuestId);
        engine.RegisterQuestNpc(SentryMob).OnKill.Add(QuestId);
    }

    public override ValueTask<bool> OnZoneMissionEndAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
        => DefaultOnZoneMissionEndEventAsync(env, conn, ct);

    public override ValueTask<bool> OnLevelUpAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
        => DefaultOnLvlUpEventAsync(env, conn, precedingQuestId: PrecedingQuestId, isZoneMission: true, ct);

    public override async ValueTask<bool> OnDialogAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var player = env.Player;
        var entry  = player.Quests.Get(QuestId);
        if (entry is null) return false;

        int var         = entry.GetVar(0);
        int var1        = entry.GetVar(1);
        int var2        = entry.GetVar(2);
        int var3        = entry.GetVar(3);
        int targetId    = env.TargetId;
        int targetObjId = env.Target?.ObjectId ?? 0;
        var dialog      = DialogActionLookup.FromId(env.DialogId);

        if (entry.Status == QuestStatus.REWARD)
        {
            if (targetId != Outremus) return false;
            if (dialog == DialogAction.USE_OBJECT)
                return await SendQuestDialogAsync(conn, targetObjId, 10002, ct);
            if (env.DialogId == (int)DialogAction.SELECT_QUEST_REWARD)
                return await SendQuestDialogAsync(conn, targetObjId, 5, ct);
            return await SendQuestEndDialogAsync(env, conn, ct);
        }
        if (entry.Status != QuestStatus.START) return false;

        if (targetId == Secundila)
        {
            if (dialog == DialogAction.QUEST_SELECT && var == 0)
                return await SendQuestDialogAsync(conn, targetObjId, 1011, ct);
            if (dialog == DialogAction.QUEST_SELECT && var == 11)
                return await SendQuestDialogAsync(conn, targetObjId, 1608, ct);
            if (dialog == DialogAction.QUEST_SELECT || dialog == DialogAction.SETPRO1)
                return await DefaultCloseDialogAsync(env, conn, 0, 1, ct);
            if (dialog == DialogAction.SET_SUCCEED)
                return await DefaultCloseDialogAsync(env, conn, 11, 11, reward: true, sameNpc: false, ct);
            return false;
        }

        if (targetId == Iaetia)
        {
            bool converged = var1 == 10 || var2 == 10 || var == 3;
            if (dialog == DialogAction.QUEST_SELECT && var == 1)
                return await SendQuestDialogAsync(conn, targetObjId, 1352, ct);
            if (dialog == DialogAction.QUEST_SELECT && converged)
            {
                await PlayQuestMovieAsync(conn, player, 503, ct);
                return await SendQuestDialogAsync(conn, targetObjId, 2034, ct);
            }
            if (dialog == DialogAction.QUEST_SELECT && var == 10)
                return await SendQuestDialogAsync(conn, targetObjId, 4080, ct);
            if (dialog == DialogAction.QUEST_SELECT || dialog == DialogAction.SETPRO2)
                return await DefaultCloseDialogAsync(env, conn, 1, 2, ct);
            if (dialog == DialogAction.SETPRO4 && converged)
            {
                entry.SetVar(0, 4);
                await UpdateQuestStatusAsync(conn, entry, ct);
                return await SendQuestSelectionDialogAsync(conn, targetObjId, ct);
            }
            if (dialog == DialogAction.SETPRO10 && var == 10)
                return await DefaultCloseDialogAsync(env, conn, 10, 11, ct);
            return false;
        }

        if (targetId == Diana)
        {
            if (dialog == DialogAction.QUEST_SELECT && var == 4)
                return await SendQuestDialogAsync(conn, targetObjId, 2375, ct);
            if (dialog == DialogAction.QUEST_SELECT && var == 7)
                return await SendQuestDialogAsync(conn, targetObjId, 3398, ct);
            if (dialog == DialogAction.QUEST_SELECT && var == 8)
                return await SendQuestDialogAsync(conn, targetObjId, 3739, ct);
            if (dialog == DialogAction.CHECK_USER_HAS_QUEST_ITEM)
                return await CheckQuestItemsAsync(env, conn, _itemDao, 4, 5, false, 10, 10001, ct);
            if (dialog == DialogAction.SETPRO8 && var == 7)
                return await DefaultCloseDialogAsync(env, conn, 7, 8, ct);
            if (dialog == DialogAction.SETPRO9 && var == 8)
                // Teleport to Hanarkand dropped (TeleportService2 not ported) — see file header.
                return await DefaultCloseDialogAsync(env, conn, 8, 9, ct);
            return false;
        }

        if (targetId == Maloren)
        {
            if (dialog == DialogAction.QUEST_SELECT && var == 5)
                return await SendQuestDialogAsync(conn, targetObjId, 2716, ct);
            if ((dialog == DialogAction.QUEST_SELECT || dialog == DialogAction.SETPRO6) && var == 5)
                return await DefaultCloseDialogAsync(env, conn, 5, 6, ct);
            return false;
        }

        if (targetId == Jamanok)
        {
            if (dialog == DialogAction.QUEST_SELECT && var == 6)
                return await SendQuestDialogAsync(conn, targetObjId, 3057, ct);
            if ((dialog == DialogAction.QUEST_SELECT || dialog == DialogAction.SETPRO7) && var == 6)
                return await DefaultCloseDialogAsync(env, conn, 6, 7, ct);
            return false;
        }

        if (targetId == InggisonDrana)
        {
            if (var == 9 && dialog == DialogAction.USE_OBJECT)
            {
                if (var3 < 4)
                    return await UseQuestObjectAsync(env, conn, var3, var3 + 1, false, 3, ct);
                if (var3 == 4)
                {
                    entry.SetVar(0, 10);
                    await UpdateQuestStatusAsync(conn, entry, ct);
                }
            }
            return false;
        }

        return false;
    }

    public override async ValueTask<bool> OnKillAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var entry = env.Player.Quests.Get(QuestId);
        if (entry is null || entry.Status != QuestStatus.START) return false;

        int targetId = env.TargetId;
        int var0 = entry.GetVar(0);
        int var1 = entry.GetVar(1);
        int var2 = entry.GetVar(2);

        if (targetId == WorkerMob1 || targetId == WorkerMob2)
        {
            if (var1 < 10 && var0 == 2)
            {
                await ChangeQuestStepAsync(conn, entry, 1, var1 + 1, toReward: false, ct);
                return true;
            }
            if (var1 == 9 && var2 == 10 && var0 == 2)
            {
                await ChangeQuestStepAsync(conn, entry, 0, var0 + 1, toReward: false, ct);
                return true;
            }
            return false;
        }

        if (targetId == VaegirMob1 || targetId == VaegirMob2 || targetId == SentryMob)
        {
            if (var2 < 10 && var0 == 2)
            {
                await ChangeQuestStepAsync(conn, entry, 2, var2 + 1, toReward: false, ct);
                return true;
            }
            if (var1 == 10 && var2 == 9 && var0 == 2)
            {
                await ChangeQuestStepAsync(conn, entry, 0, var0 + 1, toReward: false, ct);
                return true;
            }
            return false;
        }
        return false;
    }
}
