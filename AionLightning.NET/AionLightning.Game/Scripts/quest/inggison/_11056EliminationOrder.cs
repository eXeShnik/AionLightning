// Port of Java data/scripts/system/handlers/quest/inggison/_11056EliminationOrder.java.
// Item-use start (182206842, removed on accept); kill 296493 (var 0->1), 296494 (var 1->2), then
// 296495 (var 2->reward) in sequence; turn-in at 799043 requires 10,000,000 kinah (decreased on
// success, otherwise shows the "not enough" page).
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

namespace Quest.Inggison;

public sealed class _11056EliminationOrder : QuestHandlerBase
{
    private const int QuestIdConst = 11056;
    private const int TurnInNpc    = 799043;
    private const int Mob1         = 296493;
    private const int Mob2         = 296494;
    private const int Mob3         = 296495;
    private const int OrderItem    = 182206842;
    private const int KinahCost    = 10000000;
    private const int KinahItemId  = 182400001;

    private readonly IItemDao _itemDao;

    public _11056EliminationOrder(IDataManager dataManager, IQuestDao questDao, QuestRewardService rewardService, IItemDao itemDao)
        : base(QuestIdConst, dataManager, questDao, rewardService)
    {
        _itemDao = itemDao;
    }

    public override void Register(QuestEngine engine)
    {
        engine.RegisterQuestItem(OrderItem, QuestId);
        engine.RegisterQuestNpc(TurnInNpc).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(Mob1).OnKill.Add(QuestId);
        engine.RegisterQuestNpc(Mob2).OnKill.Add(QuestId);
        engine.RegisterQuestNpc(Mob3).OnKill.Add(QuestId);
    }

    public override async ValueTask<bool> OnItemUseAsync(Player player, int itemId, GsClientConnection conn, CancellationToken ct)
    {
        if (itemId != OrderItem) return false;
        if (player.Quests.Get(QuestId) is not null) return false;
        await conn.SendAsync(new SM_DIALOG_WINDOW(0, 4, QuestId), ct);
        return true;
    }

    public override async ValueTask<bool> OnDialogAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var player = env.Player;
        var entry = player.Quests.Get(QuestId);
        int targetId = env.TargetId;
        int targetObjId = env.Target?.ObjectId ?? 0;
        var dialog = DialogActionLookup.FromId(env.DialogId);

        if (entry is null || entry.Status == QuestStatus.NONE)
        {
            if (targetId == 0 && env.DialogId == (int)DialogAction.QUEST_ACCEPT_1)
            {
                await RemoveQuestItemAsync(player, conn, _itemDao, OrderItem, 1, ct);
                await StartMissionAsync(conn, player, QuestStatus.START, ct);
                return await CloseDialogWindowAsync(conn, 0, ct);
            }
            return false;
        }

        if (entry.Status == QuestStatus.REWARD && targetId == TurnInNpc)
        {
            if (dialog == DialogAction.USE_OBJECT)
                return await SendQuestDialogAsync(conn, targetObjId, 10002, ct);
            if (dialog == DialogAction.SELECT_ACTION_2034)
                return await SendQuestDialogAsync(conn, targetObjId, 2034, ct);
            if (dialog == DialogAction.SELECT_QUEST_REWARD)
            {
                long kinah = player.Inventory.FindByItemId(KinahItemId)?.Count ?? 0;
                if (kinah >= KinahCost)
                {
                    await RemoveQuestItemAsync(player, conn, _itemDao, KinahItemId, KinahCost, ct);
                    return await SendQuestDialogAsync(conn, targetObjId, 5, ct);
                }
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
            0 => await DefaultOnKillEventAsync(env, conn, Mob1, 0, 1, ct),
            1 => await DefaultOnKillEventAsync(env, conn, Mob2, 1, 2, ct),
            2 => await DefaultOnKillEventAsync(env, conn, Mob3, 2, reward: true, ct),
            _ => false,
        };
    }
}
