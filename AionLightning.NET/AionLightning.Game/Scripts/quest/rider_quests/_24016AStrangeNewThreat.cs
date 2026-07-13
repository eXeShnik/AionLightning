// Port of Java data/scripts/system/handlers/quest/rider_quests/_24016AStrangeNewThreat.java (pralinka).
// Altgard zone-mission (level-up gated on 24011-24015): talk Suthran (203557, var0 0->1); dialog id
// 1013 plays movie 66; enter the instanced gate world 320030000 (portal-driven) where onEnterWorld
// advances var0 1->2; USE the Gate Guardian Stone (700140, var0==2) spawns boss 233876 and advances
// var0 2->3; kill 233876 (var0 3->4); USE the stone again (var0==4) plays movie 154; turn in at
// Suthran. Dying while var0 >= 2 reverts to var0 1. In-instance spawn uses SpawnQuestNpc; player-
// death revert uses OnDieAsync.
// Java quirk preserved: registerOnMovieEndQuest(154) is registered but the Java handler has no
// onMovieEndEvent override, so movie 154 has no follow-up transition — the quest reaches REWARD via
// its quest_data.xml completion, not a movie-end hook. Registration is kept for parity (no-op here).
// Skip vs Java: Suthran's SETPRO1 TeleportService2 relocation to Altgard (220030000) dropped —
// non-entry relocation, the var0 0->1 transition is kept.
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

namespace Quest.RiderQuests;

public sealed class _24016AStrangeNewThreat : QuestHandlerBase
{
    private const int QuestIdConst = 24016;
    private const int SuthranNpc   = 203557;
    private const int GateStoneObj = 700140;
    private const int BossNpc      = 233876;
    private const int InstanceWorld = 320030000;

    private static readonly int[] _altgardQuests = [24011, 24012, 24013, 24014, 24015];

    public _24016AStrangeNewThreat(IDataManager dataManager, IQuestDao questDao, QuestRewardService rewardService, IItemDao itemDao)
        : base(QuestIdConst, dataManager, questDao, rewardService)
    {
    }

    public override void Register(QuestEngine engine)
    {
        engine.RegisterOnZoneMissionEnd(QuestId);
        engine.RegisterOnLevelUp(QuestId);
        RegisterOnDie(engine);
        engine.RegisterOnEnterWorld(QuestId);
        engine.RegisterOnQuestMovieEnd(154, QuestId);
        engine.RegisterQuestNpc(SuthranNpc).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(GateStoneObj).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(BossNpc).OnKill.Add(QuestId);
    }

    public override ValueTask<bool> OnZoneMissionEndAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
        => DefaultOnZoneMissionEndEventAsync(env, conn, ct);

    public override ValueTask<bool> OnLevelUpAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
        => DefaultOnLvlUpEventAsync(env, conn, _altgardQuests, isZoneMission: true, ct);

    public override async ValueTask<bool> OnDialogAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var player = env.Player;
        var entry  = player.Quests.Get(QuestId);
        if (entry is null) return false;

        int targetId    = env.TargetId;
        int targetObjId = env.Target?.ObjectId ?? 0;
        var dialog      = DialogActionLookup.FromId(env.DialogId);
        int var         = entry.GetVar(0);

        if (entry.Status == QuestStatus.START)
        {
            if (targetId == SuthranNpc)
            {
                if (dialog == DialogAction.QUEST_SELECT && var == 0)
                    return await SendQuestDialogAsync(conn, targetObjId, 1011, ct);
                if (dialog == DialogAction.SETPRO1)
                {
                    // note: TeleportService2 relocation to Altgard (220030000) dropped — non-entry relocation; var0 0->1 kept
                    await ChangeQuestStepAsync(conn, entry, 0, 1, toReward: false, ct);
                    return await CloseDialogWindowAsync(conn, targetObjId, ct);
                }
                if (env.DialogId == 1013)
                {
                    await PlayQuestMovieAsync(conn, player, 66, ct);
                    return await SendQuestDialogAsync(conn, targetObjId, 1013, ct);
                }
                return false;
            }
            if (targetId == GateStoneObj)
            {
                if (var == 2 && dialog == DialogAction.USE_OBJECT)
                {
                    SpawnQuestNpc(InstanceWorld, player.Position.InstanceId, BossNpc, 260.12f, 234.93f, 216.00f, 90);
                    return await UseQuestObjectAsync(env, conn, 2, 3, reward: false, dieObject: false, ct);
                }
                if (var == 4 && dialog == DialogAction.USE_OBJECT)
                {
                    await PlayQuestMovieAsync(conn, player, 154, ct);
                    return true;
                }
            }
        }
        else if (entry.Status == QuestStatus.REWARD)
        {
            if (targetId == SuthranNpc)
            {
                if (dialog == DialogAction.USE_OBJECT) return await SendQuestDialogAsync(conn, targetObjId, 1352, ct);
                return await SendQuestEndDialogAsync(env, conn, ct);
            }
        }
        return false;
    }

    public override ValueTask<bool> OnKillAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
        => DefaultOnKillEventAsync(env, conn, BossNpc, 3, 4, ct);

    public override async ValueTask<bool> OnDieAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var entry = env.Player.Quests.Get(QuestId);
        if (entry is null || entry.Status != QuestStatus.START) return false;
        if (entry.GetVar(0) >= 2)
        {
            entry.SetVar(0, 1);
            await UpdateQuestStatusAsync(conn, entry, ct);
            return true;
        }
        return false;
    }

    public override async ValueTask<bool> OnEnterWorldAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var player = env.Player;
        var entry  = player.Quests.Get(QuestId);
        if (entry is null || entry.Status != QuestStatus.START) return false;

        int var = entry.GetVar(0);
        if (var >= 2 && player.Position.WorldId != InstanceWorld)
        {
            await ChangeQuestStepAsync(conn, entry, 0, 1, toReward: false, ct);
            return true;
        }
        if (var == 1 && player.Position.WorldId == InstanceWorld)
        {
            await ChangeQuestStepAsync(conn, entry, 0, 2, toReward: false, ct);
            return true;
        }
        return false;
    }
}
