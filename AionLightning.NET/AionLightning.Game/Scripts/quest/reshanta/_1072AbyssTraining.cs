// Port of Java data/scripts/system/handlers/quest/reshanta/_1072AbyssTraining.java (Rhys2002).
// Talks to a chain of 7 training npcs (278627..278633), each playing a movie then advancing var
// 0->6, then reports to 278554 for the reward. Zone-mission chain, level-up gated on quest 1701.
// Java bug fixed: each step's switch(dialog) had QUEST_SELECT fall through into the movie-play
// case with no break when the var didn't match (e.g. re-opening the dialog after already advancing
// past that npc would incorrectly replay its movie) - ported with explicit `when` guards so the
// movie only plays on its own dedicated SELECT_ACTION_* dialog id, matching the other steps.
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

public sealed class _1072AbyssTraining : QuestHandlerBase
{
    private const int QuestIdConst = 1072;
    private const int Npc1 = 278627;
    private const int Npc2 = 278628;
    private const int Npc3 = 278629;
    private const int Npc4 = 278630;
    private const int Npc5 = 278631;
    private const int Npc6 = 278632;
    private const int Npc7 = 278633;
    private const int RewardNpc = 278554;

    public _1072AbyssTraining(IDataManager dataManager, IQuestDao questDao, QuestRewardService rewardService, IItemDao itemDao)
        : base(QuestIdConst, dataManager, questDao, rewardService)
    {
    }

    public override void Register(QuestEngine engine)
    {
        engine.RegisterOnZoneMissionEnd(QuestId);
        engine.RegisterOnLevelUp(QuestId);
        foreach (int npc in new[] { Npc1, Npc2, Npc3, Npc4, Npc5, Npc6, Npc7, RewardNpc })
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
            if (targetId != RewardNpc) return false;
            if (dialog == DialogAction.USE_OBJECT)
                return await SendQuestDialogAsync(conn, targetObjId, 10002, ct);
            if (env.DialogId == (int)DialogAction.SELECT_QUEST_REWARD)
                return await SendQuestDialogAsync(conn, targetObjId, 5, ct);
            return await SendQuestEndDialogAsync(env, conn, ct);
        }
        if (entry.Status != QuestStatus.START) return false;

        switch (targetId)
        {
            case Npc1:
                switch (dialog)
                {
                    case DialogAction.QUEST_SELECT when var == 0:
                        return await SendQuestDialogAsync(conn, targetObjId, 1011, ct);
                    case DialogAction.SELECT_ACTION_1013:
                        await PlayQuestMovieAsync(conn, player, 262, ct);
                        return false;
                    case DialogAction.SETPRO1 when var == 0:
                        return await DefaultCloseDialogAsync(env, conn, 0, 1, ct);
                    default:
                        return false;
                }
            case Npc2:
                switch (dialog)
                {
                    case DialogAction.QUEST_SELECT when var == 1:
                        return await SendQuestDialogAsync(conn, targetObjId, 1352, ct);
                    case DialogAction.SELECT_ACTION_1353:
                        await PlayQuestMovieAsync(conn, player, 263, ct);
                        return false;
                    case DialogAction.SETPRO2 when var == 1:
                        return await DefaultCloseDialogAsync(env, conn, 1, 2, ct);
                    default:
                        return false;
                }
            case Npc3:
                switch (dialog)
                {
                    case DialogAction.QUEST_SELECT when var == 2:
                        return await SendQuestDialogAsync(conn, targetObjId, 1693, ct);
                    case DialogAction.SELECT_ACTION_1694:
                        await PlayQuestMovieAsync(conn, player, 264, ct);
                        return false;
                    case DialogAction.SETPRO3 when var == 2:
                        return await DefaultCloseDialogAsync(env, conn, 2, 3, ct);
                    default:
                        return false;
                }
            case Npc4:
                switch (dialog)
                {
                    case DialogAction.QUEST_SELECT when var == 3:
                        return await SendQuestDialogAsync(conn, targetObjId, 2034, ct);
                    case DialogAction.SELECT_ACTION_2035:
                        await PlayQuestMovieAsync(conn, player, 265, ct);
                        return false;
                    case DialogAction.SETPRO4 when var == 3:
                        return await DefaultCloseDialogAsync(env, conn, 3, 4, ct);
                    default:
                        return false;
                }
            case Npc5:
                switch (dialog)
                {
                    case DialogAction.QUEST_SELECT when var == 4:
                        return await SendQuestDialogAsync(conn, targetObjId, 2375, ct);
                    case DialogAction.SELECT_ACTION_2376:
                        await PlayQuestMovieAsync(conn, player, 266, ct);
                        return false;
                    case DialogAction.SETPRO5 when var == 4:
                        return await DefaultCloseDialogAsync(env, conn, 4, 5, ct);
                    default:
                        return false;
                }
            case Npc6:
                switch (dialog)
                {
                    case DialogAction.QUEST_SELECT when var == 5:
                        return await SendQuestDialogAsync(conn, targetObjId, 2716, ct);
                    case DialogAction.SELECT_ACTION_2717:
                        await PlayQuestMovieAsync(conn, player, 267, ct);
                        return false;
                    case DialogAction.SETPRO6 when var == 5:
                        return await DefaultCloseDialogAsync(env, conn, 5, 6, ct);
                    default:
                        return false;
                }
            case Npc7:
                switch (dialog)
                {
                    case DialogAction.QUEST_SELECT when var == 6:
                        return await SendQuestDialogAsync(conn, targetObjId, 3057, ct);
                    case DialogAction.SELECT_ACTION_3058:
                        await PlayQuestMovieAsync(conn, player, 268, ct);
                        return false;
                    case DialogAction.SET_SUCCEED when var == 6:
                        await ChangeQuestStepAsync(conn, entry, -1, 0, toReward: true, ct);
                        return await SendQuestSelectionDialogAsync(conn, targetObjId, ct);
                    default:
                        return false;
                }
            default:
                return false;
        }
    }
}
