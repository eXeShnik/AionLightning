// Port of Java data/scripts/system/handlers/quest/cloister_of_kaisinel/_10001BoundOfInggison.java (dta3000).
// Elyos Inggison zone mission chained off _10000: accept at Outremus (798926), walk the NPC chain
// 798600->798513->203760->203782->798408->203709->798408->798408 (vars 0..7), then reach Inggison
// (world 210050000) to play movie 501 which flips to REWARD; turn in at Outremus / 798408.
// Skip vs Java: TeleportService2.teleportTo relocations (var 7 SET_SUCCEED and REWARD SETPRO10, both
// into Inggison 210050000) have no TeleportService2 port here — dropped, only the var/status
// transitions survive; the movie/enter-world completion path still fires once the player reaches
// 210050000 by other means (same precedent as altgard/_2022CrushingtheConspiracy).
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

namespace Quest.CloisterOfKaisinel;

public sealed class _10001BoundOfInggison : QuestHandlerBase
{
    private const int QuestIdConst   = 10001;
    private const int PrecedingQuest = 10000;
    private const int MovieId        = 501;
    private const int InggisonWorld  = 210050000;

    private const int Outremus = 798926;
    private const int Eremita  = 798600;
    private const int Machiah  = 798513;
    private const int Bellia   = 203760;
    private const int Jhaelas  = 203782;
    private const int Sibylle  = 798408;
    private const int Clymene  = 203709;

    public _10001BoundOfInggison(IDataManager dataManager, IQuestDao questDao, QuestRewardService rewardService, IItemDao itemDao)
        : base(QuestIdConst, dataManager, questDao, rewardService)
    {
    }

    public override void Register(QuestEngine engine)
    {
        engine.RegisterQuestNpc(Outremus).OnQuestStart.Add(QuestId);
        engine.RegisterQuestNpc(Outremus).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(Eremita).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(Machiah).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(Bellia).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(Jhaelas).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(Sibylle).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(Clymene).OnTalk.Add(QuestId);
        engine.RegisterOnEnterWorld(QuestId);
        engine.RegisterOnZoneMissionEnd(QuestId);
        engine.RegisterOnLevelUp(QuestId);
        engine.RegisterOnQuestMovieEnd(MovieId, QuestId);
    }

    public override async ValueTask<bool> OnEnterWorldAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var player = env.Player;
        var entry  = player.Quests.Get(QuestId);
        if (entry is null || entry.Status != QuestStatus.START) return false;
        if (entry.GetVar(0) != 7) return false;
        if (player.Position.WorldId != InggisonWorld) return false;

        await PlayQuestMovieAsync(conn, player, MovieId, ct);
        return true;
    }

    public override ValueTask<bool> OnZoneMissionEndAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
        => DefaultOnZoneMissionEndEventAsync(env, conn, ct);

    public override ValueTask<bool> OnLevelUpAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
        => DefaultOnLvlUpEventAsync(env, conn, precedingQuestId: PrecedingQuest, isZoneMission: true, ct);

    public override async ValueTask<bool> OnMovieEndAsync(QuestEnv env, int movieId, GsClientConnection conn, CancellationToken ct)
    {
        if (movieId != MovieId) return false;
        var player = env.Player;
        if (player.Race != Race.ELYOS) return false;

        var entry = player.Quests.Get(QuestId);
        if (entry is null || entry.Status != QuestStatus.START) return false;

        entry.Status = QuestStatus.REWARD;
        await UpdateQuestStatusAsync(conn, entry, ct);
        return true;
    }

    public override async ValueTask<bool> OnDialogAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var player      = env.Player;
        var entry       = player.Quests.Get(QuestId);
        int targetId    = env.TargetId;
        int targetObjId = env.Target?.ObjectId ?? 0;
        var dialog      = DialogActionLookup.FromId(env.DialogId);

        if (targetId == Outremus && entry is null)
        {
            if (dialog == DialogAction.QUEST_SELECT)
                return await SendQuestDialogAsync(conn, targetObjId, 1011, ct);
            return await SendQuestStartDialogAsync(env, conn, ct);
        }

        if (entry is null) return false;

        int var = entry.GetVar(0);

        if (entry.Status == QuestStatus.START)
        {
            if (targetId == Eremita && var == 0)
            {
                if (dialog == DialogAction.QUEST_SELECT)
                    return await SendQuestDialogAsync(conn, targetObjId, 1011, ct);
                if (dialog == DialogAction.SETPRO1)
                    return await AdvanceAsync(conn, entry, targetObjId, var + 1, ct);
                return await SendQuestStartDialogAsync(env, conn, ct);
            }
            else if (targetId == Machiah && var == 1)
            {
                if (dialog == DialogAction.QUEST_SELECT)
                    return await SendQuestDialogAsync(conn, targetObjId, 1352, ct);
                if (dialog == DialogAction.SETPRO2)
                    return await AdvanceAsync(conn, entry, targetObjId, var + 1, ct);
                return await SendQuestStartDialogAsync(env, conn, ct);
            }
            else if (targetId == Bellia && var == 2)
            {
                if (dialog == DialogAction.QUEST_SELECT)
                    return await SendQuestDialogAsync(conn, targetObjId, 1693, ct);
                if (dialog == DialogAction.SETPRO3)
                    return await AdvanceAsync(conn, entry, targetObjId, var + 1, ct);
                return await SendQuestStartDialogAsync(env, conn, ct);
            }
            else if (targetId == Jhaelas && var == 3)
            {
                if (dialog == DialogAction.QUEST_SELECT)
                    return await SendQuestDialogAsync(conn, targetObjId, 2034, ct);
                if (dialog == DialogAction.SETPRO4)
                    return await AdvanceAsync(conn, entry, targetObjId, var + 1, ct);
                return await SendQuestStartDialogAsync(env, conn, ct);
            }
            else if (targetId == Sibylle && var == 4)
            {
                if (dialog == DialogAction.QUEST_SELECT)
                    return await SendQuestDialogAsync(conn, targetObjId, 2375, ct);
                if (dialog == DialogAction.SETPRO5)
                    return await AdvanceAsync(conn, entry, targetObjId, var + 1, ct);
                return await SendQuestStartDialogAsync(env, conn, ct);
            }
            else if (targetId == Clymene && var == 5)
            {
                if (dialog == DialogAction.QUEST_SELECT)
                    return await SendQuestDialogAsync(conn, targetObjId, 2716, ct);
                if (dialog == DialogAction.SETPRO6)
                    return await AdvanceAsync(conn, entry, targetObjId, var + 1, ct);
                return await SendQuestStartDialogAsync(env, conn, ct);
            }
            else if (targetId == Sibylle && var == 6)
            {
                if (dialog == DialogAction.QUEST_SELECT)
                    return await SendQuestDialogAsync(conn, targetObjId, 3057, ct);
                if (dialog == DialogAction.SETPRO7)
                    return await AdvanceAsync(conn, entry, targetObjId, var + 1, ct);
                return await SendQuestStartDialogAsync(env, conn, ct);
            }
            else if (targetId == Sibylle && var == 7)
            {
                if (dialog == DialogAction.QUEST_SELECT)
                    return await SendQuestDialogAsync(conn, targetObjId, 3398, ct);
                if (dialog == DialogAction.SET_SUCCEED)
                    // note: TeleportService2.teleportTo(210050000,...) dropped; reach Inggison to trigger movie 501.
                    return true;
            }
            return false;
        }

        if (entry.Status == QuestStatus.REWARD)
        {
            if (targetId == Outremus)
            {
                if (env.DialogId == -3)
                    return await SendQuestDialogAsync(conn, targetObjId, 3399, ct);
                return await SendQuestEndDialogAsync(env, conn, ct);
            }
            if (targetId == Sibylle)
            {
                if (dialog == DialogAction.SETPRO10)
                    // note: TeleportService2.teleportTo(210050000,...) dropped; cosmetic relocation.
                    return true;
                return await SendQuestDialogAsync(conn, targetObjId, 3399, ct);
            }
        }

        return false;
    }

    private async ValueTask<bool> AdvanceAsync(GsClientConnection conn, QuestEntry entry, int targetObjId, int nextVar, CancellationToken ct)
    {
        await ChangeQuestStepAsync(conn, entry, 0, nextVar, toReward: false, ct);
        await conn.SendAsync(new SM_DIALOG_WINDOW(targetObjId, 10), ct);
        return true;
    }
}
