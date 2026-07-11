// Port of Java data/scripts/system/handlers/quest/heiron/_1561TheMisersMap.java.
// Item-use start (Jewel Box map item 182201728) directly starts the quest; turn in at the
// item-turned-npc (700188). Skip vs Java: the 3s SM_ITEM_USAGE_ANIMATION cosmetic delay before
// the quest starts is omitted (same simplification already applied to Ishalgen 2136TheLostAxe) —
// the quest starts immediately on item use instead.
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

namespace Quest.Heiron;

public sealed class _1561TheMisersMap : QuestHandlerBase
{
    private const int QuestIdConst = 1561;
    private const int JewelBoxNpc  = 700188;
    private const int MapItemId    = 182201728;

    private readonly IItemDao _itemDao;

    public _1561TheMisersMap(IDataManager dataManager, IQuestDao questDao, QuestRewardService rewardService, IItemDao itemDao)
        : base(QuestIdConst, dataManager, questDao, rewardService)
    {
        _itemDao = itemDao;
    }

    public override void Register(QuestEngine engine)
    {
        engine.RegisterQuestNpc(JewelBoxNpc).OnTalk.Add(QuestId);
        engine.RegisterQuestItem(MapItemId, QuestId);
    }

    public override async ValueTask<bool> OnItemUseAsync(Player player, int itemId, GsClientConnection conn, CancellationToken ct)
    {
        if (itemId != MapItemId) return false;
        var entry = player.Quests.Get(QuestId);
        if (entry is null || entry.Status == QuestStatus.NONE)
        {
            await RemoveQuestItemAsync(player, conn, _itemDao, MapItemId, 1, ct);
            await StartMissionAsync(conn, player, QuestStatus.START, ct);
        }
        return true;
    }

    public override async ValueTask<bool> OnDialogAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var entry = env.Player.Quests.Get(QuestId);
        if (entry is null || env.TargetId != JewelBoxNpc) return false;

        var dialog = DialogActionLookup.FromId(env.DialogId);

        if (entry.Status == QuestStatus.START && entry.GetVar(0) == 0)
        {
            if (dialog is DialogAction.QUEST_SELECT or DialogAction.USE_OBJECT)
                return await SendQuestDialogAsync(conn, env.Target?.ObjectId ?? 0, 2375, ct);
            if (dialog == DialogAction.SELECT_QUEST_REWARD)
                return await DefaultCloseDialogAsync(env, conn, 0, 0, reward: true, sameNpc: true, ct);
        }
        else if (entry.Status == QuestStatus.REWARD)
        {
            return await SendQuestEndDialogAsync(env, conn, ct);
        }
        return false;
    }
}
