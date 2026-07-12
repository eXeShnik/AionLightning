// Port of Java data/scripts/system/handlers/quest/sanctum/_3964GrowthFlorasFourthCharm.java (undertrey / vlog).
// Talk to Flora (798384) to start; Erdos (203740) hands over a charm-request slip (item
// 182206111, var 0->1); bring it back to Flora with a Yellow Aether Powder (186000090) and 90000
// kinah to advance to REWARD. Java bug fixes: same as _3963GrowthFlorasThirdCharm -- the original
// decreased kinah before checking the powder count, and called tryDecreaseKinah(90000) *and*
// decreaseKinah(90000) back to back (charging 180000 instead of 90000); this port checks the
// powder first and deducts kinah exactly once.
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

namespace Quest.Sanctum;

public sealed class _3964GrowthFlorasFourthCharm : QuestHandlerBase
{
    private const int QuestIdConst = 3964;
    private const int FloraNpc     = 798384;
    private const int ErdosNpc     = 203740;
    private const int RequestItemId = 182206111;
    private const int PowderItemId  = 186000090;
    private const int KinahCost    = 90000;
    private const int KinahItemId  = 182400001;

    private readonly IItemDao _itemDao;

    public _3964GrowthFlorasFourthCharm(IDataManager dataManager, IQuestDao questDao, QuestRewardService rewardService, IItemDao itemDao)
        : base(QuestIdConst, dataManager, questDao, rewardService)
    {
        _itemDao = itemDao;
    }

    public override void Register(QuestEngine engine)
    {
        engine.RegisterQuestNpc(FloraNpc).OnQuestStart.Add(QuestId);
        engine.RegisterQuestNpc(FloraNpc).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(ErdosNpc).OnTalk.Add(QuestId);
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
            if (targetId == FloraNpc)
            {
                if (dialog == DialogAction.QUEST_SELECT)
                    return await SendQuestDialogAsync(conn, targetObjId, 1011, ct);
                return await SendQuestStartDialogAsync(env, conn, ct);
            }
            return false;
        }

        if (entry.Status == QuestStatus.START)
        {
            int var = entry.GetVar(0);
            if (targetId == ErdosNpc)
            {
                if (dialog == DialogAction.QUEST_SELECT && var == 0)
                    return await SendQuestDialogAsync(conn, targetObjId, 1352, ct);
                if (dialog == DialogAction.SETPRO1)
                    return await DefaultCloseDialogAsync(env, conn, _itemDao, 0, 1, reward: false, sameNpc: false,
                        giveItemId: RequestItemId, giveItemCount: 1, removeItemId: 0, removeItemCount: 0, ct);
            }
            else if (targetId == FloraNpc)
            {
                if (dialog == DialogAction.QUEST_SELECT && var == 1)
                {
                    await RemoveQuestItemAsync(player, conn, _itemDao, RequestItemId, 1, ct);
                    return await SendQuestDialogAsync(conn, targetObjId, 2375, ct);
                }
                if (dialog == DialogAction.CHECK_USER_HAS_QUEST_ITEM)
                {
                    long powderCount = player.Inventory.FindByItemId(PowderItemId)?.Count ?? 0;
                    if (var == 1 && powderCount >= 1 && await TryDecreaseKinahAsync(player, conn, KinahCost, ct))
                    {
                        await RemoveQuestItemAsync(player, conn, _itemDao, PowderItemId, 1, ct);
                        await ChangeQuestStepAsync(conn, entry, 0, 1, toReward: true, ct);
                        return await SendQuestDialogAsync(conn, targetObjId, 5, ct);
                    }
                    return await SendQuestDialogAsync(conn, targetObjId, 2716, ct);
                }
                if (dialog == DialogAction.FINISH_DIALOG)
                    return await DefaultCloseDialogAsync(env, conn, 1, 1, ct);
            }
        }

        if (entry.Status == QuestStatus.REWARD && targetId == FloraNpc)
            return await SendQuestEndDialogAsync(env, conn, ct);

        return false;
    }

    private async ValueTask<bool> TryDecreaseKinahAsync(Player player, GsClientConnection conn, long amount, CancellationToken ct)
    {
        var kinah = player.Inventory.FindByItemId(KinahItemId);
        if ((kinah?.Count ?? 0) < amount) return false;
        return await RemoveQuestItemAsync(player, conn, _itemDao, KinahItemId, amount, ct);
    }
}
