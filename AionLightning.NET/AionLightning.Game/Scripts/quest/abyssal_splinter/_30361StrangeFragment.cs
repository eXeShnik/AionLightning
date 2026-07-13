// Port of Java data/scripts/system/handlers/quest/abyssal_splinter/_30361StrangeFragment.java
// (Rikka). Asmodian mirror of _30261WeirdFragment: using item 182209820 starts the quest directly
// and consumes it; report to Erik (278033) then Lugbug (279029) to advance var 0->1->2 (reward flip
// at Lugbug); turn in at Gwal (260265).
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

public sealed class _30361StrangeFragment : QuestHandlerBase
{
    private const int QuestIdConst = 30361;
    private const int ErikNpc      = 278033;
    private const int LugbugNpc    = 279029;
    private const int TurnInNpc    = 260265;
    private const int ItemId       = 182209820;

    private readonly IItemDao _itemDao;

    public _30361StrangeFragment(IDataManager dataManager, IQuestDao questDao, QuestRewardService rewardService, IItemDao itemDao)
        : base(QuestIdConst, dataManager, questDao, rewardService)
    {
        _itemDao = itemDao;
    }

    public override void Register(QuestEngine engine)
    {
        engine.RegisterQuestNpc(ErikNpc).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(LugbugNpc).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(TurnInNpc).OnTalk.Add(QuestId);
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
            if (env.TargetId == ErikNpc)
            {
                if (dialog == DialogAction.QUEST_SELECT && var == 0)
                    return await SendQuestDialogAsync(conn, targetObjId, 1352, ct);
                if (dialog == DialogAction.SETPRO1)
                    return await DefaultCloseDialogAsync(env, conn, 0, 1, ct);
                return false;
            }
            if (env.TargetId == LugbugNpc)
            {
                if (dialog == DialogAction.QUEST_SELECT && var == 1)
                    return await SendQuestDialogAsync(conn, targetObjId, 1693, ct);
                if (dialog == DialogAction.SETPRO2)
                    return await DefaultCloseDialogAsync(env, conn, 1, 2, reward: true, sameNpc: false, ct);
                return false;
            }
            return false;
        }

        if (entry.Status == QuestStatus.REWARD && env.TargetId == TurnInNpc)
            return await SendQuestEndDialogAsync(env, conn, ct);

        return false;
    }

    public override async ValueTask<bool> OnItemUseAsync(Player player, int itemId, GsClientConnection conn, CancellationToken ct)
    {
        if (itemId != ItemId) return false;
        if (player.Quests.Get(QuestId) is not null) return false;

        await StartMissionAsync(conn, player, QuestStatus.START, ct);
        await RemoveQuestItemAsync(player, conn, _itemDao, ItemId, 1, ct);
        return await SendQuestDialogAsync(conn, 0, 4, ct);
    }
}
