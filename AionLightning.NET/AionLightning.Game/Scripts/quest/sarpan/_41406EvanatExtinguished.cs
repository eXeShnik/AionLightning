// Port of Java data/scripts/system/handlers/quest/sarpan/_41406EvanatExtinguished.java (Cheatkiller).
// Auto-start/complete: engaging mob 282920 starts the quest, killing it finishes it (reward index 0).
// Java's onAddAggroListEvent (broadcast to every nearby player once the mob's aggro list gains an
// entry) has no direct port-side equivalent; approximated with the Batch 0.3 OnAttackAsync hook
// (fires for the attacking player only, on each hit while the mob is alive) — functionally
// equivalent here since the goal is "auto-start on engagement". Java's qs.canRepeat() daily-reset
// gate isn't ported (no repeat-cooldown infra yet); the quest completes once per character like the
// rest of this port.
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

namespace Quest.Sarpan;

public sealed class _41406EvanatExtinguished : QuestHandlerBase
{
    private const int QuestIdConst = 41406;
    private const int MobId        = 282920;

    public _41406EvanatExtinguished(IDataManager dataManager, IQuestDao questDao, QuestRewardService rewardService, IItemDao itemDao)
        : base(QuestIdConst, dataManager, questDao, rewardService)
    {
    }

    public override void Register(QuestEngine engine)
    {
        var npc = engine.RegisterQuestNpc(MobId);
        npc.OnAttack.Add(QuestId);
        npc.OnKill.Add(QuestId);
    }

    public override async ValueTask<bool> OnAttackAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var player = env.Player;
        if (player.Quests.Get(QuestId) is not null) return true;
        await StartMissionAsync(conn, player, QuestStatus.START, ct);
        return true;
    }

    public override async ValueTask<bool> OnKillAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var entry = env.Player.Quests.Get(QuestId);
        if (entry is null || entry.Status != QuestStatus.START || env.TargetId != MobId) return false;

        await ChangeQuestStepAsync(conn, entry, varIdx: -1, newValue: 0, toReward: true, ct);
        return await FinishQuestAsync(conn, env.Player, 0, ct);
    }
}
