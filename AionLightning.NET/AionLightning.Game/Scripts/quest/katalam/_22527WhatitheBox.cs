// Port of Java data/scripts/system/handlers/quest/katalam/_22527WhatitheBox.java (Romanz).
// Asmodian mirror of _12527WorthPublishing. Item 182213345 is held before the quest starts
// (granted elsewhere); using it opens the accept dialog (targetId 0), QUEST_ACCEPT_1 starts the
// quest directly. Turn in at 801000; the item is removed on SELECT_QUEST_REWARD, not at accept.
using System.Threading;
using System.Threading.Tasks;
using AionLightning.Game.Dao;
using AionLightning.Game.DataHolders;
using AionLightning.Game.Model;
using AionLightning.Game.Model.Quest;
using AionLightning.Game.Network.Aion;
using AionLightning.Game.Network.Aion.ServerPackets;
using AionLightning.Game.QuestEngine;
using AionLightning.Game.QuestEngine.Handlers;
using AionLightning.Game.QuestEngine.Model;
using AionLightning.Game.Services;

namespace Quest.Katalam;

public sealed class _22527WhatitheBox : QuestHandlerBase
{
    private const int QuestIdConst = 22527;
    private const int NpcId        = 801000;
    private const int ItemId       = 182213345;

    private readonly IItemDao _itemDao;

    public _22527WhatitheBox(IDataManager dataManager, IQuestDao questDao, QuestRewardService rewardService, IItemDao itemDao)
        : base(QuestIdConst, dataManager, questDao, rewardService)
    {
        _itemDao = itemDao;
    }

    public override void Register(QuestEngine engine)
    {
        engine.RegisterQuestItem(ItemId, QuestId);
        engine.RegisterQuestNpc(NpcId).OnTalk.Add(QuestId);
    }

    public override async ValueTask<bool> OnItemUseAsync(Player player, int itemId, GsClientConnection conn, CancellationToken ct)
    {
        var entry = player.Quests.Get(QuestId);
        if (entry is null || entry.Status == QuestStatus.NONE)
            await conn.SendAsync(new SM_DIALOG_WINDOW(0, 4, QuestId), ct);
        return true;
    }

    public override async ValueTask<bool> OnDialogAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var player = env.Player;
        var entry  = player.Quests.Get(QuestId);
        int targetId = env.TargetId;
        int targetObjId = env.Target?.ObjectId ?? 0;
        var dialog = DialogActionLookup.FromId(env.DialogId);

        if (targetId == 0)
        {
            if ((entry is null || entry.Status == QuestStatus.NONE) && env.DialogId == (int)DialogAction.QUEST_ACCEPT_1)
            {
                await StartMissionAsync(conn, player, QuestStatus.START, ct);
                await conn.SendAsync(new SM_DIALOG_WINDOW(0, 0), ct);
                return true;
            }
            return false;
        }

        if (entry is null) return false;

        if (entry.Status == QuestStatus.START && targetId == NpcId)
        {
            if (dialog == DialogAction.QUEST_SELECT)
                return await SendQuestDialogAsync(conn, targetObjId, 2375, ct);
            if (dialog == DialogAction.SELECT_QUEST_REWARD)
            {
                await RemoveQuestItemAsync(player, conn, _itemDao, ItemId, 1, ct);
                return await DefaultCloseDialogAsync(env, conn, 0, 1, reward: true, sameNpc: true, ct);
            }
            return false;
        }

        if (entry.Status == QuestStatus.REWARD && targetId == NpcId)
        {
            if (dialog == DialogAction.USE_OBJECT)
                return await SendQuestDialogAsync(conn, targetObjId, 5, ct);
            return await SendQuestEndDialogAsync(env, conn, ct);
        }

        return false;
    }
}
