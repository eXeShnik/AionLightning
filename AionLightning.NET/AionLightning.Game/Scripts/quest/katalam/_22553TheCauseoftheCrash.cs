// Port of Java data/scripts/system/handlers/quest/katalam/_22553TheCauseoftheCrash.java (Romanz).
// Start/turn-in both at 801005; mid-quest talk at 730778 (SET_SUCCEED flips var 1->2 + REWARD via
// DefaultCloseDialogAsync); entering the LDF5A_SENSORYAREA_Q12829_206318_9_600050000 region (Java
// reuses the same sensory-area zone name as _12829WhatsSixSidedandKillsDredgions) while at var 0
// advances 0->1 — Java's onEnterZoneEvent never returns true after the change, mirrored as-is.
// SKIPPED: Java's registerQuestNpc(206318).addOnAtDistanceEvent(questId) — onAtDistanceEvent
// duplicated the exact same 0->1 transition as onEnterZoneEvent (no other effect), and this port has
// no onAtDistance hook, so the zone trigger alone carries the same progression.
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

namespace Quest.Katalam;

public sealed class _22553TheCauseoftheCrash : QuestHandlerBase
{
    private const int QuestIdConst = 22553;
    private const int StartNpc      = 801005;
    private const int MidNpc        = 730778;
    private const string EnterZoneName = "LDF5A_SENSORYAREA_Q12829_206318_9_600050000";

    public _22553TheCauseoftheCrash(IDataManager dataManager, IQuestDao questDao, QuestRewardService rewardService, IItemDao itemDao)
        : base(QuestIdConst, dataManager, questDao, rewardService)
    {
    }

    public override void Register(QuestEngine engine)
    {
        engine.RegisterQuestNpc(StartNpc).OnQuestStart.Add(QuestId);
        engine.RegisterQuestNpc(StartNpc).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(MidNpc).OnTalk.Add(QuestId);
        RegisterOnEnterZone(engine, EnterZoneName);
    }

    public override async ValueTask<bool> OnDialogAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var entry = env.Player.Quests.Get(QuestId);
        var dialog = DialogActionLookup.FromId(env.DialogId);
        int targetObjId = env.Target?.ObjectId ?? 0;
        int targetId = env.TargetId;

        if (entry is null || entry.Status == QuestStatus.NONE)
        {
            if (targetId != StartNpc) return false;
            if (dialog == DialogAction.QUEST_SELECT)
                return await SendQuestDialogAsync(conn, targetObjId, 4762, ct);
            if (dialog == DialogAction.QUEST_ACCEPT_SIMPLE)
                return await SendQuestStartDialogAsync(env, conn, ct);
            return false;
        }

        if (targetId == MidNpc)
        {
            if (dialog == DialogAction.QUEST_SELECT)
            {
                if (entry.GetVar(0) == 1)
                    return await SendQuestDialogAsync(conn, targetObjId, 1352, ct);
                return false;
            }
            if (dialog == DialogAction.SET_SUCCEED)
                return await DefaultCloseDialogAsync(env, conn, 1, 2, reward: true, sameNpc: false, ct);
            return false;
        }

        if (entry.Status == QuestStatus.REWARD && targetId == StartNpc)
        {
            if (dialog == DialogAction.USE_OBJECT)
                return await SendQuestDialogAsync(conn, targetObjId, 10002, ct);
            return await SendQuestEndDialogAsync(env, conn, ct);
        }

        return false;
    }

    public override async ValueTask<bool> OnEnterZoneAsync(QuestEnv env, string zoneName, GsClientConnection conn, CancellationToken ct)
    {
        if (zoneName != EnterZoneName) return false;

        var entry = env.Player.Quests.Get(QuestId);
        if (entry is null || entry.Status != QuestStatus.START) return false;

        if (entry.GetVar(0) == 0)
            await ChangeQuestStepAsync(conn, entry, 0, 1, toReward: false, ct);

        return false;
    }
}
