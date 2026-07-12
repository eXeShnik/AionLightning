// Port of Java data/scripts/system/handlers/quest/sarpan/_11522FurbackHunting.java (zhkchi).
// Talk to Rigan (799532) to start (no item); kill 5 opposing-race players within level range
// [victimLevel-5, victimLevel+9] anywhere in Sarpan to complete; turn in at Rigan.
// Skips vs Java: the HEROS_DISCUS_600020000 sub-zone gate (onKillInWorldEvent originally required
// both players be inside that specific arena zone) — no zone-shape infra in this port yet, so the
// trigger is broadened to "anywhere in Sarpan" (already the granularity RegisterKillInWorld
// provides); opposite-race is still guaranteed by QuestEngine.OnPlayerKillAsync's caller
// (Combat.Handlers.PvpKillHandler only dispatches cross-race kills). qs.canRepeat() (daily-reset)
// isn't ported — completes once per character like the rest of this port. Java's REWARD-status
// `case SELECT_QUEST_REWARD: sendQuestDialog(env, 5)` is redundant with what this port's
// SendQuestEndDialogAsync already does for that same dialog id (it treats SELECT_QUEST_REWARD as
// the actual finish trigger, collapsing Java's two-stage reward-page/confirm flow into one), so no
// separate case is needed here — it simply falls into the shared end-dialog path.
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

public sealed class _11522FurbackHunting : QuestHandlerBase
{
    private const int QuestIdConst = 11522;
    private const int RiganNpc      = 799532;
    private const int SarpanWorldId = 600020000;
    private const int KillGoal      = 5;

    public _11522FurbackHunting(IDataManager dataManager, IQuestDao questDao, QuestRewardService rewardService, IItemDao itemDao)
        : base(QuestIdConst, dataManager, questDao, rewardService)
    {
    }

    public override void Register(QuestEngine engine)
    {
        engine.RegisterQuestNpc(RiganNpc).OnQuestStart.Add(QuestId);
        engine.RegisterQuestNpc(RiganNpc).OnTalk.Add(QuestId);
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
            if (targetId != RiganNpc) return false;
            if (dialog == DialogAction.QUEST_SELECT) return await SendQuestDialogAsync(conn, targetObjId, 4762, ct);
            return await SendQuestStartDialogAsync(env, conn, ct);
        }

        if (entry.Status == QuestStatus.REWARD && targetId == RiganNpc)
        {
            if (dialog == DialogAction.USE_OBJECT) return await SendQuestDialogAsync(conn, targetObjId, 10002, ct);
            return await SendQuestEndDialogAsync(env, conn, ct);
        }

        return false;
    }
}
