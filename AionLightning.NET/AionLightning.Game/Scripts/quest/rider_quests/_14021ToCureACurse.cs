// Port of Java data/scripts/system/handlers/quest/rider_quests/_14021ToCureACurse.java (pralinka).
// Zone-mission sub-quest of 14020: talk to 203902 (var0 0->1), kill 6 cursed mobs (var0 1->7),
// use/report at 700179 (var0 7->8), report at 204043 (var0 8->9), turn in at 204030 (reward).
// Java bug: onDialogEvent's switch on 204043 had no break after QUEST_SELECT, so talking with
// var0 != 8 fell through into the SETPRO4 body and force-advanced the step (changeQuestStep has no
// internal var guard, unlike defaultCloseDialog), letting the player skip straight to var0 9. Fixed
// here so the step-force only fires on an actual SETPRO4 dialog.
using System.Linq;
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

public sealed class _14021ToCureACurse : QuestHandlerBase
{
    private const int QuestIdConst = 14021;
    private static readonly int[] _mobIds = [210771, 210758, 210763, 210764, 210759, 210770];
    private const int GuardNpc  = 203902;
    private const int Npc700179 = 700179;
    private const int Npc204043 = 204043;
    private const int Npc204030 = 204030;

    public _14021ToCureACurse(IDataManager dataManager, IQuestDao questDao, QuestRewardService rewardService, IItemDao itemDao)
        : base(QuestIdConst, dataManager, questDao, rewardService)
    {
    }

    public override void Register(QuestEngine engine)
    {
        engine.RegisterOnZoneMissionEnd(QuestId);
        engine.RegisterOnLevelUp(QuestId);
        foreach (int npc in new[] { GuardNpc, Npc700179, Npc204043, Npc204030 })
            engine.RegisterQuestNpc(npc).OnTalk.Add(QuestId);
        foreach (int mob in _mobIds) engine.RegisterQuestNpc(mob).OnKill.Add(QuestId);
    }

    public override ValueTask<bool> OnZoneMissionEndAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
        => DefaultOnZoneMissionEndEventAsync(env, conn, ct);

    public override ValueTask<bool> OnLevelUpAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
        => DefaultOnLvlUpEventAsync(env, conn, 14020, isZoneMission: true, ct);

    public override async ValueTask<bool> OnDialogAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var entry = env.Player.Quests.Get(QuestId);
        if (entry is null) return false;

        var dialog = DialogActionLookup.FromId(env.DialogId);
        int targetObjId = env.Target?.ObjectId ?? 0;

        if (entry.Status == QuestStatus.START)
        {
            int var = entry.GetVar(0);
            if (env.TargetId == GuardNpc)
            {
                if (dialog == DialogAction.QUEST_SELECT)
                    return var == 0 && await SendQuestDialogAsync(conn, targetObjId, 1011, ct);
                if (dialog == DialogAction.SETPRO1)
                    return await DefaultCloseDialogAsync(env, conn, 0, 1, ct);
                return false;
            }

            if (env.TargetId == Npc700179)
            {
                if (var != 7) return false;
                if (dialog == DialogAction.USE_OBJECT)
                    return await SendQuestDialogAsync(conn, targetObjId, 1693, ct);
                if (dialog == DialogAction.SETPRO3)
                {
                    await ChangeQuestStepAsync(conn, entry, 0, 8, toReward: false, ct);
                    return await CloseDialogWindowAsync(conn, targetObjId, ct);
                }
                return false;
            }

            if (env.TargetId == Npc204043)
            {
                if (dialog == DialogAction.QUEST_SELECT)
                    return var == 8 && await SendQuestDialogAsync(conn, targetObjId, 2034, ct);
                if (dialog == DialogAction.SETPRO4)
                {
                    await ChangeQuestStepAsync(conn, entry, 0, 9, toReward: false, ct);
                    return await CloseDialogWindowAsync(conn, targetObjId, ct);
                }
                return false;
            }

            if (env.TargetId == Npc204030)
            {
                if (dialog == DialogAction.QUEST_SELECT)
                    return var == 9 && await SendQuestDialogAsync(conn, targetObjId, 2375, ct);
                if (dialog == DialogAction.SETPRO5)
                    return await DefaultCloseDialogAsync(env, conn, 9, 9, reward: true, sameNpc: false, ct);
                return false;
            }
        }

        if (entry.Status == QuestStatus.REWARD && env.TargetId == GuardNpc)
        {
            if (dialog == DialogAction.QUEST_SELECT)
                return await SendQuestDialogAsync(conn, targetObjId, 3398, ct);
            return await SendQuestEndDialogAsync(env, conn, ct);
        }
        return false;
    }

    public override async ValueTask<bool> OnKillAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var entry = env.Player.Quests.Get(QuestId);
        if (entry is null || entry.Status != QuestStatus.START) return false;
        if (!_mobIds.Contains(env.TargetId)) return false;
        return await DefaultOnKillEventAsync(env, conn, _mobIds, 1, 7, ct);
    }
}
