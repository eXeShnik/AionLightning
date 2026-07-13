// Port of Java data/scripts/system/handlers/quest/inggison/_11147CuteBeadyEyes.java (Cheatkiller).
// Talk to 798997 to start; SETPRO1 spawns two mobs (799079, 799081) at the player's position and
// advances var 0->1; talking to 798997 again advances var 1->2; 799079 advances var 2->3; 799081
// advances var 3->4; entering KLAWNICKTS_CAVE_210050000 at var 4 flips to REWARD; turn in at 798997.
// Skip vs Java: SETPRO3/SETPRO4 also call npc.getController().onDelete() on the spawned mobs after
// talking to them - no NPC despawn API is exposed to hand-written quest scripts in this port, so the
// placeholder mobs are left standing (same simplification as sarpan._11512LoversLost).
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

namespace Quest.Inggison;

public sealed class _11147CuteBeadyEyes : QuestHandlerBase
{
    private const int QuestIdConst = 11147;
    private const int StartNpc  = 798997;
    private const int FirstMob  = 799079;
    private const int SecondMob = 799081;
    private const string EnterZoneName = "KLAWNICKTS_CAVE_210050000";

    public _11147CuteBeadyEyes(IDataManager dataManager, IQuestDao questDao, QuestRewardService rewardService, IItemDao itemDao)
        : base(QuestIdConst, dataManager, questDao, rewardService)
    {
    }

    public override void Register(QuestEngine engine)
    {
        engine.RegisterQuestNpc(StartNpc).OnQuestStart.Add(QuestId);
        engine.RegisterQuestNpc(StartNpc).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(FirstMob).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(SecondMob).OnTalk.Add(QuestId);
        RegisterOnEnterZone(engine, EnterZoneName);
    }

    public override async ValueTask<bool> OnDialogAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var player = env.Player;
        var entry = player.Quests.Get(QuestId);
        int targetId = env.TargetId;
        int targetObjId = env.Target?.ObjectId ?? 0;
        var dialog = DialogActionLookup.FromId(env.DialogId);

        if (entry is null || entry.Status == QuestStatus.NONE)
        {
            if (targetId != StartNpc) return false;
            if (dialog == DialogAction.QUEST_SELECT) return await SendQuestDialogAsync(conn, targetObjId, 4762, ct);
            return await SendQuestStartDialogAsync(env, conn, ct);
        }

        if (entry.Status == QuestStatus.START)
        {
            int var = entry.GetVar(0);

            if (targetId == StartNpc)
            {
                if (dialog == DialogAction.QUEST_SELECT)
                {
                    if (var == 0) return await SendQuestDialogAsync(conn, targetObjId, 1011, ct);
                    if (var == 1) return await SendQuestDialogAsync(conn, targetObjId, 1352, ct);
                }
                if (dialog == DialogAction.SETPRO1 && var == 0)
                {
                    var pos = player.Position;
                    SpawnQuestNpc(pos.WorldId, pos.InstanceId, FirstMob, pos.X, pos.Y, pos.Z, 0);
                    SpawnQuestNpc(pos.WorldId, pos.InstanceId, SecondMob, pos.X - 1, pos.Y + 2, pos.Z, 0);
                    return await DefaultCloseDialogAsync(env, conn, 0, 1, ct);
                }
                if (dialog == DialogAction.SETPRO2 && var == 1)
                    return await DefaultCloseDialogAsync(env, conn, 1, 2, ct);
                return false;
            }
            if (targetId == FirstMob)
            {
                if (dialog == DialogAction.QUEST_SELECT) return await SendQuestDialogAsync(conn, targetObjId, 1693, ct);
                if (dialog == DialogAction.SETPRO3) return await DefaultCloseDialogAsync(env, conn, 2, 3, ct);
                return false;
            }
            if (targetId == SecondMob)
            {
                if (dialog == DialogAction.QUEST_SELECT) return await SendQuestDialogAsync(conn, targetObjId, 2034, ct);
                if (dialog == DialogAction.SETPRO4) return await DefaultCloseDialogAsync(env, conn, 3, 4, ct);
                return false;
            }
        }
        else if (entry.Status == QuestStatus.REWARD)
        {
            if (targetId != StartNpc) return false;
            if (dialog == DialogAction.USE_OBJECT) return await SendQuestDialogAsync(conn, targetObjId, 10002, ct);
            return await SendQuestEndDialogAsync(env, conn, ct);
        }
        return false;
    }

    public override async ValueTask<bool> OnEnterZoneAsync(QuestEnv env, string zoneName, GsClientConnection conn, CancellationToken ct)
    {
        if (zoneName != EnterZoneName) return false;

        var entry = env.Player.Quests.Get(QuestId);
        if (entry is null || entry.Status != QuestStatus.START) return false;
        if (entry.GetVar(0) != 4) return false;

        await ChangeQuestStepAsync(conn, entry, 0, 4, toReward: true, ct);
        return true;
    }
}
