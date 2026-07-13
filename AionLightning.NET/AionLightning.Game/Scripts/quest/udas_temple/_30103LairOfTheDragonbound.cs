// Port of Java data/scripts/system/handlers/quest/udas_temple/_30103LairOfTheDragonbound.java (vlog).
// Talk to 799333 to start; collecting item 182209189 flips straight to REWARD (var 0->1); turn in
// at the same npc, consuming the item.
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

namespace Quest.UdasTemple;

public sealed class _30103LairOfTheDragonbound : QuestHandlerBase
{
    private const int QuestIdConst = 30103;
    private const int Npc          = 799333;
    private const int RelicItem    = 182209189;

    private readonly IItemDao _itemDao;

    public _30103LairOfTheDragonbound(IDataManager dataManager, IQuestDao questDao, QuestRewardService rewardService, IItemDao itemDao)
        : base(QuestIdConst, dataManager, questDao, rewardService)
    {
        _itemDao = itemDao;
    }

    public override void Register(QuestEngine engine)
    {
        engine.RegisterQuestNpc(Npc).OnQuestStart.Add(QuestId);
        engine.RegisterQuestNpc(Npc).OnTalk.Add(QuestId);
        engine.RegisterItemGet(RelicItem, QuestId);
    }

    public override async ValueTask<bool> OnDialogAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var player      = env.Player;
        var entry       = player.Quests.Get(QuestId);
        int targetId    = env.TargetId;
        int targetObjId = env.Target?.ObjectId ?? 0;
        var dialog      = DialogActionLookup.FromId(env.DialogId);

        if (entry is null || entry.Status == QuestStatus.NONE)
        {
            if (targetId == Npc)
            {
                if (dialog == DialogAction.QUEST_SELECT)
                    return await SendQuestDialogAsync(conn, targetObjId, 1011, ct);
                return await SendQuestStartDialogAsync(env, conn, ct);
            }
        }
        else if (entry.Status == QuestStatus.START)
        {
            int var = entry.GetVar(0);
            if (targetId == Npc)
            {
                if (dialog == DialogAction.QUEST_SELECT && var == 1)
                    return await SendQuestDialogAsync(conn, targetObjId, 2375, ct);
                if (dialog == DialogAction.SELECT_QUEST_REWARD)
                {
                    await ChangeQuestStepAsync(conn, entry, 1, 1, toReward: true, ct);
                    return await SendQuestDialogAsync(conn, targetObjId, 5, ct);
                }
            }
        }
        else if (entry.Status == QuestStatus.REWARD)
        {
            if (targetId == Npc)
            {
                await RemoveQuestItemAsync(player, conn, _itemDao, RelicItem, 1, ct);
                return await SendQuestEndDialogAsync(env, conn, ct);
            }
        }
        return false;
    }

    public override async ValueTask<bool> OnItemGetAsync(Player player, int itemId, GsClientConnection conn, CancellationToken ct)
    {
        if (itemId != RelicItem) return false;
        var entry = player.Quests.Get(QuestId);
        if (entry is null || entry.Status != QuestStatus.START || entry.GetVar(0) != 0) return false;

        await ChangeQuestStepAsync(conn, entry, 0, 1, toReward: false, ct);
        return true;
    }
}
