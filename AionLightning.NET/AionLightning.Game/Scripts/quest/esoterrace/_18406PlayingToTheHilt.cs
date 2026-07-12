// Port of Java data/scripts/system/handlers/quest/esoterrace/_18406PlayingToTheHilt.java (Ritsu).
// Started purely by using quest item 182215003 (target 0, QUEST_ACCEPT_1 creates the entry); turn
// in at 799552 (SELECT_QUEST_REWARD consumes the item and grants). Skip vs Java: the
// SM_ITEM_USAGE_ANIMATION broadcast + 3s scheduled delay before showing the accept dialog are
// dropped (same cosmetic skip as other item-started quests in this port) - the dialog opens
// immediately on item use instead.
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

namespace Quest.Esoterrace;

public sealed class _18406PlayingToTheHilt : QuestHandlerBase
{
    private const int QuestIdConst = 18406;
    private const int TurnInNpc    = 799552;
    private const int HiltItem     = 182215003;

    private readonly IItemDao _itemDao;

    public _18406PlayingToTheHilt(IDataManager dataManager, IQuestDao questDao, QuestRewardService rewardService, IItemDao itemDao)
        : base(QuestIdConst, dataManager, questDao, rewardService)
    {
        _itemDao = itemDao;
    }

    public override void Register(QuestEngine engine)
    {
        engine.RegisterQuestNpc(TurnInNpc).OnTalk.Add(QuestId);
        engine.RegisterQuestItem(HiltItem, QuestId);
    }

    public override async ValueTask<bool> OnItemUseAsync(Player player, int itemId, GsClientConnection conn, CancellationToken ct)
    {
        if (itemId != HiltItem) return false;
        await SendQuestDialogAsync(conn, 0, 4, ct);
        return true;
    }

    public override async ValueTask<bool> OnDialogAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var player      = env.Player;
        int targetId    = env.TargetId;
        int targetObjId = env.Target?.ObjectId ?? 0;
        var dialog      = DialogActionLookup.FromId(env.DialogId);

        if (targetId == 0)
        {
            if (dialog == DialogAction.QUEST_ACCEPT_1)
            {
                await StartMissionAsync(conn, player, QuestStatus.START, ct);
                return await CloseDialogWindowAsync(conn, 0, ct);
            }
            return false;
        }

        if (targetId == TurnInNpc)
        {
            var entry = player.Quests.Get(QuestId);
            if (entry is null) return false;

            if (dialog == DialogAction.QUEST_SELECT && entry.Status == QuestStatus.START)
                return await SendQuestDialogAsync(conn, targetObjId, 2375, ct);
            if (dialog == DialogAction.SELECT_QUEST_REWARD)
            {
                await RemoveQuestItemAsync(player, conn, _itemDao, HiltItem, 1, ct);
                await ChangeQuestStepAsync(conn, entry, 0, 1, toReward: true, ct);
                return await SendQuestEndDialogAsync(env, conn, ct);
            }
            return await SendQuestEndDialogAsync(env, conn, ct);
        }

        return false;
    }
}
