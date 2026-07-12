// Port of Java data/scripts/system/handlers/quest/sarpan/_41204BreakItTwiceShameOnMe.java
// (Cheatkiller). Talk to 205785 to start; use the object 701161 (var 0->1), which spawns a mob
// (218658) at the player's position; kill it to flip to REWARD; turn in at 205785.
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

public sealed class _41204BreakItTwiceShameOnMe : QuestHandlerBase
{
    private const int QuestIdConst = 41204;
    private const int StartNpc     = 205785;
    private const int TargetObjectNpc = 701161;
    private const int MobId        = 218658;

    public _41204BreakItTwiceShameOnMe(IDataManager dataManager, IQuestDao questDao, QuestRewardService rewardService, IItemDao itemDao)
        : base(QuestIdConst, dataManager, questDao, rewardService)
    {
    }

    public override void Register(QuestEngine engine)
    {
        engine.RegisterQuestNpc(StartNpc).OnQuestStart.Add(QuestId);
        engine.RegisterQuestNpc(StartNpc).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(TargetObjectNpc).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(MobId).OnKill.Add(QuestId);
    }

    public override ValueTask<bool> OnKillAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
        => DefaultOnKillEventAsync(env, conn, MobId, startVar: 1, reward: true, ct);

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
            if (dialog == DialogAction.QUEST_SELECT) return await SendQuestDialogAsync(conn, targetObjId, 4762, ct);
            return await SendQuestStartDialogAsync(env, conn, ct);
        }

        if (entry.Status == QuestStatus.START && targetId == TargetObjectNpc)
        {
            if (dialog == DialogAction.USE_OBJECT && entry.GetVar(0) == 0) return await SendQuestDialogAsync(conn, targetObjId, 1011, ct);
            if (dialog == DialogAction.SETPRO1)
            {
                var pos = player.Position;
                SpawnQuestNpc(pos.WorldId, pos.InstanceId, MobId, pos.X, pos.Y, pos.Z, (byte)pos.Heading);
                return await DefaultCloseDialogAsync(env, conn, 0, 1, ct);
            }
            return false;
        }

        if (entry.Status == QuestStatus.REWARD && targetId == StartNpc)
        {
            if (dialog == DialogAction.USE_OBJECT) return await SendQuestDialogAsync(conn, targetObjId, 10002, ct);
            return await SendQuestEndDialogAsync(env, conn, ct);
        }

        return false;
    }
}
