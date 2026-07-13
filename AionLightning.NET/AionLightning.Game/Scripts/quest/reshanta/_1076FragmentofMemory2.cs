// Port of Java data/scripts/system/handlers/quest/reshanta/_1076FragmentofMemory2.java (Rhys2002, apozema).
// Zone-mission chain (level-up gated on 1701): Yuditio (278500, var0 0->1), Nestor (203834; movie 102
// on SELECT_ACTION_1353, var0 1->2 on SETPRO2, var0 3->4 on SETPRO4 which enters instance world
// 310070000 + plays movie 170, var0 5->6 on SETPRO6 consuming item 182202006), Diana (203786,
// collect-item check var0 2->3 that also grants 182202006), Aithra (203754, var0 6->6 flip to REWARD),
// then turn in at Boreas (203704). Movie 170 end advances var0 4->5.
// Instance entry: Nestor's SETPRO4 uses the getNextAvailableInstance/registerPlayerWithInstance/
// teleportTo triad -> EnterInstanceAsync (new capability).
// Java bug fixed: Nestor's onDialog switch had no break after the QUEST_SELECT case, so talking with
// var0 not in {1,3,5} fell through into the SELECT_ACTION_1353 body and spuriously played movie 102
// (returning false). Ported with explicit dialog guards so movie 102 only fires on an actual
// SELECT_ACTION_1353 dialog. Diana's and the remaining fall-throughs were internally var-guarded
// (harmless) but are likewise gated explicitly for clarity, matching sibling precedent (_1077, _24071).
// Skip vs Java: the two TeleportService2 relocations to Sanctum (110010000) — Yuditio's SETPRO1 and the
// movie-170 end — are cosmetic fixed-location hops, not instance entries; dropped with the var/status
// transitions kept intact (same precedent as _1077FragmentofMemory3).
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

namespace Quest.Reshanta;

public sealed class _1076FragmentofMemory2 : QuestHandlerBase
{
    private const int QuestIdConst = 1076;
    private const int YuditioNpc   = 278500;
    private const int NestorNpc    = 203834;
    private const int DianaNpc     = 203786;
    private const int AithraNpc    = 203754;
    private const int BoreasNpc    = 203704;
    private const int FragmentItem = 182202006;

    private readonly IItemDao _itemDao;

    public _1076FragmentofMemory2(IDataManager dataManager, IQuestDao questDao, QuestRewardService rewardService, IItemDao itemDao)
        : base(QuestIdConst, dataManager, questDao, rewardService)
    {
        _itemDao = itemDao;
    }

    public override void Register(QuestEngine engine)
    {
        engine.RegisterOnZoneMissionEnd(QuestId);
        engine.RegisterOnLevelUp(QuestId);
        engine.RegisterQuestItem(FragmentItem, QuestId);
        engine.RegisterOnQuestMovieEnd(170, QuestId);
        foreach (int npc in new[] { YuditioNpc, NestorNpc, DianaNpc, AithraNpc, BoreasNpc })
            engine.RegisterQuestNpc(npc).OnTalk.Add(QuestId);
    }

    public override ValueTask<bool> OnZoneMissionEndAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
        => DefaultOnZoneMissionEndEventAsync(env, conn, ct);

    public override ValueTask<bool> OnLevelUpAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
        => DefaultOnLvlUpEventAsync(env, conn, precedingQuestId: 1701, isZoneMission: true, ct);

    public override async ValueTask<bool> OnDialogAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var player      = env.Player;
        var entry       = player.Quests.Get(QuestId);
        if (entry is null) return false;

        int targetId    = env.TargetId;
        int targetObjId = env.Target?.ObjectId ?? 0;
        var dialog      = DialogActionLookup.FromId(env.DialogId);
        int var         = entry.GetVar(0);

        if (entry.Status == QuestStatus.REWARD)
        {
            if (targetId != BoreasNpc) return false;
            if (dialog == DialogAction.USE_OBJECT)
                return await SendQuestDialogAsync(conn, targetObjId, 10002, ct);
            if (env.DialogId == (int)DialogAction.SELECT_QUEST_REWARD)
                return await SendQuestDialogAsync(conn, targetObjId, 5, ct);
            return await SendQuestEndDialogAsync(env, conn, ct);
        }
        if (entry.Status != QuestStatus.START) return false;

        if (targetId == YuditioNpc)
        {
            if (dialog == DialogAction.QUEST_SELECT && var == 0)
                return await SendQuestDialogAsync(conn, targetObjId, 1011, ct);
            if (dialog == DialogAction.SETPRO1 && var == 0)
                // note: TeleportService2 relocation to Sanctum (110010000, 2013.65/1493.05/581.14) dropped — cosmetic, not an instance entry
                return await DefaultCloseDialogAsync(env, conn, 0, 1, ct);
            return false;
        }
        if (targetId == NestorNpc)
        {
            if (dialog == DialogAction.QUEST_SELECT && var == 1) return await SendQuestDialogAsync(conn, targetObjId, 1352, ct);
            if (dialog == DialogAction.QUEST_SELECT && var == 3) return await SendQuestDialogAsync(conn, targetObjId, 2034, ct);
            if (dialog == DialogAction.QUEST_SELECT && var == 5) return await SendQuestDialogAsync(conn, targetObjId, 2716, ct);
            if (dialog == DialogAction.SELECT_ACTION_1353)
            {
                await PlayQuestMovieAsync(conn, player, 102, ct);
                return false;
            }
            if (dialog == DialogAction.SETPRO2 && var == 1) return await DefaultCloseDialogAsync(env, conn, 1, 2, ct);
            if (dialog == DialogAction.SETPRO4 && var == 3)
            {
                await ChangeQuestStepAsync(conn, entry, 0, 4, toReward: false, ct);
                await EnterInstanceAsync(player, conn, 310070000, 180f, 253f, 1374f, 0, ct);
                await PlayQuestMovieAsync(conn, player, 170, ct);
                return await CloseDialogWindowAsync(conn, targetObjId, ct);
            }
            if (dialog == DialogAction.SETPRO6 && var == 5)
            {
                await RemoveQuestItemAsync(player, conn, _itemDao, FragmentItem, 1, ct);
                return await DefaultCloseDialogAsync(env, conn, 5, 6, ct);
            }
            return false;
        }
        if (targetId == DianaNpc)
        {
            if (dialog == DialogAction.QUEST_SELECT && var == 2) return await SendQuestDialogAsync(conn, targetObjId, 1693, ct);
            if (dialog == DialogAction.CHECK_USER_HAS_QUEST_ITEM)
                return await CheckQuestItemsAsync(env, conn, _itemDao, 2, 3, reward: false, checkOkId: 10000, checkFailId: 10001, FragmentItem, 1, ct);
            return false;
        }
        if (targetId == AithraNpc)
        {
            if (dialog == DialogAction.QUEST_SELECT && var == 6) return await SendQuestDialogAsync(conn, targetObjId, 3057, ct);
            if (dialog == DialogAction.SET_SUCCEED && var == 6) return await DefaultCloseDialogAsync(env, conn, 6, 6, reward: true, sameNpc: false, ct);
            return false;
        }
        return false;
    }

    public override async ValueTask<bool> OnMovieEndAsync(QuestEnv env, int movieId, GsClientConnection conn, CancellationToken ct)
    {
        if (movieId != 170) return false;
        var player = env.Player;
        var entry  = player.Quests.Get(QuestId);
        if (entry is null) return false;
        if (entry.GetVar(0) == 4)
        {
            await ChangeQuestStepAsync(conn, entry, 0, 5, toReward: false, ct);
            // note: TeleportService2 relocation to Sanctum (110010000, 2004.24/1489.91/581.14) dropped — cosmetic, not an instance entry
            return true;
        }
        return false;
    }
}
