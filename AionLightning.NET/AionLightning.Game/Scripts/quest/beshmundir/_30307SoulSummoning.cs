// Port of Java data/scripts/system/handlers/quest/beshmundir/_30307SoulSummoning.java (Gigi).
// Identical shape to _30207SoulInvocationCeremony at a different NPC/item/threshold: talk to
// 799322 to start, turn in 30x item 182209709 to flip straight to REWARD.
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

namespace Quest.Beshmundir;

public sealed class _30307SoulSummoning : QuestHandlerBase
{
    private const int QuestIdConst = 30307;
    private const int StartNpc     = 799322;
    private const int SoulItemId   = 182209709;
    private const long RequiredCount = 30;

    private readonly IItemDao _itemDao;

    public _30307SoulSummoning(IDataManager dataManager, IQuestDao questDao, QuestRewardService rewardService, IItemDao itemDao)
        : base(QuestIdConst, dataManager, questDao, rewardService)
    {
        _itemDao = itemDao;
    }

    public override void Register(QuestEngine engine)
    {
        engine.RegisterQuestNpc(StartNpc).OnQuestStart.Add(QuestId);
        engine.RegisterQuestNpc(StartNpc).OnTalk.Add(QuestId);
    }

    public override async ValueTask<bool> OnDialogAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var player      = env.Player;
        var entry       = player.Quests.Get(QuestId);
        int targetObjId = env.Target?.ObjectId ?? 0;
        var dialog      = DialogActionLookup.FromId(env.DialogId);

        if (env.TargetId != StartNpc) return false;

        if (entry is null || entry.Status == QuestStatus.NONE)
        {
            if (dialog == DialogAction.QUEST_SELECT) return await SendQuestDialogAsync(conn, targetObjId, 1011, ct);
            return await SendQuestStartDialogAsync(env, conn, ct);
        }

        if (entry.Status == QuestStatus.START)
        {
            if (dialog != DialogAction.QUEST_SELECT) return false;

            var item = player.Inventory.FindByItemId(SoulItemId);
            if (item is not null && item.Count >= RequiredCount)
            {
                await RemoveQuestItemAsync(player, conn, _itemDao, SoulItemId, RequiredCount, ct);
                entry.Status = QuestStatus.REWARD;
                await UpdateQuestStatusAsync(conn, entry, ct);
                return await SendQuestDialogAsync(conn, targetObjId, 2375, ct);
            }
            return await SendQuestDialogAsync(conn, targetObjId, 2716, ct);
        }

        if (entry.Status == QuestStatus.REWARD)
        {
            if (env.DialogId == (int)DialogAction.CHECK_USER_HAS_QUEST_ITEM)
                return await SendQuestDialogAsync(conn, targetObjId, 5, ct);
            return await SendQuestEndDialogAsync(env, conn, ct);
        }

        return false;
    }
}
