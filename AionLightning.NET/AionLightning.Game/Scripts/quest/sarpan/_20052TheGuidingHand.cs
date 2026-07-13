// Port of Java data/scripts/system/handlers/quest/sarpan/_20052TheGuidingHand.java (vlog).
// Elyos mirror of sarpan's _10052TheProtector: zone-mission-chain quest (no OnQuestStart npc -
// started by 20051's dialog/level-up chain): enter SARPAN_CAPITOL_600020000 to flip 0->1 (movie
// 704); Garnon (205987) var 1->2; Nytia (205788) var 2->3; kill Guard (218100) 3->4; enter
// DEBARIM_PETRALITH_STUDIO_600020000 at var 5 to flip 5->6 (movie 706); read Zayedan's Record
// (730474) 6->7; Ispharel (205988) flips to REWARD; turn in at Aimah (205617).
// Skip vs Java: Garnon's SETPRO5 branch calls TeleportService2.teleportTo(...) - no TeleportService
// exists in this port, same simplification as sarpan's _10051UnwelcomeMessengers/_10052TheProtector.
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

public sealed class _20052TheGuidingHand : QuestHandlerBase
{
    private const int QuestIdConst      = 20052;
    private const int GarnonNpc         = 205987;
    private const int NytiaNpc          = 205788;
    private const int ZayedansRecordNpc = 730474;
    private const int IspharelNpc       = 205988;
    private const int AimahNpc          = 205617;
    private const int GuardMobId        = 218100;
    private const string SarpanCapitolZone   = "SARPAN_CAPITOL_600020000";
    private const string DebarimPetralithZone = "DEBARIM_PETRALITH_STUDIO_600020000";

    public _20052TheGuidingHand(IDataManager dataManager, IQuestDao questDao, QuestRewardService rewardService, IItemDao itemDao)
        : base(QuestIdConst, dataManager, questDao, rewardService)
    {
    }

    public override void Register(QuestEngine engine)
    {
        engine.RegisterQuestNpc(GarnonNpc).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(NytiaNpc).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(ZayedansRecordNpc).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(IspharelNpc).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(AimahNpc).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(GuardMobId).OnKill.Add(QuestId);
        RegisterOnEnterZone(engine, SarpanCapitolZone);
        RegisterOnEnterZone(engine, DebarimPetralithZone);
        engine.RegisterOnLevelUp(QuestId);
        engine.RegisterOnZoneMissionEnd(QuestId);
    }

    public override ValueTask<bool> OnZoneMissionEndAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
        => DefaultOnZoneMissionEndEventAsync(env, conn, ct);

    public override ValueTask<bool> OnLevelUpAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
        => DefaultOnLvlUpEventAsync(env, conn, precedingQuestId: 20051, isZoneMission: true, ct);

    public override async ValueTask<bool> OnDialogAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var entry = env.Player.Quests.Get(QuestId);
        if (entry is null) return false;

        var dialog      = DialogActionLookup.FromId(env.DialogId);
        int targetId    = env.TargetId;
        int targetObjId = env.Target?.ObjectId ?? 0;

        if (entry.Status == QuestStatus.START)
        {
            int var = entry.GetVar(0);

            if (targetId == GarnonNpc)
            {
                if (dialog == DialogAction.QUEST_SELECT && var == 1) return await SendQuestDialogAsync(conn, targetObjId, 1352, ct);
                if (dialog == DialogAction.QUEST_SELECT && var == 4) return await SendQuestDialogAsync(conn, targetObjId, 2375, ct);
                if (dialog == DialogAction.SETPRO2) return await DefaultCloseDialogAsync(env, conn, 1, 2, ct);
                if (dialog == DialogAction.SETPRO5)
                {
                    // Skip vs Java: TeleportService2.teleportTo(...) - no TeleportService in this port.
                    return await DefaultCloseDialogAsync(env, conn, 4, 5, ct);
                }
                return false;
            }
            if (targetId == NytiaNpc)
            {
                if (dialog == DialogAction.QUEST_SELECT && var == 2) return await SendQuestDialogAsync(conn, targetObjId, 1693, ct);
                if (dialog == DialogAction.SETPRO3) return await DefaultCloseDialogAsync(env, conn, 2, 3, ct);
                return false;
            }
            if (targetId == ZayedansRecordNpc)
            {
                if (dialog == DialogAction.USE_OBJECT && var == 6) return await SendQuestDialogAsync(conn, targetObjId, 3057, ct);
                if (dialog == DialogAction.SETPRO7)
                {
                    if (var == 6) await ChangeQuestStepAsync(conn, entry, 0, 7, toReward: false, ct);
                    return await CloseDialogWindowAsync(conn, targetObjId, ct);
                }
                return false;
            }
            if (targetId == IspharelNpc)
            {
                if (dialog == DialogAction.QUEST_SELECT && var == 7) return await SendQuestDialogAsync(conn, targetObjId, 3398, ct);
                if (dialog == DialogAction.SET_SUCCEED) return await DefaultCloseDialogAsync(env, conn, 7, 7, reward: true, sameNpc: false, ct);
                return false;
            }
            return false;
        }

        if (entry.Status == QuestStatus.REWARD && targetId == AimahNpc)
        {
            if (dialog == DialogAction.USE_OBJECT) return await SendQuestDialogAsync(conn, targetObjId, 10002, ct);
            return await SendQuestEndDialogAsync(env, conn, ct);
        }

        return false;
    }

    public override async ValueTask<bool> OnEnterZoneAsync(QuestEnv env, string zoneName, GsClientConnection conn, CancellationToken ct)
    {
        if (zoneName != SarpanCapitolZone && zoneName != DebarimPetralithZone) return false;

        var entry = env.Player.Quests.Get(QuestId);
        if (entry is null || entry.Status != QuestStatus.START) return false;
        int var = entry.GetVar(0);

        if (zoneName == SarpanCapitolZone && var == 0)
        {
            await ChangeQuestStepAsync(conn, entry, 0, 1, toReward: false, ct);
            await PlayQuestMovieAsync(conn, env.Player, 704, ct);
            return true;
        }
        if (zoneName == DebarimPetralithZone && var == 5)
        {
            await ChangeQuestStepAsync(conn, entry, 0, 6, toReward: false, ct);
            await PlayQuestMovieAsync(conn, env.Player, 706, ct);
            return true;
        }
        return false;
    }

    public override ValueTask<bool> OnKillAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
        => DefaultOnKillEventAsync(env, conn, GuardMobId, 3, 4, ct);
}
