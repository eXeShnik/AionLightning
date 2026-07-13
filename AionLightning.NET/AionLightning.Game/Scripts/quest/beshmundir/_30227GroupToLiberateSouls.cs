// Port of Java data/scripts/system/handlers/quest/beshmundir/_30227GroupToLiberateSouls.java (Gigi).
// Talk to 798946 to start; at 799521, SETPRO1 (or QUEST_SELECT once var != 0 - intentional Java
// switch shortcut, see greater_stigma/_30217GroupStigmasScars) advances var 0->1; at 799517, SETPRO1
// starts a 300s quest timer; killing 216586/216590 while var==1 advances var 1->2 and plays movie
// 445; killing any of 216733/216734/216735/216736/216737/216738/216245 while var==2 flips to
// REWARD; turn in at 798946.
// Java bug fixed: the outer switch(targetId) in onDialogEvent has no break between the 799521/
// 799517 case blocks, so a non-matching dialog at 799521 would silently fall through into 799517's
// case body using the same dialog value. Ported as independent per-npc `if` blocks instead - no
// cross-npc fallthrough, same precedent as greater_stigma/_30217GroupStigmasScars.
// Skip vs Java: QuestService.questTimerEnd(env) (cancelling the pending 300s timer on kill) has no
// equivalent - no timer-cancellation API is ported (see QuestHandlerBase.StartQuestTimer); the stale
// timer harmlessly no-ops on expiry since var has already moved past 1 by then.
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

public sealed class _30227GroupToLiberateSouls : QuestHandlerBase
{
    private const int QuestIdConst = 30227;
    private const int StartNpc     = 798946;
    private const int AdvanceNpc   = 799521;
    private const int TimerNpc     = 799517;
    private static readonly int[] MovieKillNpcs = [216586, 216590];
    private static readonly int[] FinalKillNpcs = [216733, 216734, 216735, 216736, 216737, 216738, 216245];

    public _30227GroupToLiberateSouls(IDataManager dataManager, IQuestDao questDao, QuestRewardService rewardService, IItemDao itemDao)
        : base(QuestIdConst, dataManager, questDao, rewardService)
    {
    }

    public override void Register(QuestEngine engine)
    {
        engine.RegisterQuestNpc(StartNpc).OnQuestStart.Add(QuestId);
        engine.RegisterQuestNpc(StartNpc).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(AdvanceNpc).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(TimerNpc).OnTalk.Add(QuestId);
        foreach (int npcId in MovieKillNpcs) engine.RegisterQuestNpc(npcId).OnKill.Add(QuestId);
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

        if (MovieKillNpcs.Contains(targetId) && entry.GetVar(0) == 1)
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
