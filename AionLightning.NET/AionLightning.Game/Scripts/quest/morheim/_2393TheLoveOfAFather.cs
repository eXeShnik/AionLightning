// Port of Java data/scripts/system/handlers/quest/morheim/_2393TheLoveOfAFather.java (Nephis and AU quest helper Team).
// Offered at 204343 (OnQuestStart + OnTalk): accepting hands out item 182204162. Using it while
// inside DF2_ITEMUSEAREA_Q2393 swaps it for item 182204163 and flips straight to REWARD. Turn in at
// 204343 (a redundant var bump to 2 precedes the actual reward grant, matching Java exactly).
// Skip vs Java: the 3s SM_ITEM_USAGE_ANIMATION pair around the item-use swap is collapsed into an
// immediate effect (same simplification as _2321SpyTheSpiritsLetter/_2435TheBlueVineNecklace).
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

namespace Quest.Morheim;

public sealed class _2393TheLoveOfAFather : QuestHandlerBase
{
    private const int QuestIdConst = 2393;
    private const int FatherNpc    = 204343;
    private const int LetterItem   = 182204162;
    private const int TokenItem    = 182204163;
    private const string ItemUseZone = "DF2_ITEMUSEAREA_Q2393";

    private readonly IItemDao _itemDao;

    public _2393TheLoveOfAFather(IDataManager dataManager, IQuestDao questDao, QuestRewardService rewardService, IItemDao itemDao)
        : base(QuestIdConst, dataManager, questDao, rewardService)
    {
        _itemDao = itemDao;
    }

    public override void Register(QuestEngine engine)
    {
        engine.RegisterQuestItem(LetterItem, QuestId);
        engine.RegisterQuestNpc(FatherNpc).OnQuestStart.Add(QuestId);
        engine.RegisterQuestNpc(FatherNpc).OnTalk.Add(QuestId);
    }

    public override async ValueTask<bool> OnItemUseAsync(Player player, int itemId, GsClientConnection conn, CancellationToken ct)
    {
        if (itemId != LetterItem) return false;
        if (!player.CurrentZones.Contains(ItemUseZone)) return false;
        var entry = player.Quests.Get(QuestId);
        if (entry is null) return false;

        await RemoveQuestItemAsync(player, conn, _itemDao, LetterItem, 1, ct);
        await GiveQuestItemAsync(player, conn, _itemDao, TokenItem, 1, ct);
        entry.Status = QuestStatus.REWARD;
        await UpdateQuestStatusAsync(conn, entry, ct);
        return true;
    }

    public override async ValueTask<bool> OnDialogAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var player = env.Player;
        if (env.TargetId != FatherNpc) return false;

        var entry = player.Quests.Get(QuestId);
        int targetObjId = env.Target?.ObjectId ?? 0;
        var dialog = DialogActionLookup.FromId(env.DialogId);

        if (entry is null || entry.Status == QuestStatus.NONE)
        {
            if (dialog == DialogAction.QUEST_SELECT)
                return await SendQuestDialogAsync(conn, targetObjId, 4762, ct);
            if (env.DialogId == (int)DialogAction.QUEST_ACCEPT_1)
            {
                if (await GiveQuestItemAsync(player, conn, _itemDao, LetterItem, 1, ct))
                    return await SendQuestStartDialogAsync(env, conn, ct);
                return true;
            }
            return await SendQuestStartDialogAsync(env, conn, ct);
        }

        if (entry.Status == QuestStatus.REWARD)
        {
            if (dialog == DialogAction.QUEST_SELECT)
                return await SendQuestDialogAsync(conn, targetObjId, 2375, ct);
            if (env.DialogId == (int)DialogAction.SELECT_QUEST_REWARD)
            {
                entry.SetVar(0, 2);
                entry.Status = QuestStatus.REWARD;
                await UpdateQuestStatusAsync(conn, entry, ct);
                return await SendQuestEndDialogAsync(env, conn, ct);
            }
            return await SendQuestEndDialogAsync(env, conn, ct);
        }

        return false;
    }
}
