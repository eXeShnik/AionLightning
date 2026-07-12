// Port of Java data/scripts/system/handlers/quest/katalam/_12525CleaningUpTheirMesses.java (Eloann).
// Accepting at 801022 grants the Staff of Purification (182213313, Java's
// sendQuestStartDialog(env, itemId, count) overload); using it flips to REWARD; turning in at
// 801022 removes the item.
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

namespace Quest.Katalam;

public sealed class _12525CleaningUpTheirMesses : QuestHandlerBase
{
    private const int QuestIdConst = 12525;
    private const int NpcId        = 801022;
    private const int StaffItemId  = 182213313;

    private readonly IItemDao _itemDao;

    public _12525CleaningUpTheirMesses(IDataManager dataManager, IQuestDao questDao, QuestRewardService rewardService, IItemDao itemDao)
        : base(QuestIdConst, dataManager, questDao, rewardService)
    {
        _itemDao = itemDao;
    }

    public override void Register(QuestEngine engine)
    {
        engine.RegisterQuestNpc(NpcId).OnQuestStart.Add(QuestId);
        engine.RegisterQuestNpc(NpcId).OnTalk.Add(QuestId);
        engine.RegisterQuestItem(StaffItemId, QuestId);
    }

    public override async ValueTask<bool> OnDialogAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var player = env.Player;
        var entry  = player.Quests.Get(QuestId);
        int targetId = env.TargetId;
        int targetObjId = env.Target?.ObjectId ?? 0;
        var dialog = DialogActionLookup.FromId(env.DialogId);

        if (targetId != NpcId) return false;

        if (entry is null || entry.Status == QuestStatus.NONE)
        {
            if (dialog == DialogAction.QUEST_SELECT)
                return await SendQuestDialogAsync(conn, targetObjId, 1011, ct);
            if (env.DialogId == (int)DialogAction.QUEST_ACCEPT_1)
            {
                if (await GiveQuestItemAsync(player, conn, _itemDao, StaffItemId, 1, ct))
                    return await SendQuestStartDialogAsync(env, conn, ct);
                return true;
            }
            return await SendQuestStartDialogAsync(env, conn, ct);
        }

        if (entry.Status == QuestStatus.REWARD)
        {
            if (dialog == DialogAction.USE_OBJECT)
                return await SendQuestDialogAsync(conn, targetObjId, 2375, ct);
            await RemoveQuestItemAsync(player, conn, _itemDao, StaffItemId, 1, ct);
            return await SendQuestEndDialogAsync(env, conn, ct);
        }

        return false;
    }

    public override async ValueTask<bool> OnItemUseAsync(Player player, int itemId, GsClientConnection conn, CancellationToken ct)
    {
        var entry = player.Quests.Get(QuestId);
        if (entry is null || entry.Status != QuestStatus.START || entry.GetVar(0) != 0) return false;

        await ChangeQuestStepAsync(conn, entry, 0, 1, toReward: true, ct);
        return true;
    }
}
