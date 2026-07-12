// Port of Java data/scripts/system/handlers/quest/heiron/_1687TheTigrakiAgreement.java.
// Talk to Brosia (204601) to start; hand in 2x186000035 + 5x186000036 to unlock the reward-tier
// choice (SETPRO10/20/30 picks tier 0/1/2), then turn in at Brosia.
// Java bug: the original handler stores the chosen reward tier in a per-handler instance field
// (`rewardGroup`), which is shared across every player using this singleton quest script — a race
// condition where one player's choice can leak into another's concurrently. Fixed by storing the
// choice in the player's own quest entry (var 1) instead.
// Skip vs Java: qs.canRepeat() (daily-repeat/cooldown) isn't ported, so only a first-time run is
// offered — still fully completable once.
using System.Threading;
using System.Threading.Tasks;
using AionLightning.Game.Dao;
using AionLightning.Game.DataHolders;
using AionLightning.Game.Model.Quest;
using AionLightning.Game.Network.Aion;
using AionLightning.Game.QuestEngine;
using AionLightning.Game.QuestEngine.Handlers;
using AionLightning.Game.QuestEngine.Model;
using AionLightning.Game.Services;

namespace Quest.Heiron;

public sealed class _1687TheTigrakiAgreement : QuestHandlerBase
{
    private const int QuestIdConst = 1687;
    private const int BrosiaNpc = 204601;
    private const int BloodItem  = 186000035;
    private const int ScalesItem = 186000036;

    private readonly IItemDao _itemDao;

    public _1687TheTigrakiAgreement(IDataManager dataManager, IQuestDao questDao, QuestRewardService rewardService, IItemDao itemDao)
        : base(QuestIdConst, dataManager, questDao, rewardService)
    {
        _itemDao = itemDao;
    }

    public override void Register(QuestEngine engine)
    {
        engine.RegisterQuestNpc(BrosiaNpc).OnQuestStart.Add(QuestId);
        engine.RegisterQuestNpc(BrosiaNpc).OnTalk.Add(QuestId);
    }

    public override async ValueTask<bool> OnDialogAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var player = env.Player;
        if (env.TargetId != BrosiaNpc) return false;
        int targetObjId = env.Target?.ObjectId ?? 0;
        var entry  = player.Quests.Get(QuestId);
        var dialog = DialogActionLookup.FromId(env.DialogId);

        if (entry is null || entry.Status == QuestStatus.NONE)
        {
            if (dialog == DialogAction.QUEST_SELECT)
                return await SendQuestDialogAsync(conn, targetObjId, 4762, ct);
            return await SendQuestStartDialogAsync(env, conn, ct);
        }

        if (entry.Status == QuestStatus.START)
        {
            int var = entry.GetVar(0);
            if (dialog == DialogAction.QUEST_SELECT)
                return await SendQuestDialogAsync(conn, targetObjId, 1011, ct);
            if (dialog == DialogAction.CHECK_USER_HAS_QUEST_ITEM)
            {
                bool hasBlood  = player.Inventory.FindByItemId(BloodItem) is { Count: >= 2 };
                bool hasScales = player.Inventory.FindByItemId(ScalesItem) is { Count: >= 5 };
                if (hasBlood && hasScales)
                {
                    await RemoveQuestItemAsync(player, conn, _itemDao, BloodItem, 2, ct);
                    await RemoveQuestItemAsync(player, conn, _itemDao, ScalesItem, 5, ct);
                    return await SendQuestDialogAsync(conn, targetObjId, 1352, ct);
                }
                return await SendQuestDialogAsync(conn, targetObjId, 1097, ct);
            }
            if (dialog == DialogAction.FINISH_DIALOG)
                return await DefaultCloseDialogAsync(env, conn, var, var, ct);
            if (dialog == DialogAction.SETPRO10)
                return await FinishRewardTierAsync(env, conn, entry, var, 0, ct);
            if (dialog == DialogAction.SETPRO20)
                return await FinishRewardTierAsync(env, conn, entry, var, 1, ct);
            if (dialog == DialogAction.SETPRO30)
                return await FinishRewardTierAsync(env, conn, entry, var, 2, ct);
        }
        else if (entry.Status == QuestStatus.REWARD)
        {
            return await FinishQuestAsync(conn, player, entry.GetVar(1), ct);
        }
        return false;
    }

    private async ValueTask<bool> FinishRewardTierAsync(QuestEnv env, GsClientConnection conn, QuestEntry entry, int var, int rewardIndex, CancellationToken ct)
    {
        entry.SetVar(1, rewardIndex);
        await ChangeQuestStepAsync(conn, entry, 0, var, toReward: true, ct);
        return await FinishQuestAsync(conn, env.Player, rewardIndex, ct);
    }
}
