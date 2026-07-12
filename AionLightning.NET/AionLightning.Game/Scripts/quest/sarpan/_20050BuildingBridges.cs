// Port of Java data/scripts/system/handlers/quest/sarpan/_20050BuildingBridges.java (vlog).
// Talk to Aimah (205617) to start; Rafael (205585) var 1->2, showing a cutscene; Eyriss (205739)
// var 2->3; use the Mephitic Caverns Sealing Gate (730468) at var 3->4; kill Mephitic Klaws
// (6 mob ids) from var 4 through 13, then one more to flip to REWARD; turn in at Rafael.
// Skip vs Java: the sealing-gate interaction calls TeleportService2.teleportTo(...) - no
// TeleportService exists in this port, so the var/status transition happens without the
// short-range teleport (player must walk instead).
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

public sealed class _20050BuildingBridges : QuestHandlerBase
{
    private const int QuestIdConst = 20050;
    private const int AimahNpc  = 205617;
    private const int RafaelNpc = 205585;
    private const int EyrissNpc = 205739;
    private const int SealingGateNpc = 730468;

    private static readonly int[] MephiticKlaws = [217849, 217852, 217854, 217853, 217851, 217850];

    public _20050BuildingBridges(IDataManager dataManager, IQuestDao questDao, QuestRewardService rewardService, IItemDao itemDao)
        : base(QuestIdConst, dataManager, questDao, rewardService)
    {
    }

    public override void Register(QuestEngine engine)
    {
        engine.RegisterOnLevelUp(QuestId);
        engine.RegisterOnZoneMissionEnd(QuestId);
        engine.RegisterQuestNpc(AimahNpc).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(RafaelNpc).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(EyrissNpc).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(SealingGateNpc).OnTalk.Add(QuestId);
        foreach (int klaw in MephiticKlaws)
            engine.RegisterQuestNpc(klaw).OnKill.Add(QuestId);
    }

    public override ValueTask<bool> OnZoneMissionEndAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
        => DefaultOnZoneMissionEndEventAsync(env, conn, ct);

    public override ValueTask<bool> OnLevelUpAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
        => DefaultOnLvlUpEventAsync(env, conn, precedingQuestId: 20040, isZoneMission: true, ct);

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

            if (targetId == AimahNpc)
            {
                if (dialog == DialogAction.QUEST_SELECT && var == 0) return await SendQuestDialogAsync(conn, targetObjId, 1011, ct);
                if (dialog == DialogAction.SETPRO1) return await DefaultCloseDialogAsync(env, conn, 0, 1, ct);
                return false;
            }
            if (targetId == RafaelNpc)
            {
                if (dialog == DialogAction.QUEST_SELECT && var == 1) return await SendQuestDialogAsync(conn, targetObjId, 1352, ct);
                if (dialog == DialogAction.SELECT_ACTION_1353 && var == 1)
                {
                    await SendQuestDialogAsync(conn, targetObjId, 1352, ct);
                    await PlayQuestMovieAsync(conn, env.Player, 714, ct);
                    return true;
                }
                if (dialog == DialogAction.SETPRO2) return await DefaultCloseDialogAsync(env, conn, 1, 2, ct);
                return false;
            }
            if (targetId == EyrissNpc)
            {
                if (dialog == DialogAction.QUEST_SELECT && var == 2) return await SendQuestDialogAsync(conn, targetObjId, 1693, ct);
                if (dialog == DialogAction.SETPRO3) return await DefaultCloseDialogAsync(env, conn, 2, 3, ct);
                return false;
            }
            if (targetId == SealingGateNpc)
            {
                if (dialog == DialogAction.USE_OBJECT && var == 3) return await SendQuestDialogAsync(conn, targetObjId, 2034, ct);
                if (dialog == DialogAction.SETPRO4 && var == 3)
                {
                    await ChangeQuestStepAsync(conn, entry, 0, 4, toReward: false, ct);
                    // Skip vs Java: TeleportService2.teleportTo(...) to Sarpan Capitol - no TeleportService in this port.
                    return await CloseDialogWindowAsync(conn, targetObjId, ct);
                }
                return false;
            }
            return false;
        }

        if (entry.Status == QuestStatus.REWARD && targetId == RafaelNpc)
        {
            if (dialog == DialogAction.USE_OBJECT) return await SendQuestDialogAsync(conn, targetObjId, 10002, ct);
            return await SendQuestEndDialogAsync(env, conn, ct);
        }

        return false;
    }

    public override async ValueTask<bool> OnKillAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var entry = env.Player.Quests.Get(QuestId);
        if (entry is null || entry.Status != QuestStatus.START) return false;

        int var = entry.GetVar(0);
        if (var >= 4 && var < 13)
            return await DefaultOnKillEventAsync(env, conn, MephiticKlaws, var, var + 1, ct);
        if (var == 13)
            return await DefaultOnKillEventAsync(env, conn, MephiticKlaws, 13, reward: true, ct);

        return false;
    }
}
