// Port of Java data/scripts/system/handlers/quest/black_cloud_traders/_39505BackbitingBotheration.java
// (Cheatkiller). Kill one of 6 mobs for a 20% chance to despawn it and spawn the relay npc 701153 at
// its position; talk to 701153 (var 0->1), then to 205628 to flip to REWARD and turn in.
// Skip vs Java: Npc.getController().onDelete() (a despawn visual on the killed mob / on the relay
// npc once it's talked to) is omitted — no NPC controller/AI subsystem exists in this port; the
// var/status transitions and the replacement spawn itself are unaffected.
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

public sealed class _39505BackbitingBotheration : QuestHandlerBase
{
    private const int QuestIdConst = 39505;
    private const int FinalNpc     = 205628;
    private const int RelayNpc     = 701153;

    private static readonly int[] Mobs = [218600, 218602, 218023, 218024, 218025, 218022];

    public _39505BackbitingBotheration(IDataManager dataManager, IQuestDao questDao, QuestRewardService rewardService, IItemDao itemDao)
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
