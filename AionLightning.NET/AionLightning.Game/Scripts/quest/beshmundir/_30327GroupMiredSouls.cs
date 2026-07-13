// Port of Java data/scripts/system/handlers/quest/beshmundir/_30327GroupMiredSouls.java (Gigi).
// Asmodian counterpart of _30227GroupToLiberateSouls, identical shape at different npcs/mobs: talk
// to 799244 to start; at 799521, SETPRO1 (or QUEST_SELECT once var != 0 - intentional Java switch
// shortcut, see greater_stigma/_30217GroupStigmasScars) advances var 0->1; at 799517, SETPRO1 starts
// a 300s quest timer; killing 216586 while var==1 advances var 1->2 and plays movie 445; killing any
// of 216735/216734/216737/216245 while var==2 flips to REWARD; turn in at 799244.
// Java bug fixed / skip: same outer switch(targetId) fallthrough fix and questTimerEnd skip as
// _30227GroupToLiberateSouls - see that file's header.
using System.Linq;
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

namespace Quest.Beshmundir;

public sealed class _30327GroupMiredSouls : QuestHandlerBase
{
    private const int QuestIdConst = 30327;
    private const int StartNpc     = 799244;
    private const int AdvanceNpc   = 799521;
    private const int TimerNpc     = 799517;
    private const int MovieKillNpc = 216586;
    private static readonly int[] FinalKillNpcs = [216735, 216734, 216737, 216245];

    public _30327GroupMiredSouls(IDataManager dataManager, IQuestDao questDao, QuestRewardService rewardService, IItemDao itemDao)
        : base(QuestIdConst, dataManager, questDao, rewardService)
    {
    }

    public override void Register(QuestEngine engine)
    {
        engine.RegisterQuestNpc(StartNpc).OnQuestStart.Add(QuestId);
        engine.RegisterQuestNpc(StartNpc).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(AdvanceNpc).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(TimerNpc).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(MovieKillNpc).OnKill.Add(QuestId);
        foreach (int npcId in FinalKillNpcs) engine.RegisterQuestNpc(npcId).OnKill.Add(QuestId);
        engine.RegisterOnQuestTimerEnd(QuestId);
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
            if (targetId != StartNpc) return false;
            if (dialog == DialogAction.QUEST_SELECT) return await SendQuestDialogAsync(conn, targetObjId, 4762, ct);
            return await SendQuestStartDialogAsync(env, conn, ct);
        }

        if (entry.Status == QuestStatus.START)
        {
            if (targetId == AdvanceNpc)
            {
                int var = entry.GetVar(0);
                if (dialog == DialogAction.QUEST_SELECT && var == 0)
                    return await SendQuestDialogAsync(conn, targetObjId, 1011, ct);
                if (dialog == DialogAction.SETPRO1 || (dialog == DialogAction.QUEST_SELECT && var != 0))
                    return await DefaultCloseDialogAsync(env, conn, 0, 1, ct);
                return false;
            }

            if (targetId == TimerNpc)
            {
                if (dialog != DialogAction.SETPRO1) return false;
                StartQuestTimer(env, conn, 300);
                return true;
            }

            return false;
        }

        if (entry.Status == QuestStatus.REWARD && targetId == StartNpc)
        {
            return dialog switch
            {
                DialogAction.USE_OBJECT          => await SendQuestDialogAsync(conn, targetObjId, 10002, ct),
                DialogAction.SELECT_QUEST_REWARD => await SendQuestDialogAsync(conn, targetObjId, 5, ct),
                _ => await SendQuestEndDialogAsync(env, conn, ct),
            };
        }

        return false;
    }

    public override async ValueTask<bool> OnQuestTimerEndAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var entry = env.Player.Quests.Get(QuestId);
        if (entry is null || entry.Status != QuestStatus.START || entry.GetVar(0) != 1) return false;

        await ChangeQuestStepAsync(conn, entry, 0, 0, toReward: false, ct);
        return true;
    }

    public override async ValueTask<bool> OnKillAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var entry = env.Player.Quests.Get(QuestId);
        if (entry is null || entry.Status != QuestStatus.START) return false;

        int targetId = env.TargetId;

        if (targetId == MovieKillNpc && entry.GetVar(0) == 1)
        {
            await ChangeQuestStepAsync(conn, entry, 0, entry.GetVar(0) + 1, toReward: false, ct);
            await PlayQuestMovieAsync(conn, env.Player, 445, ct);
            return true;
        }

        if (FinalKillNpcs.Contains(targetId) && entry.GetVar(0) == 2)
        {
            entry.Status = QuestStatus.REWARD;
            await UpdateQuestStatusAsync(conn, entry, ct);
            return true;
        }

        return false;
    }
}
