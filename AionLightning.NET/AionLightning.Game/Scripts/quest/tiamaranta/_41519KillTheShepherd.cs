// Port of Java data/scripts/system/handlers/quest/tiamaranta/_41519KillTheShepherd.java (Cheatkiller).
// Talk to 205938 to start; interact with 701316 to spawn 3 decorative "june" companions and
// advance (var 0->1); kill 218333 once var is 1 to flip to REWARD; return to 205914 to turn in.
// Skip vs Java: the spawned "june" companions are never despawned on kill (Java calls
// npc.getController().onDelete() on each) — no NPC despawn/lifecycle API is ported yet (see
// migration_plan.md's dieObject note on UseQuestObjectAsync for the same gap). Purely cosmetic;
// does not block quest completion.
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

namespace Quest.Tiamaranta;

public sealed class _41519KillTheShepherd : QuestHandlerBase
{
    private const int QuestIdConst  = 41519;
    private const int StartNpc      = 205938;
    private const int CompanionSpawnObj = 701316;
    private const int JuneNpc       = 701261;
    private const int ShepherdNpc   = 218333;
    private const int TurnInNpc     = 205914;

    public _41519KillTheShepherd(IDataManager dataManager, IQuestDao questDao, QuestRewardService rewardService, IItemDao itemDao)
        : base(QuestIdConst, dataManager, questDao, rewardService)
    {
    }

    public override void Register(QuestEngine engine)
    {
        engine.RegisterQuestNpc(StartNpc).OnQuestStart.Add(QuestId);
        engine.RegisterQuestNpc(CompanionSpawnObj).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(ShepherdNpc).OnKill.Add(QuestId);
        engine.RegisterQuestNpc(TurnInNpc).OnTalk.Add(QuestId);
    }

    public override async ValueTask<bool> OnDialogAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var player = env.Player;
        var entry  = player.Quests.Get(QuestId);
        int targetId = env.TargetId;
        int targetObjId = env.Target?.ObjectId ?? 0;
        var dialog = DialogActionLookup.FromId(env.DialogId);

        if (entry is null || entry.Status == QuestStatus.NONE)
        {
            if (targetId != StartNpc) return false;
            if (dialog == DialogAction.QUEST_SELECT)
                return await SendQuestDialogAsync(conn, targetObjId, 4762, ct);
            return await SendQuestStartDialogAsync(env, conn, ct);
        }

        if (entry.Status == QuestStatus.START && targetId == CompanionSpawnObj)
        {
            var pos = env.Target!.Position;
            SpawnQuestNpc(pos.WorldId, pos.InstanceId, JuneNpc, pos.X + 2, pos.Y + 2, pos.Z, (byte)pos.Heading);
            SpawnQuestNpc(pos.WorldId, pos.InstanceId, JuneNpc, pos.X - 2, pos.Y + 2, pos.Z, (byte)pos.Heading);
            SpawnQuestNpc(pos.WorldId, pos.InstanceId, JuneNpc, pos.X - 2, pos.Y - 2, pos.Z, (byte)pos.Heading);
            return await UseQuestObjectAsync(env, conn, 0, 1, reward: false, dieObject: false, ct);
        }

        if (entry.Status == QuestStatus.REWARD && targetId == TurnInNpc)
        {
            if (dialog == DialogAction.USE_OBJECT)
                return await SendQuestDialogAsync(conn, targetObjId, 10002, ct);
            return await SendQuestEndDialogAsync(env, conn, ct);
        }
        return false;
    }

    public override async ValueTask<bool> OnKillAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var entry = env.Player.Quests.Get(QuestId);
        if (entry is null || entry.Status != QuestStatus.START || entry.GetVar(0) != 1) return false;
        return await DefaultOnKillEventAsync(env, conn, ShepherdNpc, 1, reward: true, ct);
    }
}
