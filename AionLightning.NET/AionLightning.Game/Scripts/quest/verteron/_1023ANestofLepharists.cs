// Port of Java data/scripts/system/handlers/quest/verteron/_1023ANestofLepharists.java (Mr. Poke,
// Dune11, vlog, Antraxx). Talk to Spatalos (203098), infiltrate via Khidia (203183) with a
// disguise + 300s timer, enter the sensory zone to trigger the reveal movie, then turn in.
// Zone-mission-end gated on quest 1013; level-up gated on quests 1130 and 1013.
// Java bug: Khidia's case QUEST_SELECT/SETPRO2/SETPRO3/CHECK_USER_HAS_QUEST_ITEM all fall through
// (missing break) into the next case's unconditional body when their own var guard doesn't match
// - e.g. QUEST_SELECT with var!=1/3/4 would misfire SELECT_ACTION_1353's movie+dialog regardless
// of the actual dialog id sent. Ported here as independent per-case branches instead.
// Skip vs Java: registerOnDie/onDieEvent isn't ported (no player-death quest hook exists yet) -
// its only effect (revert var 2 back to 1 early) is also performed by OnEnterWorldAsync, so a
// player who dies mid-timer and doesn't relog just waits out the 300s timer instead. The disguise
// buff (SkillEngine.applyEffectDirectly/removeEffect on skill 8197) is cosmetic and omitted (no
// effect-application infra in the quest engine layer; see altgard _2021KnowYourEnemy precedent).
using System.Collections.Generic;
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

namespace Quest.Verteron;

public sealed class _1023ANestofLepharists : QuestHandlerBase
{
    private const int QuestIdConst = 1023;
    private const int SpatalosNpc  = 203098;
    private const int KhidiaNpc    = 203183;
    private const int RevealMovie  = 23;
    private const string SensoryZone = "LF1A_SENSORYAREA_Q1023_SPG_206008_2_210030000";

    private readonly IItemDao _itemDao;

    public _1023ANestofLepharists(IDataManager dataManager, IQuestDao questDao, QuestRewardService rewardService, IItemDao itemDao)
        : base(QuestIdConst, dataManager, questDao, rewardService)
    {
        _itemDao = itemDao;
    }

    public override void Register(QuestEngine engine)
    {
        engine.RegisterQuestNpc(SpatalosNpc).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(KhidiaNpc).OnTalk.Add(QuestId);
        RegisterOnEnterZone(engine, SensoryZone);
        engine.RegisterOnZoneMissionEnd(QuestId);
        engine.RegisterOnLevelUp(QuestId);
        engine.RegisterOnQuestTimerEnd(QuestId);
        engine.RegisterOnEnterWorld(QuestId);
        engine.RegisterOnQuestMovieEnd(RevealMovie, QuestId);
    }

    public override ValueTask<bool> OnZoneMissionEndAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
        => DefaultOnZoneMissionEndEventAsync(env, conn, precedingQuestId: 1013, ct);

    public override ValueTask<bool> OnLevelUpAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
        => DefaultOnLvlUpEventAsync(env, conn, (IReadOnlyCollection<int>)[1130, 1013], isZoneMission: true, ct);

    public override async ValueTask<bool> OnDialogAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var player = env.Player;
        var entry  = player.Quests.Get(QuestId);
        if (entry is null) return false;

        int targetObjId = env.Target?.ObjectId ?? 0;
        var dialog = DialogActionLookup.FromId(env.DialogId);

        if (entry.Status == QuestStatus.REWARD)
        {
            if (env.TargetId != SpatalosNpc) return false;
            if (dialog == DialogAction.USE_OBJECT)
                return await SendQuestDialogAsync(conn, targetObjId, 2375, ct);
            return await SendQuestEndDialogAsync(env, conn, ct);
        }

        if (entry.Status != QuestStatus.START) return false;
        int var = entry.GetVar(0);

        if (env.TargetId == SpatalosNpc)
        {
            switch (dialog)
            {
                case DialogAction.QUEST_SELECT when var == 0:
                    return await SendQuestDialogAsync(conn, targetObjId, 1011, ct);
                case DialogAction.SELECT_ACTION_1012:
                    return await SendQuestDialogAsync(conn, targetObjId, 1012, ct);
                case DialogAction.SELECT_ACTION_1013:
                    return await SendQuestDialogAsync(conn, targetObjId, 1013, ct);
                case DialogAction.SETPRO1:
                    return await DefaultCloseDialogAsync(env, conn, 0, 1, ct);
                default:
                    return false;
            }
        }

        if (env.TargetId == KhidiaNpc)
        {
            switch (dialog)
            {
                case DialogAction.QUEST_SELECT when var == 1:
                    return await SendQuestDialogAsync(conn, targetObjId, 1352, ct);
                case DialogAction.QUEST_SELECT when var == 3:
                    return await SendQuestDialogAsync(conn, targetObjId, 1693, ct);
                case DialogAction.QUEST_SELECT when var == 4:
                    return await SendQuestDialogAsync(conn, targetObjId, 2034, ct);
                case DialogAction.SELECT_ACTION_1353:
                    await PlayQuestMovieAsync(conn, player, 30, ct);
                    return await SendQuestDialogAsync(conn, targetObjId, 1353, ct);
                case DialogAction.SETPRO2 when var == 1:
                    StartQuestTimer(env, conn, 300);
                    return await DefaultCloseDialogAsync(env, conn, 1, 2, ct);
                case DialogAction.SELECT_ACTION_1694:
                    return await SendQuestDialogAsync(conn, targetObjId, 1694, ct);
                case DialogAction.SETPRO3 when var == 3:
                    return await DefaultCloseDialogAsync(env, conn, 3, 4, ct);
                case DialogAction.CHECK_USER_HAS_QUEST_ITEM when var == 4:
                    return await CheckQuestItemsAsync(env, conn, _itemDao, 4, 4, false, 2120, 2035, ct);
                case DialogAction.FINISH_DIALOG:
                    return await SendQuestDialogAsync(conn, targetObjId, 10, ct);
                case DialogAction.SETPRO4:
                    await ChangeQuestStepAsync(conn, entry, 0, 5, toReward: true, ct);
                    await conn.SendAsync(new SM_DIALOG_WINDOW(targetObjId, 0), ct);
                    return true;
                default:
                    return false;
            }
        }

        return false;
    }

    public override async ValueTask<bool> OnQuestTimerEndAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var entry = env.Player.Quests.Get(QuestId);
        if (entry is null || entry.Status != QuestStatus.START) return false;

        int var = entry.GetVar(0);
        if (var <= 1 || var >= 3) return false;

        await ChangeQuestStepAsync(conn, entry, 0, 1, toReward: false, ct);
        return true;
    }

    public override ValueTask<bool> OnEnterWorldAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
        // Java re-runs the same revert-on-timer-end logic here (after cancelling the pending
        // timer); this port has no timer-cancellation handle, but the timer's later fire is a
        // harmless no-op once var is no longer in (1,3).
        => OnQuestTimerEndAsync(env, conn, ct);

    public override async ValueTask<bool> OnMovieEndAsync(QuestEnv env, int movieId, GsClientConnection conn, CancellationToken ct)
    {
        if (movieId != RevealMovie) return false;
        var entry = env.Player.Quests.Get(QuestId);
        if (entry is null || entry.Status == QuestStatus.COMPLETE) return false;

        // Java also removes the disguise effect (8197) here; omitted along with the apply above.
        await ChangeQuestStepAsync(conn, entry, 0, 3, toReward: false, ct);
        return true;
    }

    public override async ValueTask<bool> OnEnterZoneAsync(QuestEnv env, string zoneName, GsClientConnection conn, CancellationToken ct)
    {
        var entry = env.Player.Quests.Get(QuestId);
        if (entry is null || entry.GetVar(0) != 2) return false;

        // Java also cancels the pending quest timer here; see the OnEnterWorldAsync note above.
        await PlayQuestMovieAsync(conn, env.Player, RevealMovie, ct);
        return true;
    }
}
