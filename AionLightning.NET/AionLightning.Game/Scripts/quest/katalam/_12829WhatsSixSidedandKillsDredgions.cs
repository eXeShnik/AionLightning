// Port of Java data/scripts/system/handlers/quest/katalam/_12829WhatsSixSidedandKillsDredgions.java
// (Evil_dnk). Start/reward turn-in at 801235; mid-quest talk at 730778 (SET_SUCCEED flips var 1->2 +
// REWARD, no var guard — matches Java exactly, which applies it unconditionally once START); entering
// the LDF5A_SENSORYAREA_Q12829_206318_9_600050000 region while at var 0 advances 0->1 (Java's
// onEnterZoneEvent never returns true after the change — mirrored as-is, changeQuestStep still fires
// the packet update). Java's onDialogEvent redundantly calls updateQuestStatus right after
// changeQuestStep for the SET_SUCCEED branch — changeQuestStep already persists/broadcasts, so that
// duplicate call is dropped here (ChangeQuestStepAsync does it once).
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

public sealed class _12829WhatsSixSidedandKillsDredgions : QuestHandlerBase
{
    private const int QuestIdConst = 12829;
    private const int StartNpc      = 801235;
    private const int MidNpc        = 730778;
    private const string EnterZoneName = "LDF5A_SENSORYAREA_Q12829_206318_9_600050000";

    public _12829WhatsSixSidedandKillsDredgions(IDataManager dataManager, IQuestDao questDao, QuestRewardService rewardService, IItemDao itemDao)
        : base(QuestIdConst, dataManager, questDao, rewardService)
    {
    }

    public override void Register(QuestEngine engine)
    {
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
            return await SendQuestStartDialogAsync(env, conn, ct);
        }

        if (entry.Status == QuestStatus.START)
        {
            if (targetId != MidNpc) return false;

            if (dialog == DialogAction.QUEST_SELECT)
            {
                if (entry.GetVar(0) == 1)
                    return await SendQuestDialogAsync(conn, targetObjId, 1352, ct);
                return await CloseDialogWindowAsync(conn, targetObjId, ct);
            }
            if (dialog == DialogAction.SET_SUCCEED)
            {
                await ChangeQuestStepAsync(conn, entry, 0, 2, toReward: true, ct);
                return await CloseDialogWindowAsync(conn, targetObjId, ct);
            }
            return false;
        }

        if (entry.Status == QuestStatus.REWARD)
        {
            if (targetId == StartNpc)
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
