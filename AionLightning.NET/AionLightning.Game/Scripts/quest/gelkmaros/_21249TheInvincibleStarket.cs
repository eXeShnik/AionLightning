// Port of Java data/scripts/system/handlers/quest/gelkmaros/_21249TheInvincibleStarket.java (Cheatkiller).
// Talk to 799416 to start; SETPRO1 spawns 799529 at the player's position via the Batch 0.2
// SpawnQuestNpc primitive (var 0->1); talk to the spawned 799529 (SET_SUCCEED, reward flip);
// turn in at 799417.
// Skip vs Java: the source npc's scheduleRespawn()/onDelete() calls (both occurrences) are
// dropped — no Npc AI/controller subsystem in this port yet, this only drops the despawn visual.
// Java's redundant `changeQuestStep(env, 0, 1, false)` immediately before the reward-flip
// defaultCloseDialog(1, 1, ...) call is a guarded no-op in Java (var is already 1 by then, so the
// step==0 guard always fails) — omitted here as dead code rather than replicated as an unguarded
// duplicate write (this port's ChangeQuestStepAsync has no current-value guard).
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

namespace Quest.Gelkmaros;

public sealed class _21249TheInvincibleStarket : QuestHandlerBase
{
    private const int QuestIdConst = 21249;
    private const int StartNpc     = 799416;
    private const int SpawnedNpc   = 799529;
    private const int FinalNpc     = 799417;

    public _21249TheInvincibleStarket(IDataManager dataManager, IQuestDao questDao, QuestRewardService rewardService, IItemDao itemDao)
        : base(QuestIdConst, dataManager, questDao, rewardService)
    {
    }

    public override void Register(QuestEngine engine)
    {
        engine.RegisterQuestNpc(StartNpc).OnQuestStart.Add(QuestId);
        engine.RegisterQuestNpc(SpawnedNpc).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(StartNpc).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(FinalNpc).OnTalk.Add(QuestId);
    }

    public override async ValueTask<bool> OnDialogAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var player = env.Player;
        var entry  = player.Quests.Get(QuestId);
        int targetId = env.TargetId;
        int targetObjId = env.Target?.ObjectId ?? 0;
        var dialog = DialogActionLookup.FromId(env.DialogId);

        if (entry is null)
        {
            if (targetId == StartNpc)
            {
                if (dialog == DialogAction.QUEST_SELECT)
                    return await SendQuestDialogAsync(conn, targetObjId, 4762, ct);
                return await SendQuestStartDialogAsync(env, conn, ct);
            }
            return false;
        }

        if (entry.Status == QuestStatus.START)
        {
            if (targetId == StartNpc)
            {
                if (dialog == DialogAction.QUEST_SELECT && entry.GetVar(0) == 0)
                    return await SendQuestDialogAsync(conn, targetObjId, 1011, ct);
                if (dialog == DialogAction.SETPRO1)
                {
                    SpawnQuestNpc(player.Position.WorldId, player.Position.InstanceId, SpawnedNpc,
                        player.Position.X, player.Position.Y, player.Position.Z, (byte)player.Position.Heading);
                    return await DefaultCloseDialogAsync(env, conn, 0, 1, ct);
                }
                return false;
            }

            if (targetId == SpawnedNpc)
            {
                if (dialog == DialogAction.QUEST_SELECT && entry.GetVar(0) == 1)
                    return await SendQuestDialogAsync(conn, targetObjId, 1352, ct);
                if (dialog == DialogAction.SET_SUCCEED)
                    return await DefaultCloseDialogAsync(env, conn, 1, 1, reward: true, sameNpc: false, ct);
                return false;
            }
        }

        if (entry.Status == QuestStatus.REWARD && targetId == FinalNpc)
        {
            if (dialog == DialogAction.USE_OBJECT)
                return await SendQuestDialogAsync(conn, targetObjId, 10002, ct);
            return await SendQuestEndDialogAsync(env, conn, ct);
        }

        return false;
    }
}
