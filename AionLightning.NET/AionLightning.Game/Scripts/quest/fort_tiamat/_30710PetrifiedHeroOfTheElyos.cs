// Port of Java data/scripts/system/handlers/quest/fort_tiamat/_30710PetrifiedHeroOfTheElyos.java (Cheatkiller).
// Accept at 800066 (page 4762); interacting with 701498 while START spawns npc 800382 at the
// player's current position (Java QuestService.addNewSpawn) and immediately completes (var0->1,
// reward) — no dialog-action guard in the original, so any dialog fired at 701498 during START
// triggers it. Turn in back at 800066.
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

namespace Quest.FortTiamat;

public sealed class _30710PetrifiedHeroOfTheElyos : QuestHandlerBase
{
    private const int QuestIdConst = 30710;
    private const int StartNpc     = 800066;
    private const int InteractNpc  = 701498;
    private const int SpawnNpc     = 800382;

    public _30710PetrifiedHeroOfTheElyos(IDataManager dataManager, IQuestDao questDao, QuestRewardService rewardService, IItemDao itemDao)
        : base(QuestIdConst, dataManager, questDao, rewardService)
    {
    }

    public override void Register(QuestEngine engine)
    {
        engine.RegisterQuestNpc(StartNpc).OnQuestStart.Add(QuestId);
        engine.RegisterQuestNpc(StartNpc).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(InteractNpc).OnTalk.Add(QuestId);
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
            if (dialog == DialogAction.QUEST_SELECT)
                return await SendQuestDialogAsync(conn, targetObjId, 4762, ct);
            return await SendQuestStartDialogAsync(env, conn, ct);
        }

        if (entry.Status == QuestStatus.START && targetId == InteractNpc)
        {
            var pos = player.Position;
            SpawnQuestNpc(pos.WorldId, pos.InstanceId, SpawnNpc, pos.X, pos.Y, pos.Z, 0);
            return await UseQuestObjectAsync(env, conn, 0, 1, reward: true, dieObject: false, ct);
        }

        if (entry.Status == QuestStatus.REWARD && targetId == StartNpc)
        {
            if (dialog == DialogAction.USE_OBJECT)
                return await SendQuestDialogAsync(conn, targetObjId, 10002, ct);
            return await SendQuestEndDialogAsync(env, conn, ct);
        }

        return false;
    }
}
