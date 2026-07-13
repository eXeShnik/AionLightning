// Port of Java data/scripts/system/handlers/quest/abyssal_splinter/_30265APolearmWalksintoaBar.java
// (Rikka). Using item 182209803 starts the quest directly and opens dialog 4 (item not explicitly
// removed, matching Java); report to Fuchsia (203830, var 0->1), then Asteros (203058, var 1->2),
// then Aratus (790001), whose SELECT_QUEST_REWARD flips straight to REWARD; turn in at Aratus.
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

public sealed class _30265APolearmWalksintoaBar : QuestHandlerBase
{
    private const int QuestIdConst = 30265;
    private const int FuchsiaNpc   = 203830;
    private const int AsterosNpc   = 203058;
    private const int AratusNpc    = 790001;
    private const int ItemId       = 182209803;

    public _30265APolearmWalksintoaBar(IDataManager dataManager, IQuestDao questDao, QuestRewardService rewardService, IItemDao itemDao)
        : base(QuestIdConst, dataManager, questDao, rewardService)
    {
    }

    public override void Register(QuestEngine engine)
    {
        engine.RegisterQuestNpc(FuchsiaNpc).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(AsterosNpc).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(AratusNpc).OnTalk.Add(QuestId);
        engine.RegisterQuestItem(ItemId, QuestId);
    }

    public override async ValueTask<bool> OnDialogAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var entry = env.Player.Quests.Get(QuestId);
        if (entry is null || entry.Status == QuestStatus.NONE) return false;

        int targetObjId = env.Target?.ObjectId ?? 0;
        var dialog = DialogActionLookup.FromId(env.DialogId);

        if (entry.Status == QuestStatus.START)
        {
            int var = entry.GetVar(0);
            if (env.TargetId == FuchsiaNpc)
            {
                if (dialog == DialogAction.QUEST_SELECT && var == 0)
                    return await SendQuestDialogAsync(conn, targetObjId, 1352, ct);
                if (dialog == DialogAction.SETPRO1)
                    return await DefaultCloseDialogAsync(env, conn, 0, 1, ct);
                return false;
            }
            if (env.TargetId == AsterosNpc)
            {
                if (dialog == DialogAction.QUEST_SELECT && var == 1)
                    return await SendQuestDialogAsync(conn, targetObjId, 1693, ct);
                if (dialog == DialogAction.SETPRO2)
                    return await DefaultCloseDialogAsync(env, conn, 1, 2, ct);
                return false;
            }
            if (env.TargetId == AratusNpc)
            {
                if (dialog == DialogAction.QUEST_SELECT && var == 2)
                    return await SendQuestDialogAsync(conn, targetObjId, 2375, ct);
                if (dialog == DialogAction.SELECT_QUEST_REWARD)
                {
                    await ChangeQuestStepAsync(conn, entry, 2, 2, toReward: true, ct);
                    return await SendQuestDialogAsync(conn, targetObjId, 5, ct);
                }
                return false;
            }
            return false;
        }

        if (entry.Status == QuestStatus.REWARD && env.TargetId == AratusNpc)
            return await SendQuestEndDialogAsync(env, conn, ct);

        return false;
    }

    public override async ValueTask<bool> OnItemUseAsync(Player player, int itemId, GsClientConnection conn, CancellationToken ct)
    {
        if (itemId != ItemId) return false;
        if (player.Quests.Get(QuestId) is not null) return false;

        await StartMissionAsync(conn, player, QuestStatus.START, ct);
        return await SendQuestDialogAsync(conn, 0, 4, ct);
    }
}
