// Port of Java data/scripts/system/handlers/quest/esoterrace/_28406FindersFee.java (Ritsu). Elyos
// mirror of _18406PlayingToTheHilt: started purely by using quest item 182215015 (target 0,
// QUEST_ACCEPT_1 creates the entry); turn in at 799557 (SELECT_QUEST_REWARD consumes the item and
// grants). Skip vs Java: the SM_ITEM_USAGE_ANIMATION broadcast + 3s scheduled delay before showing
// the accept dialog are dropped (same cosmetic skip as other item-started quests in this port) -
// the dialog opens immediately on item use instead.
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

public sealed class _28406FindersFee : QuestHandlerBase
{
    private const int QuestIdConst = 28406;
    private const int TurnInNpc    = 799557;
    private const int FeeItem      = 182215015;

    private readonly IItemDao _itemDao;

    public _28406FindersFee(IDataManager dataManager, IQuestDao questDao, QuestRewardService rewardService, IItemDao itemDao)
        : base(QuestIdConst, dataManager, questDao, rewardService)
    {
        _itemDao = itemDao;
    }

    public override void Register(QuestEngine engine)
    {
        engine.RegisterQuestNpc(TurnInNpc).OnTalk.Add(QuestId);
        engine.RegisterQuestItem(FeeItem, QuestId);
    }

    public override async ValueTask<bool> OnItemUseAsync(Player player, int itemId, GsClientConnection conn, CancellationToken ct)
    {
        if (itemId != FeeItem) return false;
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
                await RemoveQuestItemAsync(player, conn, _itemDao, FeeItem, 1, ct);
                await ChangeQuestStepAsync(conn, entry, 0, 1, toReward: true, ct);
                return await SendQuestEndDialogAsync(env, conn, ct);
            }
            return await SendQuestEndDialogAsync(env, conn, ct);
        }

        return false;
    }
}
