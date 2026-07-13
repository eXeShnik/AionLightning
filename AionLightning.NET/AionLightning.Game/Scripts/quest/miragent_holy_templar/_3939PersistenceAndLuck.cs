// Port of Java data/scripts/system/handlers/quest/miragent_holy_templar/_3939PersistenceAndLuck.java
// (Nanou, reworked vlog, modified Gigi). Start at Lavirintos (203701); Cornelius (203780, var 0->1,
// then a quest_data.xml collect-item check at var 2 hands out an empty vessel — Java's
// QuestService.collectItemCheck via QuestHandlerBase.CheckQuestItemsAsync); Sabotes (203781, var
// 1->2, pays 3400000 kinah); a quest device (700537, var 2, USE_OBJECT no-op fills the vessel); back
// at Cornelius (var 2->3, consumes the Tear of Luck collect-items); report to Jucleas (203752) with
// the Oath Stone (186000080, removed) to flip to reward; turn in at Lavirintos.
// Java's reward-status turn-in at Lavirintos checks QUEST_SELECT (not USE_OBJECT, unlike most sibling
// quests in this zone) before falling back to sendQuestEndDialog — kept as authored.
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

namespace Quest.MiragentHolyTemplar;

public sealed class _3939PersistenceAndLuck : QuestHandlerBase
{
    private const int QuestIdConst  = 3939;
    private const int LavirintosNpc = 203701;
    private const int CorneliusNpc  = 203780;
    private const int SabotesNpc    = 203781;
    private const int DeviceNpc     = 700537;
    private const int JucleasNpc    = 203752;
    private const int EmptyVesselItem = 122001274;
    private const int OathStoneItem = 186000080;
    private const int KinahItemId   = 182400001;
    private const long KinahCost    = 3400000;

    private readonly IItemDao _itemDao;

    public _3939PersistenceAndLuck(IDataManager dataManager, IQuestDao questDao, QuestRewardService rewardService, IItemDao itemDao)
        : base(QuestIdConst, dataManager, questDao, rewardService)
    {
        _itemDao = itemDao;
    }

    public override void Register(QuestEngine engine)
    {
        engine.RegisterQuestNpc(LavirintosNpc).OnQuestStart.Add(QuestId);
        engine.RegisterQuestNpc(LavirintosNpc).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(CorneliusNpc).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(SabotesNpc).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(JucleasNpc).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(DeviceNpc).OnTalk.Add(QuestId);
    }

    public override async ValueTask<bool> OnDialogAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var player      = env.Player;
        var entry       = player.Quests.Get(QuestId);
        int targetId    = env.TargetId;
        int targetObjId = env.Target?.ObjectId ?? 0;
        var dialog      = DialogActionLookup.FromId(env.DialogId);

        if (entry is null)
        {
            if (targetId != LavirintosNpc) return false;
            if (dialog == DialogAction.QUEST_SELECT)
                return await SendQuestDialogAsync(conn, targetObjId, 4762, ct);
            return await SendQuestStartDialogAsync(env, conn, ct);
        }

        if (entry.Status == QuestStatus.START)
        {
            int var = entry.GetVar(0);
            if (targetId == CorneliusNpc)
            {
                if (var == 0)
                {
                    if (dialog == DialogAction.QUEST_SELECT)
                        return await SendQuestDialogAsync(conn, targetObjId, 1011, ct);
                    if (dialog == DialogAction.SETPRO1)
                        return await DefaultCloseDialogAsync(env, conn, 0, 1, ct);
                }
                if (var == 2)
                {
                    if (dialog == DialogAction.QUEST_SELECT)
                        return await SendQuestDialogAsync(conn, targetObjId, 1693, ct);
                    if (dialog == DialogAction.CHECK_USER_HAS_QUEST_ITEM)
                        return await CheckQuestItemsAsync(env, conn, _itemDao, 2, 3, reward: false, 10000, 10001, EmptyVesselItem, 1, ct);
                }
                if (dialog == DialogAction.FINISH_DIALOG)
                    return await DefaultCloseDialogAsync(env, conn, var, var, ct);
                return false;
            }
            if (targetId == SabotesNpc)
            {
                if (dialog == DialogAction.QUEST_SELECT && var == 1)
                    return await SendQuestDialogAsync(conn, targetObjId, 1352, ct);
                if (dialog == DialogAction.SELECT_ACTION_1354)
                {
                    if (var == 1 && await TryDeductKinahAsync(player, conn, ct))
                        return await DefaultCloseDialogAsync(env, conn, _itemDao, 1, 2, reward: false, sameNpc: false,
                            giveItemId: EmptyVesselItem, giveItemCount: 1, removeItemId: 0, removeItemCount: 0, ct);
                    return await SendQuestDialogAsync(conn, targetObjId, 1438, ct);
                }
                if (dialog == DialogAction.FINISH_DIALOG)
                    return await DefaultCloseDialogAsync(env, conn, 1, 1, ct);
                return false;
            }
            if (targetId == DeviceNpc)
            {
                if (dialog == DialogAction.USE_OBJECT && var == 2)
                    return await UseQuestObjectAsync(env, conn, 2, 2, reward: false, varNum: 0, ct);
                return false;
            }
            if (targetId == JucleasNpc)
            {
                switch (dialog)
                {
                    case DialogAction.QUEST_SELECT:
                        if (var == 3)
                            return await SendQuestDialogAsync(conn, targetObjId, 2034, ct);
                        return false;
                    case DialogAction.SET_SUCCEED:
                        if (HasItem(player, OathStoneItem, 1))
                        {
                            await RemoveQuestItemAsync(player, conn, _itemDao, OathStoneItem, 1, ct);
                            return await DefaultCloseDialogAsync(env, conn, 3, 3, reward: true, sameNpc: false, ct);
                        }
                        return await SendQuestDialogAsync(conn, targetObjId, 2120, ct);
                    case DialogAction.FINISH_DIALOG:
                        return await SendQuestSelectionDialogAsync(conn, targetObjId, ct);
                    default:
                        return false;
                }
            }
            return await SendQuestStartDialogAsync(env, conn, ct);
        }

        if (entry.Status == QuestStatus.REWARD && targetId == LavirintosNpc)
        {
            if (dialog == DialogAction.QUEST_SELECT)
                return await SendQuestDialogAsync(conn, targetObjId, 10002, ct);
            return await SendQuestEndDialogAsync(env, conn, ct);
        }
        return false;
    }

    private static bool HasItem(Player player, int itemId, long count)
    {
        var item = player.Inventory.FindByItemId(itemId);
        return item is not null && item.Count >= count;
    }

    /// <summary>Java Inventory.tryDecreaseKinah(amount): deducts kinah if the player has enough, persisting the change.</summary>
    private async ValueTask<bool> TryDeductKinahAsync(Player player, GsClientConnection conn, CancellationToken ct)
    {
        var kinah = player.Inventory.FindByItemId(KinahItemId);
        if (kinah is null || kinah.Count < KinahCost) return false;

        kinah.Count -= KinahCost;
        await _itemDao.SaveAllAsync(player.ObjectId, player.Inventory.All, ct);
        await conn.SendAsync(new SM_INVENTORY_ADD_ITEM([kinah]), ct);
        return true;
    }
}
