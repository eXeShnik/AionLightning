// Port of Java data/scripts/system/handlers/quest/rider_quests/_24015TotemPlowed.java (pralinka).
// Zone-mission sub-quest of 24010: talk to 203669 (var0 0->1, applies a disguise effect), enter the
// Black Claw Outpost zone (var0 1->2, removes the disguise effect), kill 2 Black Claw mobs (var0
// 2->4 then straight to REWARD), turn in at 203557 (Suthran).
// Skip vs Java: the disguise buff (SkillEngine.applyEffectDirectly(1868, ...) on SETPRO1 /
// player.getEffectController().removeEffect(1868) on zone entry) is cosmetic combat flavor with no
// completion-gating role and is omitted - no direct-effect-apply/remove service ported (same
// precedent as quest/altgard/_2021KnowYourEnemy.cs). The var/status transitions are kept.
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

public sealed class _24015TotemPlowed : QuestHandlerBase
{
    private const int QuestIdConst = 24015;
    private const int OutpostNpc = 203669;
    private const int SuthranNpc = 203557;
    private const int OutpostMob = 700099;
    private const string OutpostZone = "BLACK_CLAW_OUTPOST_220030000";

    public _24015TotemPlowed(IDataManager dataManager, IQuestDao questDao, QuestRewardService rewardService, IItemDao itemDao)
        : base(QuestIdConst, dataManager, questDao, rewardService)
    {
    }

    public override void Register(QuestEngine engine)
    {
        engine.RegisterOnZoneMissionEnd(QuestId);
        engine.RegisterOnLevelUp(QuestId);
        engine.RegisterQuestNpc(OutpostNpc).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(SuthranNpc).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(OutpostMob).OnKill.Add(QuestId);
        RegisterOnEnterZone(engine, OutpostZone);
    }

    public override ValueTask<bool> OnZoneMissionEndAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
        => DefaultOnZoneMissionEndEventAsync(env, conn, ct);

    public override ValueTask<bool> OnLevelUpAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
        => DefaultOnLvlUpEventAsync(env, conn, precedingQuestId: 24010, isZoneMission: true, ct);

    public override async ValueTask<bool> OnEnterZoneAsync(QuestEnv env, string zoneName, GsClientConnection conn, CancellationToken ct)
    {
        if (zoneName != OutpostZone) return false;
        var entry = env.Player.Quests.Get(QuestId);
        if (entry is null || entry.Status != QuestStatus.START || entry.GetVar(0) != 1) return false;

        await ChangeQuestStepAsync(conn, entry, 0, 2, toReward: false, ct);
        return true;
    }

    public override async ValueTask<bool> OnDialogAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var entry = env.Player.Quests.Get(QuestId);
        if (entry is null) return false;

        int var = entry.GetVar(0);
        int targetId = env.TargetId;
        int targetObjId = env.Target?.ObjectId ?? 0;
        var dialog = DialogActionLookup.FromId(env.DialogId);

        if (entry.Status == QuestStatus.START)
        {
            if (targetId == OutpostNpc)
            {
                if (dialog == DialogAction.QUEST_SELECT)
                {
                    if (var == 0) return await SendQuestDialogAsync(conn, targetObjId, 1011, ct);
                    if (var == 5) return await SendQuestDialogAsync(conn, targetObjId, 1352, ct);
                    return false;
                }
                if (dialog == DialogAction.SETPRO1)
                    return var == 0 && await DefaultCloseDialogAsync(env, conn, 0, 1, ct);
                if (dialog == DialogAction.SETPRO2)
                    return await DefaultCloseDialogAsync(env, conn, 5, 5, reward: true, sameNpc: false, ct);
                return false;
            }
        }
        else if (entry.Status == QuestStatus.REWARD)
        {
            if (targetId == SuthranNpc)
                return await SendQuestEndDialogAsync(env, conn, ct);
        }
        return false;
    }

    public override async ValueTask<bool> OnKillAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var entry = env.Player.Quests.Get(QuestId);
        if (entry is null) return false;

        int var = entry.GetVar(0);
        if (var >= 2 && var < 4)
            return await DefaultOnKillEventAsync(env, conn, OutpostMob, 2, 4, ct);
        if (var == 4)
        {
            entry.Status = QuestStatus.REWARD;
            await UpdateQuestStatusAsync(conn, entry, ct);
            return true;
        }
        return false;
    }
}
