// Port of Java data/scripts/system/handlers/quest/gelkmaros/_21057FundinOldGrudge.java (Cheatkiller).
// Item-use quest: using item 182207846 removes it and starts the quest directly (no accept
// dialog). Kill chain 296489 (var 0->1) -> 296490 (var 1->2) -> 296491 (var 2, reward). Turn in
// at 799354: paying 15,000,000 kinah unlocks the real reward page; otherwise shows the
// insufficient-funds dialog.
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

namespace Quest.Gelkmaros;

public sealed class _21057FundinOldGrudge : QuestHandlerBase
{
    private const int QuestIdConst = 21057;
    private const int NpcId        = 799354;
    private const int ItemId       = 182207846;
    private const int KinahItemId  = 182400001;
    private const long KinahCost   = 15000000;
    private static readonly int[] MobIds = [296489, 296490, 296491];

    private readonly IItemDao _itemDao;

    public _21057FundinOldGrudge(IDataManager dataManager, IQuestDao questDao, QuestRewardService rewardService, IItemDao itemDao)
        : base(QuestIdConst, dataManager, questDao, rewardService)
    {
        _itemDao = itemDao;
    }

    public override void Register(QuestEngine engine)
    {
        engine.RegisterQuestItem(ItemId, QuestId);
        engine.RegisterQuestNpc(NpcId).OnTalk.Add(QuestId);
        foreach (int mobId in MobIds)
            engine.RegisterQuestNpc(mobId).OnKill.Add(QuestId);
    }

    public override async ValueTask<bool> OnItemUseAsync(Player player, int itemId, GsClientConnection conn, CancellationToken ct)
    {
        if (itemId != ItemId) return false;
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
            if (entry is null && env.DialogId == (int)DialogAction.QUEST_ACCEPT_1)
            {
                await RemoveQuestItemAsync(player, conn, _itemDao, ItemId, 1, ct);
                await StartMissionAsync(conn, player, QuestStatus.START, ct);
                await conn.SendAsync(new SM_DIALOG_WINDOW(0, 0), ct);
                return true;
            }
            return false;
        }

        if (entry is null) return false;

        if (entry.Status == QuestStatus.REWARD && targetId == NpcId)
        {
            if (dialog == DialogAction.USE_OBJECT)
                return await SendQuestDialogAsync(conn, targetObjId, 10002, ct);
            if (dialog == DialogAction.SELECT_ACTION_2034)
                return await SendQuestDialogAsync(conn, targetObjId, 2034, ct);
            if (dialog == DialogAction.SELECT_QUEST_REWARD)
            {
                if (await TryPayKinahAsync(player, conn, KinahCost, ct))
                    return await SendQuestDialogAsync(conn, targetObjId, 5, ct);
                return await SendQuestDialogAsync(conn, targetObjId, 3739, ct);
            }
            return await SendQuestEndDialogAsync(env, conn, ct);
        }

        return false;
    }

    public override async ValueTask<bool> OnKillAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var entry = env.Player.Quests.Get(QuestId);
        if (entry is null || entry.Status != QuestStatus.START) return false;

        int var = entry.GetVar(0);
        return var switch
        {
            0 => await DefaultOnKillEventAsync(env, conn, MobIds[0], 0, 1, ct),
            1 => await DefaultOnKillEventAsync(env, conn, MobIds[1], 1, 2, ct),
            2 => await DefaultOnKillEventAsync(env, conn, MobIds[2], 2, reward: true, ct),
            _ => false,
        };
    }

    private async ValueTask<bool> TryPayKinahAsync(Player player, GsClientConnection conn, long amount, CancellationToken ct)
    {
        var kinahItem = player.Inventory.FindByItemId(KinahItemId);
        if ((kinahItem?.Count ?? 0) < amount) return false;

        kinahItem!.Count -= amount;
        await _itemDao.SaveAllAsync(player.ObjectId, player.Inventory.All, ct);
        await conn.SendAsync(new SM_INVENTORY_ADD_ITEM([kinahItem]), ct);
        return true;
    }
}
