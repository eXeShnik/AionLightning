// Port of Java data/scripts/system/handlers/quest/eltnen/_1361FindingDrinkingWater.java (Xitanium).
// Standalone quest: Turiel (203943) starts it (handing out the Empty Bucket, 182201326) and
// finishes it; using the bucket inside "LF2_ITEMUSEAREA_Q1361" swaps it for the Filled Bucket
// (182201327, var 0->1); using the Water Tank object (700173) at var 1 consumes the filled bucket
// and flips straight to REWARD.
// Skip vs Java: the 3s SM_ITEM_USAGE_ANIMATION cast delay on the bucket use is applied on the same
// tick instead - the same simplification UseQuestObjectAsync's doc comment already documents.
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

namespace Quest.Eltnen;

public sealed class _1361FindingDrinkingWater : QuestHandlerBase
{
    private const int QuestIdConst  = 1361;
    private const int TurielNpc     = 203943;
    private const int WaterTankObj  = 700173;
    private const int EmptyBucketItem  = 182201326;
    private const int FilledBucketItem = 182201327;
    private const string ItemUseZone = "LF2_ITEMUSEAREA_Q1361";

    private readonly IItemDao _itemDao;

    public _1361FindingDrinkingWater(IDataManager dataManager, IQuestDao questDao, QuestRewardService rewardService, IItemDao itemDao)
        : base(QuestIdConst, dataManager, questDao, rewardService)
    {
        _itemDao = itemDao;
    }

    public override void Register(QuestEngine engine)
    {
        engine.RegisterQuestItem(EmptyBucketItem, QuestId);
        engine.RegisterQuestNpc(TurielNpc).OnQuestStart.Add(QuestId);
        engine.RegisterQuestNpc(TurielNpc).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(WaterTankObj).OnTalk.Add(QuestId);
    }

    public override async ValueTask<bool> OnItemUseAsync(Player player, int itemId, GsClientConnection conn, CancellationToken ct)
    {
        if (itemId != EmptyBucketItem) return false;
        if (!player.CurrentZones.Contains(ItemUseZone)) return false;

        var entry = player.Quests.Get(QuestId);
        if (entry is null) return false;

        await RemoveQuestItemAsync(player, conn, _itemDao, EmptyBucketItem, 1, ct);
        if (!await GiveQuestItemAsync(player, conn, _itemDao, FilledBucketItem, 1, ct)) return false;

        entry.SetVar(0, 1);
        await UpdateQuestStatusAsync(conn, entry, ct);
        return true;
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
            if (targetId != TurielNpc) return false;
            if (dialog == DialogAction.QUEST_SELECT)
                return await SendQuestDialogAsync(conn, targetObjId, 1011, ct);
            if (dialog == DialogAction.QUEST_ACCEPT_1)
            {
                if (!await GiveQuestItemAsync(player, conn, _itemDao, EmptyBucketItem, 1, ct)) return true;
                return await SendQuestStartDialogAsync(env, conn, ct);
            }
            return await SendQuestStartDialogAsync(env, conn, ct);
        }

        if (entry.Status == QuestStatus.REWARD)
        {
            if (dialog == DialogAction.QUEST_SELECT)
                return await SendQuestDialogAsync(conn, targetObjId, 2375, ct);
            if (dialog == DialogAction.SELECT_QUEST_REWARD)
            {
                entry.SetVar(0, 2);
                await UpdateQuestStatusAsync(conn, entry, ct);
                return await SendQuestEndDialogAsync(env, conn, ct);
            }
            return await SendQuestEndDialogAsync(env, conn, ct);
        }

        if (entry.Status == QuestStatus.START && entry.GetVar(0) == 1 && targetId == WaterTankObj)
        {
            if (dialog == DialogAction.USE_OBJECT)
                return await UseQuestObjectAsync(env, conn, step: 1, nextStep: 1, reward: true, varNum: 0,
                    addItemId: 0, addItemCount: 0, removeItemId: FilledBucketItem, removeItemCount: 1,
                    movieId: 0, dieObject: false, _itemDao, ct);
        }

        return false;
    }
}
