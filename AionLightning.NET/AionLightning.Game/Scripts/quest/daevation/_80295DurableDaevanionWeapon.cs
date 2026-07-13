// Port of Java data/scripts/system/handlers/quest/daevation/_80295DurableDaevanionWeapon.java (Romanz).
// Start and turn in at 831387: hand over the required collect items (CHECK_USER_HAS_QUEST_ITEM,
// var0 0->1, straight to REWARD, dialog 5) then finish.
// Skip vs Java: the start dialog branched on whether the Daevanion armor set (itemSetPartsEquipped)
// is worn (1003 vs 4762) - no equipment set-parts API ported, so the eligible page (4762) is shown.
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

namespace Quest.Daevation;

public sealed class _80295DurableDaevanionWeapon : QuestHandlerBase
{
    private const int QuestIdConst = 80295;
    private const int Npc          = 831387;

    private readonly IItemDao _itemDao;

    public _80295DurableDaevanionWeapon(IDataManager dataManager, IQuestDao questDao, QuestRewardService rewardService, IItemDao itemDao)
        : base(QuestIdConst, dataManager, questDao, rewardService)
    {
        _itemDao = itemDao;
    }

    public override void Register(QuestEngine engine)
    {
        engine.RegisterQuestNpc(Npc).OnQuestStart.Add(QuestId);
        engine.RegisterQuestNpc(Npc).OnTalk.Add(QuestId);
    }

    public override async ValueTask<bool> OnDialogAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var entry       = env.Player.Quests.Get(QuestId);
        int targetId    = env.TargetId;
        int targetObjId = env.Target?.ObjectId ?? 0;
        var dialog      = DialogActionLookup.FromId(env.DialogId);

        if (targetId != Npc) return false;

        if (entry is null || entry.Status == QuestStatus.NONE)
        {
            if (dialog == DialogAction.QUEST_SELECT)
                // note: Java shows 1003 when the Daevanion armor set is not worn; no set-parts API ported.
                return await SendQuestDialogAsync(conn, targetObjId, 4762, ct);
            return await SendQuestStartDialogAsync(env, conn, ct);
        }

        if (entry.Status == QuestStatus.START)
        {
            int var = entry.GetVar(0);
            if (dialog == DialogAction.QUEST_SELECT && var == 0)
                return await SendQuestDialogAsync(conn, targetObjId, 1011, ct);
            // Java switch fallthrough: QUEST_SELECT falls into CHECK_USER_HAS_QUEST_ITEM
            if (dialog == DialogAction.QUEST_SELECT || dialog == DialogAction.CHECK_USER_HAS_QUEST_ITEM)
            {
                if (var == 0)
                    return await CheckQuestItemsAsync(env, conn, _itemDao, 0, 1, reward: true, 5, 0, ct);
                return false;
            }
            if (dialog == DialogAction.SELECT_ACTION_1352 && var == 0)
                return await SendQuestDialogAsync(conn, targetObjId, 1352, ct);
            return false;
        }

        if (entry.Status == QuestStatus.REWARD)
            return await SendQuestEndDialogAsync(env, conn, ct);

        return false;
    }
}
