// Port of Java data/scripts/system/handlers/quest/convent_of_marchutan/_20001TravelingtoGelkmaros.java (Gigi).
// Asmodian Gelkmaros zone mission chained off _20000: walk the NPC chain 798800->798409->204202->
// 204073->204283 (vars 0..5), then reach Gelkmaros (world 220070000) to play movie 551 which flips to
// REWARD; turn in at 799225 / 798409.
// Skip vs Java: TeleportService2.teleportTo relocations (var 5 SET_SUCCEED and REWARD SETPRO10, both
// into Gelkmaros 220070000) have no TeleportService2 port here — dropped, only the var/status
// transitions survive; the movie/enter-world completion path still fires once the player reaches
// 220070000 by other means (same precedent as altgard/_2022CrushingtheConspiracy).
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

namespace Quest.ConventOfMarchutan;

public sealed class _20001TravelingtoGelkmaros : QuestHandlerBase
{
    private const int QuestIdConst   = 20001;
    private const int PrecedingQuest = 20000;
    private const int MovieId        = 551;
    private const int GelkmarosWorld = 220070000;

    private const int Eremita = 798800;
    private const int Vidar2  = 798409; // eremitia (relay)
    private const int Machiah = 204202;
    private const int Bellia  = 204073;
    private const int Jhaelas = 204283;
    private const int Sibylle = 799225;

    public _20001TravelingtoGelkmaros(IDataManager dataManager, IQuestDao questDao, QuestRewardService rewardService, IItemDao itemDao)
        : base(QuestIdConst, dataManager, questDao, rewardService)
    {
    }

    public override void Register(QuestEngine engine)
    {
        engine.RegisterOnZoneMissionEnd(QuestId);
        engine.RegisterOnLevelUp(QuestId);
        engine.RegisterOnEnterWorld(QuestId);
        engine.RegisterOnQuestMovieEnd(MovieId, QuestId);
        engine.RegisterQuestNpc(Eremita).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(Vidar2).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(Machiah).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(Bellia).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(Jhaelas).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(Sibylle).OnTalk.Add(QuestId);
    }

    public override ValueTask<bool> OnZoneMissionEndAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
        => DefaultOnZoneMissionEndEventAsync(env, conn, ct);

    public override ValueTask<bool> OnLevelUpAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
        => DefaultOnLvlUpEventAsync(env, conn, precedingQuestId: PrecedingQuest, isZoneMission: true, ct);

    public override async ValueTask<bool> OnMovieEndAsync(QuestEnv env, int movieId, GsClientConnection conn, CancellationToken ct)
    {
        if (movieId != MovieId) return false;
        var player = env.Player;
        if (player.Race != Race.ASMODIANS) return false;

        var entry = player.Quests.Get(QuestId);
        if (entry is null || entry.Status != QuestStatus.START) return false;

        entry.Status = QuestStatus.REWARD;
        await UpdateQuestStatusAsync(conn, entry, ct);
        return true;
    }

    public override async ValueTask<bool> OnEnterWorldAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var player = env.Player;
        var entry  = player.Quests.Get(QuestId);
        if (entry is null || entry.Status != QuestStatus.START) return false;
        if (entry.GetVar(0) != 5) return false;
        if (player.Position.WorldId != GelkmarosWorld) return false;

        await PlayQuestMovieAsync(conn, player, MovieId, ct);
        return true;
    }

    public override async ValueTask<bool> OnDialogAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var player      = env.Player;
        var entry       = player.Quests.Get(QuestId);
        if (entry is null) return false;

        int var         = entry.GetVar(0);
        int targetId    = env.TargetId;
        int targetObjId = env.Target?.ObjectId ?? 0;
        var dialog      = DialogActionLookup.FromId(env.DialogId);

        if (entry.Status == QuestStatus.START)
        {
            if (targetId == Eremita && var == 0)
            {
                if (dialog == DialogAction.QUEST_SELECT)
                    return await SendQuestDialogAsync(conn, targetObjId, 1011, ct);
                if (dialog == DialogAction.SETPRO1)
                    return await AdvanceAsync(conn, entry, targetObjId, var + 1, ct);
            }

            if (targetId == Vidar2)
            {
                if (var == 1)
                {
                    if (dialog == DialogAction.QUEST_SELECT)
                        return await SendQuestDialogAsync(conn, targetObjId, 1352, ct);
                    if (dialog == DialogAction.SETPRO2)
                        return await AdvanceAsync(conn, entry, targetObjId, var + 1, ct);
                    return await SendQuestStartDialogAsync(env, conn, ct);
                }
                if (var == 5)
                {
                    if (dialog == DialogAction.QUEST_SELECT)
                        return await SendQuestDialogAsync(conn, targetObjId, 2716, ct);
                    if (dialog == DialogAction.SET_SUCCEED)
                        // note: TeleportService2.teleportTo(220070000,...) dropped; reach Gelkmaros to trigger movie 551.
                        return true;
                    return await SendQuestStartDialogAsync(env, conn, ct);
                }
            }
            else if (targetId == Machiah && var == 2)
            {
                if (dialog == DialogAction.QUEST_SELECT)
                    return await SendQuestDialogAsync(conn, targetObjId, 1693, ct);
                if (dialog == DialogAction.SETPRO3)
                    return await AdvanceAsync(conn, entry, targetObjId, var + 1, ct);
                return await SendQuestStartDialogAsync(env, conn, ct);
            }
            else if (targetId == Bellia && var == 3)
            {
                if (dialog == DialogAction.QUEST_SELECT)
                    return await SendQuestDialogAsync(conn, targetObjId, 2034, ct);
                if (dialog == DialogAction.SETPRO4)
                    return await AdvanceAsync(conn, entry, targetObjId, var + 1, ct);
                return await SendQuestStartDialogAsync(env, conn, ct);
            }
            else if (targetId == Jhaelas && var == 4)
            {
                if (dialog == DialogAction.QUEST_SELECT)
                    return await SendQuestDialogAsync(conn, targetObjId, 2375, ct);
                if (dialog == DialogAction.SETPRO5)
                    return await AdvanceAsync(conn, entry, targetObjId, var + 1, ct);
                return await SendQuestStartDialogAsync(env, conn, ct);
            }
            return false;
        }

        if (entry.Status == QuestStatus.REWARD)
        {
            if (targetId == Sibylle)
            {
                if (dialog == DialogAction.QUEST_SELECT)
                    return await SendQuestDialogAsync(conn, targetObjId, 2802, ct);
                return await SendQuestEndDialogAsync(env, conn, ct);
            }
            if (targetId == Vidar2)
            {
                if (dialog == DialogAction.SETPRO10)
                    // note: TeleportService2.teleportTo(220070000,...) dropped; cosmetic relocation.
                    return true;
                return await SendQuestDialogAsync(conn, targetObjId, 2802, ct);
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
