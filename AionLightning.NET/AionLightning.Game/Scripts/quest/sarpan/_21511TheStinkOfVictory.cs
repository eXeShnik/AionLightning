// Port of Java data/scripts/system/handlers/quest/sarpan/_21511TheStinkOfVictory.java (Cheatkiller).
// Talk to 205782 to start (no item); talk to 730469 to spawn mob 218651 at the player's position
// (unconditional while quest is at START, no var gate — ported 1:1); kill 218651 (var 0->1);
// return to 205782 (var==1, SELECT_QUEST_REWARD flips to REWARD, same npc); turn in at 205782.
// Skip vs Java: the source npc's scheduleRespawn()/onDelete() controller calls (no Npc AI/
// controller subsystem in this port yet) — the underlying spawn/kill/reward flow is unaffected,
// this only drops the despawn/respawn visual on npc 730469.
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

public sealed class _21511TheStinkOfVictory : QuestHandlerBase
{
    private const int QuestIdConst = 21511;
    private const int StartNpc      = 205782;
    private const int SpawnerNpc    = 730469;
    private const int MobId         = 218651;

    public _21511TheStinkOfVictory(IDataManager dataManager, IQuestDao questDao, QuestRewardService rewardService, IItemDao itemDao)
        : base(QuestIdConst, dataManager, questDao, rewardService)
    {
    }

    public override void Register(QuestEngine engine)
    {
        engine.RegisterQuestNpc(StartNpc).OnQuestStart.Add(QuestId);
        engine.RegisterQuestNpc(StartNpc).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(SpawnerNpc).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(MobId).OnKill.Add(QuestId);
    }

    public override ValueTask<bool> OnKillAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
        => DefaultOnKillEventAsync(env, conn, MobId, 0, 1, ct);

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
            if (dialog == DialogAction.QUEST_SELECT) return await SendQuestDialogAsync(conn, targetObjId, 1011, ct);
            return await SendQuestStartDialogAsync(env, conn, ct);
        }

        if (entry.Status == QuestStatus.START)
        {
            if (targetId == StartNpc)
            {
                if (dialog == DialogAction.QUEST_SELECT) return await SendQuestDialogAsync(conn, targetObjId, 1352, ct);
                if (dialog == DialogAction.SELECT_QUEST_REWARD)
                    return await DefaultCloseDialogAsync(env, conn, 1, 1, reward: true, sameNpc: true, ct);
                return false;
            }
            if (targetId == SpawnerNpc)
            {
                var pos = player.Position;
                SpawnQuestNpc(pos.WorldId, pos.InstanceId, MobId, pos.X, pos.Y, pos.Z, (byte)pos.Heading);
                return true;
            }
            return false;
        }

        if (entry.Status == QuestStatus.REWARD && targetId == StartNpc)
        {
            if (dialog == DialogAction.USE_OBJECT) return await SendQuestDialogAsync(conn, targetObjId, 1352, ct);
            return await SendQuestEndDialogAsync(env, conn, ct);
        }

        return false;
    }
}
