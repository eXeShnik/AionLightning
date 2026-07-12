// Port of Java data/scripts/system/handlers/quest/brusthonin/_4077PorgusRoundup.java.
// Talk to Holekk (205158) to start; attack the Porgus (214732) twice within 10 units of a fixed
// pen location (var 0->1, then var 1 -> REWARD; Java's MathUtil.getDistance gate, ported via
// Position.DistanceTo — this is a plain coordinate check, not a zone-shape/onAtDistance system).
// Skip vs Java: npc.getController().scheduleRespawn()/onDelete() (instant "corral" respawn visual)
// is a documented no-op — no NPC controller/respawn-scheduling infra in this port yet; the quest
// var still advances so progress isn't blocked.
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

namespace Quest.Brusthonin;

public sealed class _4077PorgusRoundup : QuestHandlerBase
{
    private const int QuestIdConst = 4077;
    private const int HolekkNpc    = 205158;
    private const int PorgusNpc    = 214732;

    private static readonly Position _penCenter = new(1356, 1901, 46, 0, 0);
    private const float PenRadius = 10f;

    public _4077PorgusRoundup(IDataManager dataManager, IQuestDao questDao, QuestRewardService rewardService, IItemDao itemDao)
        : base(QuestIdConst, dataManager, questDao, rewardService)
    {
    }

    public override void Register(QuestEngine engine)
    {
        engine.RegisterQuestNpc(HolekkNpc).OnQuestStart.Add(QuestId);
        engine.RegisterQuestNpc(HolekkNpc).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(PorgusNpc).OnAttack.Add(QuestId);
    }

    public override ValueTask<bool> OnAttackAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        if (env.TargetId != PorgusNpc) return ValueTask.FromResult(false);

        var entry = env.Player.Quests.Get(QuestId);
        if (entry is null || entry.Status != QuestStatus.START) return ValueTask.FromResult(false);
        if (env.Target is null || _penCenter.DistanceTo(env.Target.Position) > PenRadius) return ValueTask.FromResult(false);

        int var = entry.GetVar(0);
        if (var == 0)
        {
            entry.SetVar(0, var + 1);
            return AdvanceAsync(conn, entry, ct);
        }
        if (var == 1)
        {
            entry.Status = QuestStatus.REWARD;
            return AdvanceAsync(conn, entry, ct);
        }
        return ValueTask.FromResult(false);
    }

    private async ValueTask<bool> AdvanceAsync(GsClientConnection conn, QuestEntry entry, CancellationToken ct)
    {
        await UpdateQuestStatusAsync(conn, entry, ct);
        return true;
    }

    public override async ValueTask<bool> OnDialogAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var player = env.Player;
        var entry  = player.Quests.Get(QuestId);
        int targetId = env.TargetId;
        int targetObjId = env.Target?.ObjectId ?? 0;

        if (entry is null)
        {
            if (targetId == HolekkNpc)
            {
                if (DialogActionLookup.FromId(env.DialogId) == DialogAction.QUEST_SELECT)
                    return await SendQuestDialogAsync(conn, targetObjId, 4762, ct);
                return await SendQuestStartDialogAsync(env, conn, ct);
            }
            return false;
        }

        if (entry.Status == QuestStatus.REWARD && targetId == HolekkNpc)
            return await SendQuestEndDialogAsync(env, conn, ct);

        return false;
    }
}
