// Port of Java data/scripts/system/handlers/quest/idian_depths/_23615BeyondTheDepths.java (pralinka).
// Asmodian twin of 13615. Start at 801551 (QUEST_SELECT 4762 -> accept); entering the South Grand
// Passage zone (LDF5_UNDER_SENSORYAREA_Q13615_206326_4_600070000, shared name as in Java) while
// START with var0==0 runs changeQuestStep(0,1,true) = set var0=1 AND flip to REWARD; turn in at
// 801545 (QUEST_SELECT 10001, else end dialog).
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

namespace Quest.IdianDepths;

public sealed class _23615BeyondTheDepths : QuestHandlerBase
{
    private const int QuestIdConst = 23615;
    private const int StartNpc  = 801551;
    private const int TurnInNpc = 801545;
    private const string ZoneName = "LDF5_UNDER_SENSORYAREA_Q13615_206326_4_600070000";

    public _23615BeyondTheDepths(IDataManager dataManager, IQuestDao questDao, QuestRewardService rewardService, IItemDao itemDao)
        : base(QuestIdConst, dataManager, questDao, rewardService)
    {
    }

    public override void Register(QuestEngine engine)
    {
        engine.RegisterQuestNpc(StartNpc).OnQuestStart.Add(QuestId);
        engine.RegisterQuestNpc(TurnInNpc).OnTalk.Add(QuestId);
        RegisterOnEnterZone(engine, ZoneName);
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
            if (dialog == DialogAction.QUEST_ACCEPT_SIMPLE)
                return await SendQuestStartDialogAsync(env, conn, ct);
            return false;
        }

        if (entry.Status == QuestStatus.REWARD && targetId == TurnInNpc)
        {
            if (dialog == DialogAction.QUEST_SELECT)
                return await SendQuestDialogAsync(conn, targetObjId, 10001, ct);
            return await SendQuestEndDialogAsync(env, conn, ct);
        }

        return false;
    }

    public override async ValueTask<bool> OnEnterZoneAsync(QuestEnv env, string zoneName, GsClientConnection conn, CancellationToken ct)
    {
        if (zoneName != ZoneName) return false;
        var entry = env.Player.Quests.Get(QuestId);
        if (entry is null || entry.Status != QuestStatus.START || entry.GetVar(0) != 0) return false;

        await ChangeQuestStepAsync(conn, entry, 0, 1, toReward: true, ct);
        return true;
    }
}
