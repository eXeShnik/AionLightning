// Port of Java data/scripts/system/handlers/quest/event_quests/_50005DaevasDayEnergy.java.
// Started from a target-less (targetObjId 0) QUEST_ACCEPT_1 dialog rather than an NPC-anchored
// accept flow — same idiom already used by Scripts/quest/esoterrace/_18405MemoriesInTheCornerOfHisMind.cs
// (StartMissionAsync + CloseDialogWindowAsync). Turn in immediately at Gracia (799933) for reward.
// Skip vs Java: onLvlUpEvent only ever abandons this quest when EventService.checkQuestIsActive(...)
// reports the Daeva's Day event as switched off server-side — there is no EventService/event-toggle
// concept in this port, so RegisterOnLevelUp is kept for structural parity but OnLevelUpAsync is not
// overridden (default no-op).
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

namespace Quest.EventQuests;

public sealed class _50005DaevasDayEnergy : QuestHandlerBase
{
    private const int QuestIdConst = 50005;
    private const int GraciaNpc    = 799933;

    public _50005DaevasDayEnergy(IDataManager dataManager, IQuestDao questDao, QuestRewardService rewardService, IItemDao itemDao)
        : base(QuestIdConst, dataManager, questDao, rewardService)
    {
    }

    public override void Register(QuestEngine engine)
    {
        engine.RegisterQuestNpc(GraciaNpc).OnTalk.Add(QuestId);
        engine.RegisterOnLevelUp(QuestId);
    }

    public override async ValueTask<bool> OnDialogAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var player = env.Player;
        var dialog = DialogActionLookup.FromId(env.DialogId);

        if (env.TargetId == 0)
        {
            if (dialog == DialogAction.QUEST_ACCEPT_1)
            {
                await StartMissionAsync(conn, player, QuestStatus.START, ct);
                return await CloseDialogWindowAsync(conn, 0, ct);
            }
            return false;
        }

        if (env.TargetId != GraciaNpc) return false;

        var entry = player.Quests.Get(QuestId);
        if (entry is null) return false;

        int targetObjId = env.Target?.ObjectId ?? 0;
        if (dialog == DialogAction.QUEST_SELECT && entry.Status == QuestStatus.START)
            return await SendQuestDialogAsync(conn, targetObjId, 2375, ct);

        if (dialog == DialogAction.SELECT_QUEST_REWARD)
            await ChangeQuestStepAsync(conn, entry, 0, 1, toReward: true, ct);

        return await SendQuestEndDialogAsync(env, conn, ct);
    }
}
