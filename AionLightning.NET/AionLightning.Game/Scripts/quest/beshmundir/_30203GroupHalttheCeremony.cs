// Port of Java data/scripts/system/handlers/quest/beshmundir/_30203GroupHalttheCeremony.java (Gigi).
// Talk to 798926 to start; kill any of 216175/216177/216179/216181 (each sets its own quest-var
// slot 0-3, always overwriting to 1 - Java's kill-branch conditions are effectively unconditional,
// ported literally) then kill 216263 once all four vars are 1 to flip to REWARD (plays movie 443);
// turn in at 798926.
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

public sealed class _30203GroupHalttheCeremony : QuestHandlerBase
{
    private const int QuestIdConst = 30203;
    private const int StartNpc     = 798926;
    private const int KillNpc1     = 216175;
    private const int KillNpc2     = 216177;
    private const int KillNpc3     = 216179;
    private const int KillNpc4     = 216181;
    private const int FinalNpc     = 216263;

    public _30203GroupHalttheCeremony(IDataManager dataManager, IQuestDao questDao, QuestRewardService rewardService, IItemDao itemDao)
        : base(QuestIdConst, dataManager, questDao, rewardService)
    {
    }

    public override void Register(QuestEngine engine)
    {
        engine.RegisterQuestNpc(StartNpc).OnQuestStart.Add(QuestId);
        engine.RegisterQuestNpc(StartNpc).OnTalk.Add(QuestId);
        foreach (int npcId in new[] { KillNpc1, KillNpc2, KillNpc3, KillNpc4, FinalNpc })
            engine.RegisterQuestNpc(npcId).OnKill.Add(QuestId);
    }

    public override async ValueTask<bool> OnDialogAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var player      = env.Player;
        var entry       = player.Quests.Get(QuestId);
        int targetObjId = env.Target?.ObjectId ?? 0;
        var dialog      = DialogActionLookup.FromId(env.DialogId);

        if (env.TargetId != StartNpc) return false;

        if (entry is null || entry.Status == QuestStatus.NONE)
        {
            if (dialog == DialogAction.QUEST_SELECT) return await SendQuestDialogAsync(conn, targetObjId, 4762, ct);
            return await SendQuestStartDialogAsync(env, conn, ct);
        }

        if (entry.Status == QuestStatus.START)
        {
            if (dialog == DialogAction.QUEST_SELECT) return await SendQuestDialogAsync(conn, targetObjId, 10002, ct);
            return false;
        }

        if (entry.Status == QuestStatus.REWARD) return await SendQuestEndDialogAsync(env, conn, ct);

        return false;
    }

    public override async ValueTask<bool> OnKillAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var entry = env.Player.Quests.Get(QuestId);
        if (entry is null || entry.Status != QuestStatus.START) return false;

        switch (env.TargetId)
        {
            case KillNpc1: entry.SetVar(0, 1); await UpdateQuestStatusAsync(conn, entry, ct); break;
            case KillNpc2: entry.SetVar(1, 1); await UpdateQuestStatusAsync(conn, entry, ct); break;
            case KillNpc3: entry.SetVar(2, 1); await UpdateQuestStatusAsync(conn, entry, ct); break;
            case KillNpc4: entry.SetVar(3, 1); await UpdateQuestStatusAsync(conn, entry, ct); break;
            case FinalNpc:
                if (entry.GetVar(0) == 1 && entry.GetVar(1) == 1 && entry.GetVar(2) == 1 && entry.GetVar(3) == 1)
                {
                    entry.Status = QuestStatus.REWARD;
                    await UpdateQuestStatusAsync(conn, entry, ct);
                    await PlayQuestMovieAsync(conn, env.Player, 443, ct);
                }
                break;
        }
        return false;
    }
}
