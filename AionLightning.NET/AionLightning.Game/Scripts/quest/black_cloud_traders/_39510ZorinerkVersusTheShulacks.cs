// Port of Java data/scripts/system/handlers/quest/black_cloud_traders/_39510ZorinerkVersusTheShulacks.java
// (Cheatkiller). Structurally identical to _39505BackbitingBotheration (see that file's header for
// the documented Npc.getController().onDelete() skip): kill one of 3 mobs for a 20% chance to spawn
// the relay npc 205983; talk to it (var 0->1), then to 205629 to flip to REWARD and turn in.
using System;
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

namespace Quest.BlackCloudTraders;

public sealed class _39510ZorinerkVersusTheShulacks : QuestHandlerBase
{
    private const int QuestIdConst = 39510;
    private const int FinalNpc     = 205629;
    private const int RelayNpc     = 205983;

    private static readonly int[] Mobs = [218053, 218055, 218057];

    public _39510ZorinerkVersusTheShulacks(IDataManager dataManager, IQuestDao questDao, QuestRewardService rewardService, IItemDao itemDao)
        : base(QuestIdConst, dataManager, questDao, rewardService)
    {
    }

    public override void Register(QuestEngine engine)
    {
        engine.RegisterQuestNpc(FinalNpc).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(RelayNpc).OnTalk.Add(QuestId);
        foreach (int mob in Mobs)
            engine.RegisterQuestNpc(mob).OnKill.Add(QuestId);
    }

    public override ValueTask<bool> OnKillAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var entry = env.Player.Quests.Get(QuestId);
        if (entry is null || entry.Status != QuestStatus.START) return ValueTask.FromResult(false);
        if (env.Target is null || Random.Shared.Next(1, 101) >= 20) return ValueTask.FromResult(false);

        var pos = env.Target.Position;
        SpawnQuestNpc(pos.WorldId, pos.InstanceId, RelayNpc, pos.X, pos.Y, pos.Z, (byte)pos.Heading);
        return ValueTask.FromResult(true);
    }

    public override async ValueTask<bool> OnDialogAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var player      = env.Player;
        int targetId    = env.TargetId;
        int targetObjId = env.Target?.ObjectId ?? 0;
        var dialog      = DialogActionLookup.FromId(env.DialogId);

        if (targetId == 0)
        {
            if (dialog == DialogAction.QUEST_ACCEPT_1)
                await StartMissionAsync(conn, player, QuestStatus.START, ct);
            return await CloseDialogWindowAsync(conn, 0, ct);
        }

        var entry = player.Quests.Get(QuestId);
        if (entry is null) return false;

        if (entry.Status == QuestStatus.START)
        {
            if (targetId == RelayNpc)
            {
                if (dialog == DialogAction.USE_OBJECT)
                    return await SendQuestDialogAsync(conn, targetObjId, 1352, ct);
                if (dialog == DialogAction.SETPRO1)
                    return await DefaultCloseDialogAsync(env, conn, 0, 1, ct);
            }
            else if (targetId == FinalNpc)
            {
                if (dialog == DialogAction.USE_OBJECT)
                    return await SendQuestDialogAsync(conn, targetObjId, 2375, ct);
                if (dialog == DialogAction.SELECT_QUEST_REWARD)
                {
                    await ChangeQuestStepAsync(conn, entry, 1, 1, toReward: true, ct);
                    return await SendQuestDialogAsync(conn, targetObjId, 5, ct);
                }
            }
            return false;
        }

        if (entry.Status == QuestStatus.REWARD && targetId == FinalNpc)
        {
            if (dialog == DialogAction.USE_OBJECT)
                return await SendQuestDialogAsync(conn, targetObjId, 2375, ct);
            return await SendQuestEndDialogAsync(env, conn, ct);
        }

        return false;
    }
}
