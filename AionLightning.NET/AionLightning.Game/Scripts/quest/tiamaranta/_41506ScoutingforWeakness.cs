// Port of Java data/scripts/system/handlers/quest/tiamaranta/_41506ScoutingforWeakness.java (mr.madison).
// Talk to 205934 to accept; entering the BALAUR_CAVALRY_BASE_600030000 zone while still on step 0
// flips the quest straight to REWARD; return to 205934 to turn in.
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

namespace Quest.Tiamaranta;

public sealed class _41506ScoutingforWeakness : QuestHandlerBase
{
    private const int QuestIdConst = 41506;
    private const int StartNpc     = 205934;
    private const string CavalryBaseZone = "BALAUR_CAVALRY_BASE_600030000";

    public _41506ScoutingforWeakness(IDataManager dataManager, IQuestDao questDao, QuestRewardService rewardService, IItemDao itemDao)
        : base(QuestIdConst, dataManager, questDao, rewardService)
    {
    }

    public override void Register(QuestEngine engine)
    {
        engine.RegisterQuestNpc(StartNpc).OnQuestStart.Add(QuestId);
        engine.RegisterQuestNpc(StartNpc).OnTalk.Add(QuestId);
        RegisterOnEnterZone(engine, CavalryBaseZone);
    }

    public override async ValueTask<bool> OnEnterZoneAsync(QuestEnv env, string zoneName, GsClientConnection conn, CancellationToken ct)
    {
        if (zoneName != CavalryBaseZone) return false;
        var entry = env.Player.Quests.Get(QuestId);
        if (entry is null || entry.GetVar(0) != 0 || entry.Status != QuestStatus.START) return false;

        await ChangeQuestStepAsync(conn, entry, -1, 0, toReward: true, ct);
        return true;
    }

    public override async ValueTask<bool> OnDialogAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var player = env.Player;
        var entry  = player.Quests.Get(QuestId);
        int targetId = env.TargetId;
        int targetObjId = env.Target?.ObjectId ?? 0;
        var dialog = DialogActionLookup.FromId(env.DialogId);

        if (entry is null || entry.Status == QuestStatus.NONE)
        {
            if (targetId != StartNpc) return false;
            if (dialog == DialogAction.QUEST_SELECT)
                return await SendQuestDialogAsync(conn, targetObjId, 4762, ct);
            return await SendQuestStartDialogAsync(env, conn, ct);
        }

        if (entry.Status == QuestStatus.REWARD && targetId == StartNpc)
            return await SendQuestEndDialogAsync(env, conn, ct);

        return false;
    }
}
