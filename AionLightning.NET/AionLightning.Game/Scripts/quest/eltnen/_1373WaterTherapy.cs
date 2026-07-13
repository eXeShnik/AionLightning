// Port of Java data/scripts/system/handlers/quest/eltnen/_1373WaterTherapy.java (Ritsu).
// Standalone quest: Aerope (203949) starts it (handing out the Dry Towel, 182201372) and finishes
// it; using the towel inside "LF2_ITEMUSEAREA_Q1373" consumes it, grants the Soaked Towel
// (182201373, var 0->2) and starts a 180s abandon-timer; handing in the soaked towel (via
// CHECK_USER_HAS_QUEST_ITEM) flips to REWARD.
// Deviation: Java's QuestService.questTimerEnd(env) call (cancelling the pending abandon-timer once
// the soaked towel is turned in) has no cancellation primitive in this port's fire-and-forget
// StartQuestTimer - the timer still fires later, but OnQuestTimerEndAsync only acts while the quest
// is still START, so it harmlessly no-ops once the quest has moved to REWARD/COMPLETE.
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

namespace Quest.Eltnen;

public sealed class _1373WaterTherapy : QuestHandlerBase
{
    private const int QuestIdConst  = 1373;
    private const int AeropeNpc     = 203949;
    private const int DryTowelItem    = 182201372;
    private const int SoakedTowelItem = 182201373;
    private const string ItemUseZone = "LF2_ITEMUSEAREA_Q1373";

    private readonly IItemDao _itemDao;

    public _1373WaterTherapy(IDataManager dataManager, IQuestDao questDao, QuestRewardService rewardService, IItemDao itemDao)
        : base(QuestIdConst, dataManager, questDao, rewardService)
    {
        _itemDao = itemDao;
    }

    public override void Register(QuestEngine engine)
    {
        engine.RegisterQuestNpc(AeropeNpc).OnQuestStart.Add(QuestId);
        engine.RegisterQuestNpc(AeropeNpc).OnTalk.Add(QuestId);
        engine.RegisterQuestItem(DryTowelItem, QuestId);
        engine.RegisterOnQuestTimerEnd(QuestId);
    }

    public override async ValueTask<bool> OnItemUseAsync(Player player, int itemId, GsClientConnection conn, CancellationToken ct)
    {
        if (itemId != DryTowelItem) return false;

        var entry = player.Quests.Get(QuestId);
        if (entry is null || entry.Status != QuestStatus.START) return false;
        if (!player.CurrentZones.Contains(ItemUseZone)) return false;

        await RemoveQuestItemAsync(player, conn, _itemDao, DryTowelItem, 1, ct);
        var env = new QuestEnv(null, player, QuestId, 0);
        await UseQuestObjectAsync(env, conn, step: 0, nextStep: 2, reward: false, varNum: 0,
            addItemId: SoakedTowelItem, addItemCount: 1, removeItemId: 0, removeItemCount: 0,
            movieId: 0, dieObject: false, _itemDao, ct);
        StartQuestTimer(env, conn, 180);
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
            if (targetId != AeropeNpc) return false;
            if (dialog == DialogAction.QUEST_SELECT)
                return await SendQuestDialogAsync(conn, targetObjId, 1011, ct);
            if (dialog == DialogAction.QUEST_ACCEPT_1)
            {
                if (!await GiveQuestItemAsync(player, conn, _itemDao, DryTowelItem, 1, ct)) return true;
                return await SendQuestStartDialogAsync(env, conn, ct);
            }
            return await SendQuestStartDialogAsync(env, conn, ct);
        }

        if (entry.Status == QuestStatus.START)
        {
            if (targetId != AeropeNpc || entry.GetVar(0) != 2) return false;
            if (dialog == DialogAction.QUEST_SELECT)
                return await SendQuestDialogAsync(conn, targetObjId, 2375, ct);
            if (dialog == DialogAction.CHECK_USER_HAS_QUEST_ITEM)
            {
                var towel = player.Inventory.FindByItemId(SoakedTowelItem);
                if (towel is { Count: 1 })
                    return await CheckQuestItemsAsync(env, conn, _itemDao, 2, 3, reward: true, checkOkId: 5, checkFailId: 2716, ct);
                return false;
            }
            return await SendQuestEndDialogAsync(env, conn, ct);
        }

        if (entry.Status == QuestStatus.REWARD)
            return targetId == AeropeNpc && await SendQuestEndDialogAsync(env, conn, ct);

        return false;
    }

    public override async ValueTask<bool> OnQuestTimerEndAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var player = env.Player;
        var entry  = player.Quests.Get(QuestId);
        if (entry is null || entry.Status != QuestStatus.START) return false;

        await RemoveQuestItemAsync(player, conn, _itemDao, SoakedTowelItem, 1, ct);
        player.Quests.Remove(QuestId);
        await QuestDao.DeleteAsync(player.ObjectId, QuestId, ct);
        await conn.SendAsync(new SM_QUEST_ACTION(QuestId), ct);
        await conn.SendAsync(new SM_QUEST_LIST(player.Quests.Active), ct);
        return true;
    }
}
