// Port of Java data/scripts/system/handlers/quest/abyssal_splinter/_30264ANecklacewithHistory.java
// (Rikka). Using item 182209802 starts the quest and immediately flips it to REWARD (Java
// QuestService.startQuest(env) then changeQuestStep(env, 0, 0, true) - var untouched, reward flip
// only) and opens dialog 4; the lore item is never explicitly removed by the handler, matching Java
// exactly (registerQuestItem only, no removeQuestItem call). Turn in at Charna (203706).
using System.Threading;
using System.Threading.Tasks;
using AionLightning.Game.Dao;
using AionLightning.Game.DataHolders;
using AionLightning.Game.Model;
using AionLightning.Game.Model.Quest;
using AionLightning.Game.Network.Aion;
using AionLightning.Game.QuestEngine;
using AionLightning.Game.QuestEngine.Handlers;
using AionLightning.Game.QuestEngine.Model;
using AionLightning.Game.Services;

namespace Quest.AbyssalSplinter;

public sealed class _30264ANecklacewithHistory : QuestHandlerBase
{
    private const int QuestIdConst = 30264;
    private const int TurnInNpc    = 203706;
    private const int ItemId       = 182209802;

    public _30264ANecklacewithHistory(IDataManager dataManager, IQuestDao questDao, QuestRewardService rewardService, IItemDao itemDao)
        : base(QuestIdConst, dataManager, questDao, rewardService)
    {
    }

    public override void Register(QuestEngine engine)
    {
        engine.RegisterQuestNpc(TurnInNpc).OnTalk.Add(QuestId);
        engine.RegisterQuestItem(ItemId, QuestId);
    }

    public override async ValueTask<bool> OnDialogAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var entry = env.Player.Quests.Get(QuestId);
        if (entry is null || entry.Status == QuestStatus.NONE) return false;

        if (entry.Status == QuestStatus.REWARD && env.TargetId == TurnInNpc)
            return await SendQuestEndDialogAsync(env, conn, ct);

        return false;
    }

    public override async ValueTask<bool> OnItemUseAsync(Player player, int itemId, GsClientConnection conn, CancellationToken ct)
    {
        if (itemId != ItemId) return false;
        if (player.Quests.Get(QuestId) is not null) return false;

        await StartMissionAsync(conn, player, QuestStatus.START, ct);
        var entry = player.Quests.Get(QuestId);
        if (entry is not null)
            await ChangeQuestStepAsync(conn, entry, 0, 0, toReward: true, ct);
        return await SendQuestDialogAsync(conn, 0, 4, ct);
    }
}
