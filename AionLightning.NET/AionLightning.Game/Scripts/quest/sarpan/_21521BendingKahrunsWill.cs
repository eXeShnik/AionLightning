// Port of Java data/scripts/system/handlers/quest/sarpan/_21521BendingKahrunsWill.java (zhkchi).
// Asmodian mirror of Elyos _11522FurbackHunting — talk to Ophelia (799266) to start (no item);
// kill 5 opposing-race players within level range [victimLevel-5, victimLevel+9] anywhere in
// Sarpan to complete; turn in at Ophelia. Same skips as the Elyos mirror: no HEROS_DISCUS zone
// sub-gate (no zone-shape infra), no daily-repeat modeling, and Java's redundant REWARD-status
// SELECT_QUEST_REWARD case collapses into this port's SendQuestEndDialogAsync.
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

namespace Quest.Sarpan;

public sealed class _21521BendingKahrunsWill : QuestHandlerBase
{
    private const int QuestIdConst = 21521;
    private const int OpheliaNpc    = 799266;
    private const int SarpanWorldId = 600020000;
    private const int KillGoal      = 5;

    public _21521BendingKahrunsWill(IDataManager dataManager, IQuestDao questDao, QuestRewardService rewardService, IItemDao itemDao)
        : base(QuestIdConst, dataManager, questDao, rewardService)
    {
    }

    public override void Register(QuestEngine engine)
    {
        engine.RegisterQuestNpc(OpheliaNpc).OnQuestStart.Add(QuestId);
        engine.RegisterQuestNpc(OpheliaNpc).OnTalk.Add(QuestId);
        engine.RegisterKillInWorld(SarpanWorldId, QuestId);
    }

    public override async ValueTask<bool> OnPlayerKillAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var entry = env.Player.Quests.Get(QuestId);
        if (entry is null || entry.Status != QuestStatus.START) return false;
        if (env.Target is not Player victim) return false;

        int killerLevel = env.Player.Level;
        int victimLevel = victim.Level;
        if (killerLevel < victimLevel - 5 || killerLevel > victimLevel + 9) return false;

        int var = entry.GetVar(0);
        if (var < KillGoal - 1)
        {
            await ChangeQuestStepAsync(conn, entry, varIdx: 0, newValue: var + 1, toReward: false, ct);
            return true;
        }
        if (var == KillGoal - 1)
        {
            await ChangeQuestStepAsync(conn, entry, varIdx: -1, newValue: 0, toReward: true, ct);
            return true;
        }
        return false;
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
            if (targetId != OpheliaNpc) return false;
            if (dialog == DialogAction.QUEST_SELECT) return await SendQuestDialogAsync(conn, targetObjId, 4762, ct);
            return await SendQuestStartDialogAsync(env, conn, ct);
        }

        if (entry.Status == QuestStatus.REWARD && targetId == OpheliaNpc)
        {
            if (dialog == DialogAction.USE_OBJECT) return await SendQuestDialogAsync(conn, targetObjId, 10002, ct);
            return await SendQuestEndDialogAsync(env, conn, ct);
        }

        return false;
    }
}
