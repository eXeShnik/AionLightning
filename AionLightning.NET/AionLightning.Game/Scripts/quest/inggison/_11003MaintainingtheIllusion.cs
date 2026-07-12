// Port of Java data/scripts/system/handlers/quest/inggison/_11003MaintainingtheIllusion.java.
// Talk to 798933 to start (no item); turning in at Harknes (798942) requires >=12 of 182206701 and
// >=10 of 182206702 (Java's strict > 11 / > 9 checks) -- removes both and flips to reward, else
// shows the "not enough" page. REWARD-status CHECK_USER_HAS_QUEST_ITEM dialog id shows page 5
// before the normal turn-in dialog.
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

namespace Quest.Inggison;

public sealed class _11003MaintainingtheIllusion : QuestHandlerBase
{
    private const int QuestIdConst = 11003;
    private const int StartNpc     = 798933;
    private const int TurnInNpc    = 798942;
    private const int Item1        = 182206701;
    private const int Item2        = 182206702;

    private readonly IItemDao _itemDao;

    public _11003MaintainingtheIllusion(IDataManager dataManager, IQuestDao questDao, QuestRewardService rewardService, IItemDao itemDao)
        : base(QuestIdConst, dataManager, questDao, rewardService)
    {
        _itemDao = itemDao;
    }

    public override void Register(QuestEngine engine)
    {
        engine.RegisterQuestNpc(StartNpc).OnQuestStart.Add(QuestId);
        engine.RegisterQuestNpc(TurnInNpc).OnTalk.Add(QuestId);
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
            if (targetId == StartNpc)
            {
                if (dialog == DialogAction.QUEST_SELECT)
                    return await SendQuestDialogAsync(conn, targetObjId, 1011, ct);
                return await SendQuestStartDialogAsync(env, conn, ct);
            }
            return false;
        }

        if (entry.Status == QuestStatus.START && targetId == TurnInNpc)
        {
            if (dialog == DialogAction.QUEST_SELECT)
            {
                long count1 = player.Inventory.FindByItemId(Item1)?.Count ?? 0;
                long count2 = player.Inventory.FindByItemId(Item2)?.Count ?? 0;
                if (count1 > 11 && count2 > 9)
                {
                    await RemoveQuestItemAsync(player, conn, _itemDao, Item1, 12, ct);
                    await RemoveQuestItemAsync(player, conn, _itemDao, Item2, 10, ct);
                    entry.Status = QuestStatus.REWARD;
                    await UpdateQuestStatusAsync(conn, entry, ct);
                    return await SendQuestDialogAsync(conn, targetObjId, 2375, ct);
                }
                return await SendQuestDialogAsync(conn, targetObjId, 2716, ct);
            }
            return false;
        }

        if (entry.Status == QuestStatus.REWARD && targetId == TurnInNpc)
        {
            if (dialog == DialogAction.CHECK_USER_HAS_QUEST_ITEM)
                return await SendQuestDialogAsync(conn, targetObjId, 5, ct);
            return await SendQuestEndDialogAsync(env, conn, ct);
        }
        return false;
    }
}
