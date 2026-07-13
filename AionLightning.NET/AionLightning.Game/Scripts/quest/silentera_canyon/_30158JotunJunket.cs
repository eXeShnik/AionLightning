// Port of Java data/scripts/system/handlers/quest/silentera_canyon/_30158JotunJunket.java (Ritsu).
// Talk to 799383 to start; entering the Unknown Lands zone while START at var0 0 flips the quest
// straight to REWARD (var unchanged, matching Java's setStatus(REWARD)+updateQuestStatus); turn in
// back at 799383.
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

namespace Quest.SilenteraCanyon;

public sealed class _30158JotunJunket : QuestHandlerBase
{
    private const int QuestIdConst = 30158;
    private const int StartNpc     = 799383;
    private const string UnknownLandsZone = "UNKNOWN_LANDS_600010000";

    public _30158JotunJunket(IDataManager dataManager, IQuestDao questDao, QuestRewardService rewardService, IItemDao itemDao)
        : base(QuestIdConst, dataManager, questDao, rewardService)
    {
    }

    public override void Register(QuestEngine engine)
    {
        engine.RegisterQuestNpc(StartNpc).OnQuestStart.Add(QuestId);
        engine.RegisterQuestNpc(StartNpc).OnTalk.Add(QuestId);
        RegisterOnEnterZone(engine, UnknownLandsZone);
    }

    public override async ValueTask<bool> OnEnterZoneAsync(QuestEnv env, string zoneName, GsClientConnection conn, CancellationToken ct)
    {
        if (zoneName != UnknownLandsZone) return false;
        var entry = env.Player.Quests.Get(QuestId);
        if (entry is null || entry.GetVar(0) != 0) return false;
        if (entry.Status != QuestStatus.START) return false;

        await ChangeQuestStepAsync(conn, entry, varIdx: -1, newValue: 0, toReward: true, ct);
        return true;
    }

    public override async ValueTask<bool> OnDialogAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        if (env.TargetId != StartNpc) return false;

        var entry       = env.Player.Quests.Get(QuestId);
        int targetObjId = env.Target?.ObjectId ?? 0;
        var dialog      = DialogActionLookup.FromId(env.DialogId);

        if (entry is null || entry.Status == QuestStatus.NONE)
        {
            if (dialog == DialogAction.QUEST_SELECT)
                return await SendQuestDialogAsync(conn, targetObjId, 4762, ct);
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
