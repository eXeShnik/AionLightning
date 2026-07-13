// Port of Java data/scripts/system/handlers/quest/rider_quests/_24041TrainingInTheAbyss.java (pralinka).
// Zone-mission sub-quest of 24040: a linear chain of 6 trainer NPCs (278126/127/128/129/130/131),
// each advancing var0 by one, followed by a cutscene NPC (278136, SET_SUCCEED -> reward); turn in at
// 278054.
// Java bug: every trainer's switch had no break after QUEST_SELECT, so a stray QUEST_SELECT sent
// while var0 didn't match fell through into the SELECT_ACTION_XXXX case and replayed that step's
// cutscene movie unconditionally (that case has no var guard of its own in Java either). Fixed here
// so each movie only plays on its own actual dialog id.
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

public sealed class _24041TrainingInTheAbyss : QuestHandlerBase
{
    private const int QuestIdConst = 24041;
    private const int Npc1 = 278126;
    private const int Npc2 = 278127;
    private const int Npc3 = 278128;
    private const int Npc4 = 278129;
    private const int Npc5 = 278130;
    private const int Npc6 = 278131;
    private const int CutsceneNpc = 278136;
    private const int FinalNpc    = 278054;

    public _24041TrainingInTheAbyss(IDataManager dataManager, IQuestDao questDao, QuestRewardService rewardService, IItemDao itemDao)
        : base(QuestIdConst, dataManager, questDao, rewardService)
    {
    }

    public override void Register(QuestEngine engine)
    {
        engine.RegisterOnZoneMissionEnd(QuestId);
        engine.RegisterOnLevelUp(QuestId);
        foreach (int npc in new[] { Npc1, Npc2, Npc3, Npc4, Npc5, Npc6, CutsceneNpc, FinalNpc })
            engine.RegisterQuestNpc(npc).OnTalk.Add(QuestId);
    }

    public override ValueTask<bool> OnZoneMissionEndAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
        => DefaultOnZoneMissionEndEventAsync(env, conn, ct);

    public override ValueTask<bool> OnLevelUpAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
        => DefaultOnLvlUpEventAsync(env, conn, precedingQuestId: 24040, isZoneMission: true, ct);

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
            if (targetId != FinalNpc) return false;
            if (dialog == DialogAction.USE_OBJECT)
                return await SendQuestDialogAsync(conn, targetObjId, 10002, ct);
            if (env.DialogId == (int)DialogAction.SELECT_QUEST_REWARD)
                return await SendQuestDialogAsync(conn, targetObjId, 5, ct);
            return await SendQuestEndDialogAsync(env, conn, ct);
        }
        if (entry.Status != QuestStatus.START) return false;

        int var = entry.GetVar(0);

        if (targetId == Npc1)
        {
            if (dialog == DialogAction.QUEST_SELECT && var == 0)
                return await SendQuestDialogAsync(conn, targetObjId, 1011, ct);
            if (dialog == DialogAction.SELECT_ACTION_1013)
            {
                await PlayQuestMovieAsync(conn, player, 282, ct);
                return false;
            }
            if (dialog == DialogAction.SETPRO1)
                return await DefaultCloseDialogAsync(env, conn, 0, 1, ct);
            return false;
        }
        if (targetId == Npc2)
        {
            if (dialog == DialogAction.QUEST_SELECT && var == 1)
                return await SendQuestDialogAsync(conn, targetObjId, 1352, ct);
            if (dialog == DialogAction.SELECT_ACTION_1353)
            {
                await PlayQuestMovieAsync(conn, player, 283, ct);
                return false;
            }
            if (dialog == DialogAction.SETPRO2)
                return await DefaultCloseDialogAsync(env, conn, 1, 2, ct);
            return false;
        }
        if (targetId == Npc3)
        {
            if (dialog == DialogAction.QUEST_SELECT && var == 2)
                return await SendQuestDialogAsync(conn, targetObjId, 1693, ct);
            if (dialog == DialogAction.SELECT_ACTION_1694)
            {
                await PlayQuestMovieAsync(conn, player, 284, ct);
                return false;
            }
            if (dialog == DialogAction.SETPRO3)
                return await DefaultCloseDialogAsync(env, conn, 2, 3, ct);
            return false;
        }
        if (targetId == Npc4)
        {
            if (dialog == DialogAction.QUEST_SELECT && var == 3)
                return await SendQuestDialogAsync(conn, targetObjId, 2034, ct);
            if (dialog == DialogAction.SELECT_ACTION_2035)
            {
                await PlayQuestMovieAsync(conn, player, 285, ct);
                return false;
            }
            if (dialog == DialogAction.SETPRO4)
                return await DefaultCloseDialogAsync(env, conn, 3, 4, ct);
            return false;
        }
        if (targetId == Npc5)
        {
            if (dialog == DialogAction.QUEST_SELECT && var == 4)
                return await SendQuestDialogAsync(conn, targetObjId, 2375, ct);
            if (dialog == DialogAction.SELECT_ACTION_2376)
            {
                await PlayQuestMovieAsync(conn, player, 286, ct);
                return false;
            }
            if (dialog == DialogAction.SETPRO5)
                return await DefaultCloseDialogAsync(env, conn, 4, 5, ct);
            return false;
        }
        if (targetId == Npc6)
        {
            if (dialog == DialogAction.QUEST_SELECT && var == 5)
                return await SendQuestDialogAsync(conn, targetObjId, 2716, ct);
            if (dialog == DialogAction.SELECT_ACTION_2717)
            {
                await PlayQuestMovieAsync(conn, player, 287, ct);
                return false;
            }
            if (dialog == DialogAction.SETPRO6)
                return await DefaultCloseDialogAsync(env, conn, 5, 6, ct);
            return false;
        }
        if (targetId == CutsceneNpc)
        {
            if (dialog == DialogAction.QUEST_SELECT && var == 6)
                return await SendQuestDialogAsync(conn, targetObjId, 3057, ct);
            if (dialog == DialogAction.SELECT_ACTION_3058)
            {
                await PlayQuestMovieAsync(conn, player, 288, ct);
                return false;
            }
            if (dialog == DialogAction.SET_SUCCEED)
                return await DefaultCloseDialogAsync(env, conn, 6, 6, reward: true, sameNpc: false, ct);
            return false;
        }
        return false;
    }
}
