// Port of Java data/scripts/system/handlers/quest/rider_quests/_14041AbyssalAbilities.java (pralinka).
// Zone-mission sub-quest of 14040: a chain of 7 npcs (278627-278633), each advancing var0 by one
// and playing a preview cutscene when its SELECT_ACTION_* dialog fires; the last npc (278633)
// flips straight to REWARD; turn in at 278554.
// Java bug: onDialogEvent's per-npc switch had no break after QUEST_SELECT, so talking while var0
// didn't match that npc's step fell through into the SELECT_ACTION_* body and played the preview
// movie unconditionally. Fixed here so each movie only plays on its own actual dialog id.
using System.Threading;
using System.Threading.Tasks;
using AionLightning.Game.Dao;
using AionLightning.Game.DataHolders;
using AionLightning.Game.Model;
using AionLightning.Game.Model.Quest;
using AionLightning.Game.Network.Aion;
using AionLightning.Game.QuestEngine;
using AionLightning.Game.QuestEngine.Handlers;
using AionLightning.Game.QuestEngine.Model;
using AionLightning.Game.Services;

namespace Quest.RiderQuests;

public sealed class _14041AbyssalAbilities : QuestHandlerBase
{
    private const int QuestIdConst = 14041;
    private const int FinalTurnInNpc = 278554;

    public _14041AbyssalAbilities(IDataManager dataManager, IQuestDao questDao, QuestRewardService rewardService, IItemDao itemDao)
        : base(QuestIdConst, dataManager, questDao, rewardService)
    {
    }

    public override void Register(QuestEngine engine)
    {
        engine.RegisterOnZoneMissionEnd(QuestId);
        engine.RegisterOnLevelUp(QuestId);
        foreach (int npc in new[] { 278627, 278628, 278629, 278630, 278631, 278632, 278633, FinalTurnInNpc })
            engine.RegisterQuestNpc(npc).OnTalk.Add(QuestId);
    }

    public override ValueTask<bool> OnZoneMissionEndAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
        => DefaultOnZoneMissionEndEventAsync(env, conn, ct);

    public override ValueTask<bool> OnLevelUpAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
        => DefaultOnLvlUpEventAsync(env, conn, 1701, isZoneMission: true, ct);

    public override async ValueTask<bool> OnDialogAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var player = env.Player;
        var entry  = player.Quests.Get(QuestId);
        if (entry is null) return false;

        var dialog = DialogActionLookup.FromId(env.DialogId);
        int targetObjId = env.Target?.ObjectId ?? 0;

        if (entry.Status == QuestStatus.REWARD)
        {
            if (env.TargetId != FinalTurnInNpc) return false;
            if (dialog == DialogAction.USE_OBJECT)
                return await SendQuestDialogAsync(conn, targetObjId, 10002, ct);
            if (env.DialogId == (int)DialogAction.SELECT_QUEST_REWARD)
                return await SendQuestDialogAsync(conn, targetObjId, 5, ct);
            return await SendQuestEndDialogAsync(env, conn, ct);
        }
        if (entry.Status != QuestStatus.START) return false;

        return env.TargetId switch
        {
            278627 => await HandleChainStepAsync(env, conn, dialog, 0, 1011, DialogAction.SELECT_ACTION_1013, 262, DialogAction.SETPRO1, 1, false, ct),
            278628 => await HandleChainStepAsync(env, conn, dialog, 1, 1352, DialogAction.SELECT_ACTION_1353, 263, DialogAction.SETPRO2, 2, false, ct),
            278629 => await HandleChainStepAsync(env, conn, dialog, 2, 1693, DialogAction.SELECT_ACTION_1694, 264, DialogAction.SETPRO3, 3, false, ct),
            278630 => await HandleChainStepAsync(env, conn, dialog, 3, 2034, DialogAction.SELECT_ACTION_2035, 265, DialogAction.SETPRO4, 4, false, ct),
            278631 => await HandleChainStepAsync(env, conn, dialog, 4, 2375, DialogAction.SELECT_ACTION_2376, 266, DialogAction.SETPRO5, 5, false, ct),
            278632 => await HandleChainStepAsync(env, conn, dialog, 5, 2716, DialogAction.SELECT_ACTION_2717, 267, DialogAction.SETPRO6, 6, false, ct),
            278633 => await HandleChainStepAsync(env, conn, dialog, 6, 3057, DialogAction.SELECT_ACTION_3058, 268, DialogAction.SET_SUCCEED, 6, true, ct),
            _ => false,
        };
    }

    private async ValueTask<bool> HandleChainStepAsync(QuestEnv env, GsClientConnection conn, DialogAction dialog,
        int expectedVar, int selectDialogId, DialogAction actionDialog, int movieId, DialogAction advanceDialog,
        int nextVar, bool reward, CancellationToken ct)
    {
        var entry = env.Player.Quests.Get(QuestId)!;
        int targetObjId = env.Target?.ObjectId ?? 0;

        if (dialog == DialogAction.QUEST_SELECT)
            return entry.GetVar(0) == expectedVar && await SendQuestDialogAsync(conn, targetObjId, selectDialogId, ct);
        if (dialog == actionDialog)
        {
            await PlayQuestMovieAsync(conn, env.Player, movieId, ct);
            return false;
        }
        if (dialog == advanceDialog)
            return await DefaultCloseDialogAsync(env, conn, expectedVar, nextVar, reward, sameNpc: false, ct);
        return false;
    }
}
