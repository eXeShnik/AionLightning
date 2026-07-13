// Port of Java data/scripts/system/handlers/quest/katalam/_12566ChasingRumors.java (Eloann).
// Start/turn-in both at Mikene (800551); entering the SAPARINERKS_LOGGING_AREA_600050000 region
// while START and untouched (var 0 == 0) flips straight to REWARD (Java's onEnterZoneEvent sets
// QuestStatus.REWARD directly, bypassing changeQuestStep — mirrored here via
// ChangeQuestStepAsync(varIdx: -1, toReward: true) so the var stays untouched exactly like Java).
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

public sealed class _12566ChasingRumors : QuestHandlerBase
{
    private const int QuestIdConst = 12566;
    private const int MikeneNpc     = 800551;
    private const string EnterZoneName = "SAPARINERKS_LOGGING_AREA_600050000";

    public _12566ChasingRumors(IDataManager dataManager, IQuestDao questDao, QuestRewardService rewardService, IItemDao itemDao)
        : base(QuestIdConst, dataManager, questDao, rewardService)
    {
    }

    public override void Register(QuestEngine engine)
    {
        engine.RegisterQuestNpc(MikeneNpc).OnTalk.Add(QuestId);
        RegisterOnEnterZone(engine, EnterZoneName);
    }

    public override async ValueTask<bool> OnEnterZoneAsync(QuestEnv env, string zoneName, GsClientConnection conn, CancellationToken ct)
    {
        if (zoneName != EnterZoneName) return false;

        var entry = env.Player.Quests.Get(QuestId);
        if (entry is null || entry.GetVar(0) != 0 || entry.Status != QuestStatus.START) return false;

        await ChangeQuestStepAsync(conn, entry, varIdx: -1, newValue: 0, toReward: true, ct);
        return true;
    }

    public override async ValueTask<bool> OnDialogAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        if (env.TargetId != MikeneNpc) return false;

        var entry = env.Player.Quests.Get(QuestId);
        var dialog = DialogActionLookup.FromId(env.DialogId);
        int targetObjId = env.Target?.ObjectId ?? 0;

        if (entry is null || entry.Status == QuestStatus.NONE)
        {
            if (dialog == DialogAction.QUEST_SELECT)
                return await SendQuestDialogAsync(conn, targetObjId, 1011, ct);
            return await SendQuestStartDialogAsync(env, conn, ct);
        }

        if (entry.Status == QuestStatus.REWARD)
        {
            if (dialog == DialogAction.USE_OBJECT)
                return await SendQuestDialogAsync(conn, targetObjId, 10002, ct);
            if (dialog == DialogAction.SELECT_QUEST_REWARD)
                return await SendQuestDialogAsync(conn, targetObjId, 5, ct);
            return await SendQuestEndDialogAsync(env, conn, ct);
        }

        return false;
    }
}
