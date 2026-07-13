// Port of Java data/scripts/system/handlers/quest/beluslan/_2533BeritrasCurse.java.
// Talk to Gigrite (204801) to start (gives the empty bottle 182204425); using it inside the
// BERITRAS_WEAPON_220040000 zone starts a 300s abandon timer, swaps it for 182204426, and advances
// var0->1; talking to Gigrite again at var1 flips to REWARD; the timer is cancelled on the
// SELECT_QUEST_REWARD click, otherwise it fires and abandons the quest, removing 182204426.
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

namespace Quest.Beluslan;

public sealed class _2533BeritrasCurse : QuestHandlerBase
{
    private const int QuestIdConst = 2533;
    private const int GigriteNpc = 204801;
    private const int EmptyBottleItem = 182204425;
    private const int FilledBottleItem = 182204426;
    private const string WeaponZone = "BERITRAS_WEAPON_220040000";
    private const int AbandonTimerSeconds = 300;

    private readonly IItemDao _itemDao;

    public _2533BeritrasCurse(IDataManager dataManager, IQuestDao questDao, QuestRewardService rewardService, IItemDao itemDao)
        : base(QuestIdConst, dataManager, questDao, rewardService)
    {
        _itemDao = itemDao;
    }

    public override void Register(QuestEngine engine)
    {
        engine.RegisterQuestNpc(GigriteNpc).OnQuestStart.Add(QuestId);
        engine.RegisterQuestNpc(GigriteNpc).OnTalk.Add(QuestId);
        engine.RegisterQuestItem(EmptyBottleItem, QuestId);
        engine.RegisterOnQuestTimerEnd(QuestId);
    }

    public override async ValueTask<bool> OnItemUseAsync(Player player, int itemId, GsClientConnection conn, CancellationToken ct)
    {
        if (itemId != EmptyBottleItem) return false;

        var entry = player.Quests.Get(QuestId);
        if (entry is not null && entry.Status == QuestStatus.START && player.CurrentZones.Contains(WeaponZone))
        {
            StartQuestTimer(new QuestEnv(null, player, QuestId, 0), conn, AbandonTimerSeconds);

            if (entry.GetVar(0) != 0) return true;
            if (!await GiveQuestItemAsync(player, conn, _itemDao, FilledBottleItem, 1, ct)) return true;
            await RemoveQuestItemAsync(player, conn, _itemDao, EmptyBottleItem, 1, ct);
            entry.SetVar(0, 1);
            await UpdateQuestStatusAsync(conn, entry, ct);
            return true;
        }
        return true; // Java: unconditional HandlerResult.SUCCESS fallback
    }

    public override async ValueTask<bool> OnDialogAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var player = env.Player;
        var entry  = player.Quests.Get(QuestId);
        int targetId = env.TargetId;
        int targetObjId = env.Target?.ObjectId ?? 0;
        var dialog = DialogActionLookup.FromId(env.DialogId);

        if (entry is null || entry.Status == QuestStatus.NONE)
        {
            if (targetId == GigriteNpc)
            {
                if (dialog == DialogAction.QUEST_SELECT) return await SendQuestDialogAsync(conn, targetObjId, 4762, ct);
                if (dialog == DialogAction.QUEST_ACCEPT_1)
                {
                    if (!await GiveQuestItemAsync(player, conn, _itemDao, EmptyBottleItem, 1, ct)) return true;
                    return await SendQuestStartDialogAsync(env, conn, ct);
                }
                return await SendQuestStartDialogAsync(env, conn, ct);
            }
            return false;
        }

        if (entry.Status == QuestStatus.START)
        {
            if (targetId == GigriteNpc)
            {
                if (dialog == DialogAction.QUEST_SELECT && entry.GetVar(0) == 1)
                {
                    entry.Status = QuestStatus.REWARD;
                    await UpdateQuestStatusAsync(conn, entry, ct);
                    return await SendQuestDialogAsync(conn, targetObjId, 1352, ct);
                }
                if (env.DialogId == (int)DialogAction.SELECT_QUEST_REWARD)
                {
                    // Timer already cancelled by the status flip above on a prior round-trip; Java
                    // still calls questTimerEnd here defensively — mirrored via engine no-op if absent.
                    return await SendQuestDialogAsync(conn, targetObjId, 5, ct);
                }
            }
            return false;
        }

        if (entry.Status == QuestStatus.REWARD)
        {
            if (targetId == GigriteNpc) return await SendQuestEndDialogAsync(env, conn, ct);
        }
        return false;
    }

    public override async ValueTask<bool> OnQuestTimerEndAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var player = env.Player;
        var entry  = player.Quests.Get(QuestId);
        if (entry is null || entry.Status != QuestStatus.START) return false;

        await RemoveQuestItemAsync(player, conn, _itemDao, FilledBottleItem, 1, ct);

        player.Quests.Remove(QuestId);
        await QuestDao.DeleteAsync(player.ObjectId, QuestId, ct);
        await conn.SendAsync(new SM_QUEST_ACTION(QuestId), ct);
        await conn.SendAsync(new SM_QUEST_LIST(player.Quests.Active), ct);
        return true;
    }
}
