// Port of Java data/scripts/system/handlers/quest/pernon/_51009TheGiftOfCharity.java (Bobobear).
// Talk to either 831033 or 831039 to start; talking to either again while START unconditionally
// flips var 0->0 to REWARD (no dialog-action gate in Java — any dialog value while START completes
// it, ported the same way) and shows dialog 2375; turn in at either npc.
// Skip vs Java: qs.canRepeat() (repeatable-quest gate) is approximated as "no active entry", same
// as the rest of this port (see QuestEngine.ComputeNearbyQuests).
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

namespace Quest.Pernon;

public sealed class _51009TheGiftOfCharity : QuestHandlerBase
{
    private const int QuestIdConst = 51009;
    private const int StartNpc1    = 831033;
    private const int StartNpc2    = 831039;

    public _51009TheGiftOfCharity(IDataManager dataManager, IQuestDao questDao, QuestRewardService rewardService, IItemDao itemDao)
        : base(QuestIdConst, dataManager, questDao, rewardService)
    {
    }

    public override void Register(QuestEngine engine)
    {
        engine.RegisterQuestNpc(StartNpc1).OnQuestStart.Add(QuestId);
        engine.RegisterQuestNpc(StartNpc1).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(StartNpc2).OnQuestStart.Add(QuestId);
        engine.RegisterQuestNpc(StartNpc2).OnTalk.Add(QuestId);
    }

    public override async ValueTask<bool> OnDialogAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var player = env.Player;
        var entry  = player.Quests.Get(QuestId);
        int targetId = env.TargetId;
        int targetObjId = env.Target?.ObjectId ?? 0;
        var dialog = DialogActionLookup.FromId(env.DialogId);

        if (targetId != StartNpc1 && targetId != StartNpc2) return false;

        if (entry is null)
        {
            if (dialog == DialogAction.QUEST_SELECT)
                return await SendQuestDialogAsync(conn, targetObjId, 1011, ct);
            if (dialog is DialogAction.QUEST_ACCEPT_1 or DialogAction.QUEST_ACCEPT_SIMPLE)
                return await SendQuestStartDialogAsync(env, conn, ct);
            return false;
        }

        if (entry.Status == QuestStatus.START)
        {
            await ChangeQuestStepAsync(conn, entry, 0, 0, toReward: true, ct);
            return await SendQuestDialogAsync(conn, targetObjId, 2375, ct);
        }

        if (entry.Status == QuestStatus.REWARD)
            return await SendQuestEndDialogAsync(env, conn, ct);

        return false;
    }
}
