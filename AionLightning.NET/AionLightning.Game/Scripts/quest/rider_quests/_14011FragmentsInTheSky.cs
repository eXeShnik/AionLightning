// Port of Java data/scripts/system/handlers/quest/rider_quests/_14011FragmentsInTheSky.java (pralinka).
// Zone-mission sub-quest of 14010: talk to Kairon (203109, var 0->1), kill Sky Rippers (700091,
// var 2->4), talk to Hynops (203122, var 1->2, movie 24), turn in at Kairon.
// Java bug: the onDialogEvent switch on Hynops (203122) fell through from case QUEST_SELECT into
// case SETPRO2 (missing break) — talking with any var other than 1 would still play movie 24 as a
// side effect before defaultCloseDialog's var guard failed. Fixed here by only running the
// SETPRO2 body when the dialog is actually SETPRO2.
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

namespace Quest.RiderQuests;

public sealed class _14011FragmentsInTheSky : QuestHandlerBase
{
    private const int QuestIdConst = 14011;
    private const int KaironNpc    = 203109;
    private const int HynopsNpc    = 203122;
    private const int SkyRipperNpc = 700091;

    public _14011FragmentsInTheSky(IDataManager dataManager, IQuestDao questDao, QuestRewardService rewardService, IItemDao itemDao)
        : base(QuestIdConst, dataManager, questDao, rewardService)
    {
    }

    public override void Register(QuestEngine engine)
    {
        engine.RegisterQuestNpc(SkyRipperNpc).OnKill.Add(QuestId);
        engine.RegisterOnZoneMissionEnd(QuestId);
        engine.RegisterOnLevelUp(QuestId);
        engine.RegisterQuestNpc(KaironNpc).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(HynopsNpc).OnTalk.Add(QuestId);
    }

    public override async ValueTask<bool> OnKillAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var entry = env.Player.Quests.Get(QuestId);
        if (entry is null || entry.Status != QuestStatus.START) return false;

        int var = entry.GetVar(0);
        if (var >= 2 && var < 4)
            return await DefaultOnKillEventAsync(env, conn, SkyRipperNpc, 2, 4, ct);
        if (var == 4)
        {
            entry.Status = QuestStatus.REWARD;
            await UpdateQuestStatusAsync(conn, entry, ct);
            return true;
        }
        return false;
    }

    public override async ValueTask<bool> OnDialogAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var player = env.Player;
        var entry  = player.Quests.Get(QuestId);
        if (entry is null) return false;

        int var         = entry.GetVar(0);
        int targetObjId = env.Target?.ObjectId ?? 0;
        var dialog      = DialogActionLookup.FromId(env.DialogId);

        if (entry.Status == QuestStatus.START)
        {
            if (env.TargetId == KaironNpc)
            {
                if (dialog == DialogAction.QUEST_SELECT)
                {
                    if (var == 0) return await SendQuestDialogAsync(conn, targetObjId, 1011, ct);
                    if (var == 5) return await SendQuestDialogAsync(conn, targetObjId, 1693, ct);
                    return false;
                }
                if (dialog == DialogAction.SETPRO1)
                    return await DefaultCloseDialogAsync(env, conn, 0, 1, ct);
                return false;
            }

            if (env.TargetId == HynopsNpc)
            {
                if (dialog == DialogAction.QUEST_SELECT && var == 1)
                    return await SendQuestDialogAsync(conn, targetObjId, 1352, ct);
                if (dialog == DialogAction.SETPRO2)
                {
                    await PlayQuestMovieAsync(conn, player, 24, ct);
                    return await DefaultCloseDialogAsync(env, conn, 1, 2, ct);
                }
                return false;
            }
        }
        else if (entry.Status == QuestStatus.REWARD)
        {
            if (env.TargetId == KaironNpc)
            {
                if (dialog == DialogAction.USE_OBJECT)
                    return await SendQuestDialogAsync(conn, targetObjId, 1693, ct);
                return await SendQuestEndDialogAsync(env, conn, ct);
            }
        }
        return false;
    }

    public override ValueTask<bool> OnZoneMissionEndAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
        => DefaultOnZoneMissionEndEventAsync(env, conn, ct);

    public override ValueTask<bool> OnLevelUpAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
        => DefaultOnLvlUpEventAsync(env, conn, 14010, isZoneMission: true, ct);
}
