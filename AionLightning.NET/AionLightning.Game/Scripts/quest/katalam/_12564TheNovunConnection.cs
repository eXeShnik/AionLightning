// Port of Java data/scripts/system/handlers/quest/katalam/_12564TheNovunConnection.java (Romanz).
// Start/turn-in both at 801019; mid-quest talk at 730802 (SETPRO2 flips var 1->2 + REWARD); entering
// the LDF5A_SENSORYAREA_Q12564_206317_10_600050000 region while at var 0 advances 0->1. SKIPPED:
// Java's registerQuestNpc(206317).addOnAtDistanceEvent(questId) — onAtDistanceEvent duplicated the
// exact same 0->1 transition as onEnterZoneEvent (no other effect), and this port has no
// onAtDistance hook, so the zone trigger alone carries the same progression.
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

public sealed class _12564TheNovunConnection : QuestHandlerBase
{
    private const int QuestIdConst = 12564;
    private const int StartNpc      = 801019;
    private const int MidNpc        = 730802;
    private const string EnterZoneName = "LDF5A_SENSORYAREA_Q12564_206317_10_600050000";

    public _12564TheNovunConnection(IDataManager dataManager, IQuestDao questDao, QuestRewardService rewardService, IItemDao itemDao)
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

    public override async ValueTask<bool> OnEnterZoneAsync(QuestEnv env, string zoneName, GsClientConnection conn, CancellationToken ct)
    {
        if (zoneName != EnterZoneName) return false;

        var entry = env.Player.Quests.Get(QuestId);
        if (entry is null || entry.Status != QuestStatus.START || entry.GetVar(0) != 0) return false;

        await ChangeQuestStepAsync(conn, entry, 0, 1, toReward: false, ct);
        return true;
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
            if (dialog == DialogAction.SETPRO2)
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
}
