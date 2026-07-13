// Port of Java data/scripts/system/handlers/quest/katalam/_12522DoYouHearWhatHear.java (Romanz).
// Start/turn-in both at 801023; kill any of the 6 mobIds five times (var 1->5 span, then 5->reward)
// via DefaultOnKillEventAsync; entering the LDF5A_SENSORYAREA_Q12522_206316_8_600050000 region while
// at var 0 advances 0->1. SKIPPED: Java's registerQuestNpc(206316).addOnAtDistanceEvent(questId) —
// onAtDistanceEvent duplicated the exact same 0->1 transition as onEnterZoneEvent (no other effect),
// and this port has no onAtDistance hook, so the zone trigger alone carries the same progression.
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

public sealed class _12522DoYouHearWhatHear : QuestHandlerBase
{
    private const int QuestIdConst = 12522;
    private const int StartNpc      = 801023;
    private const string EnterZoneName = "LDF5A_SENSORYAREA_Q12522_206316_8_600050000";
    private static readonly int[] MobIds = [231207, 231208, 231209, 231210, 231211, 231212];

    public _12522DoYouHearWhatHear(IDataManager dataManager, IQuestDao questDao, QuestRewardService rewardService, IItemDao itemDao)
        : base(QuestIdConst, dataManager, questDao, rewardService)
    {
    }

    public override void Register(QuestEngine engine)
    {
        engine.RegisterQuestNpc(StartNpc).OnQuestStart.Add(QuestId);
        engine.RegisterQuestNpc(StartNpc).OnTalk.Add(QuestId);
        RegisterOnEnterZone(engine, EnterZoneName);
        foreach (int mobId in MobIds)
            engine.RegisterQuestNpc(mobId).OnKill.Add(QuestId);
    }

    public override async ValueTask<bool> OnDialogAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        if (env.TargetId != StartNpc) return false;

        var entry = env.Player.Quests.Get(QuestId);
        var dialog = DialogActionLookup.FromId(env.DialogId);
        int targetObjId = env.Target?.ObjectId ?? 0;

        if (entry is null || entry.Status == QuestStatus.NONE)
        {
            if (dialog == DialogAction.QUEST_SELECT)
                return await SendQuestDialogAsync(conn, targetObjId, 4762, ct);
            if (dialog == DialogAction.QUEST_ACCEPT_SIMPLE)
                return await SendQuestStartDialogAsync(env, conn, ct);
            return false;
        }

        if (entry.Status == QuestStatus.REWARD)
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
        if (entry is null || entry.Status != QuestStatus.START || entry.GetVar(0) != 0) return false;

        await ChangeQuestStepAsync(conn, entry, 0, 1, toReward: false, ct);
        return true;
    }

    public override async ValueTask<bool> OnKillAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        if (await DefaultOnKillEventAsync(env, conn, MobIds, 1, 5, ct)) return true;
        return await DefaultOnKillEventAsync(env, conn, MobIds, 5, reward: true, ct);
    }
}
