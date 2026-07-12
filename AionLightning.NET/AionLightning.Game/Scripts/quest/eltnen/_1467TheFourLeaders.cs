// Port of Java data/scripts/system/handlers/quest/eltnen/_1467TheFourLeaders.java (Balthazar).
// Talk to the Overseer (204045) to pick one of four Kaidan Fortress leaders to hunt (SETPRO1..4
// assign quest var 1..4); kill the matching leader npc (211696..211699) to flip to REWARD; turn in
// at the Overseer, picking one of four reward tiers (var-1) via the "no reward" dialog action.
// Java bug fix: onDialogEvent fetches `qs` once at the top of the method (null, since no quest state
// exists yet) and the SETPRO1..4 branches guard on that stale `qs != null` *after* calling
// QuestService.startQuest(env) - the reference is never refreshed, so the guard is always false, the
// leader var is never set, and (combined with the missing switch breaks between SETPRO1..4)
// execution falls through every case down to the default branch. The quest state gets created but
// stuck at var 0 forever, so none of the 4 kill checks (var 1-4) can ever match - the quest could
// never complete. Fixed here by re-fetching the entry after creating it and setting the chosen
// leader var immediately, matching the evident "pick one of the four bounties" intent.
// Java bug fix: onKillEvent's switch(targetId) has no breaks between the four leader cases, so
// failing the current case's "quest var == N" check falls into the next leader's case body and
// matches against ITS var check instead - killing the wrong leader could complete the quest. Fixed
// by gating each leader npc to its own var exactly (single-npc DefaultOnKillEventAsync per leader).
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

namespace Quest.Eltnen;

public sealed class _1467TheFourLeaders : QuestHandlerBase
{
    private const int QuestIdConst = 1467;
    private const int OverseerNpc  = 204045;
    private const int Leader1Npc   = 211696;
    private const int Leader2Npc   = 211697;
    private const int Leader3Npc   = 211698;
    private const int Leader4Npc   = 211699;

    public _1467TheFourLeaders(IDataManager dataManager, IQuestDao questDao, QuestRewardService rewardService, IItemDao itemDao)
        : base(QuestIdConst, dataManager, questDao, rewardService)
    {
    }

    public override void Register(QuestEngine engine)
    {
        engine.RegisterQuestNpc(OverseerNpc).OnQuestStart.Add(QuestId);
        engine.RegisterQuestNpc(OverseerNpc).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(Leader1Npc).OnKill.Add(QuestId);
        engine.RegisterQuestNpc(Leader2Npc).OnKill.Add(QuestId);
        engine.RegisterQuestNpc(Leader3Npc).OnKill.Add(QuestId);
        engine.RegisterQuestNpc(Leader4Npc).OnKill.Add(QuestId);
    }

    public override async ValueTask<bool> OnDialogAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var player = env.Player;
        var entry  = player.Quests.Get(QuestId);
        int targetId    = env.TargetId;
        int targetObjId = env.Target?.ObjectId ?? 0;
        var dialog      = DialogActionLookup.FromId(env.DialogId);

        if (targetId != OverseerNpc) return false;

        if (entry is null || entry.Status == QuestStatus.NONE)
        {
            switch (dialog)
            {
                case DialogAction.QUEST_SELECT:
                    return await SendQuestDialogAsync(conn, targetObjId, 4762, ct);
                case DialogAction.QUEST_ACCEPT_1:
                    return await SendQuestDialogAsync(conn, targetObjId, 1011, ct);
                case DialogAction.SETPRO1:
                    return await PickLeaderAsync(conn, player, 1, ct);
                case DialogAction.SETPRO2:
                    return await PickLeaderAsync(conn, player, 2, ct);
                case DialogAction.SETPRO3:
                    return await PickLeaderAsync(conn, player, 3, ct);
                case DialogAction.SETPRO4:
                    return await PickLeaderAsync(conn, player, 4, ct);
                default:
                    return await SendQuestStartDialogAsync(env, conn, ct);
            }
        }

        if (entry.Status == QuestStatus.REWARD)
        {
            if (dialog == DialogAction.USE_OBJECT)
            {
                int dialogId = entry.GetVar(0) switch { 1 => 5, 2 => 6, 3 => 7, 4 => 8, _ => 0 };
                return dialogId != 0 && await SendQuestDialogAsync(conn, targetObjId, dialogId, ct);
            }
            if (dialog == DialogAction.SELECTED_QUEST_NOREWARD)
            {
                await FinishQuestAsync(conn, player, entry.GetVar(0) - 1, ct);
                return await SendQuestSelectionDialogAsync(conn, targetObjId, ct);
            }
        }
        return false;
    }

    private async ValueTask<bool> PickLeaderAsync(GsClientConnection conn, Player player, int leaderVar, CancellationToken ct)
    {
        if (!await StartMissionAsync(conn, player, QuestStatus.START, ct)) return false;
        var entry = player.Quests.Get(QuestId);
        if (entry is null) return false;

        await ChangeQuestStepAsync(conn, entry, 0, leaderVar, toReward: false, ct);
        return true;
    }

    public override async ValueTask<bool> OnKillAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
        => await DefaultOnKillEventAsync(env, conn, Leader1Npc, 1, reward: true, ct)
        || await DefaultOnKillEventAsync(env, conn, Leader2Npc, 2, reward: true, ct)
        || await DefaultOnKillEventAsync(env, conn, Leader3Npc, 3, reward: true, ct)
        || await DefaultOnKillEventAsync(env, conn, Leader4Npc, 4, reward: true, ct);
}
