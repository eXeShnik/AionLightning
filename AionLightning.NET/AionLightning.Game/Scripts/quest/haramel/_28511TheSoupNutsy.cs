// Port of Java data/scripts/system/handlers/quest/haramel/_28511TheSoupNutsy.java.
// Asmodian mirror of _18511OutOfThePast: accept from Moorilerk (799522); loot Well-Dried
// Ginseng (700954, stub — no reward-drop system ported), use the Huge Cauldron (730359) to check
// collected quest items and brew the soup (182212023); receiving the soup item auto-advances
// var 0->1 and flips the quest to REWARD (Java's onGetItemEvent hook); turn in at Chagarinerk
// (798031).
using System.Linq;
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

namespace Quest.Haramel;

public sealed class _28511TheSoupNutsy : QuestHandlerBase
{
    private const int QuestIdConst        = 28511;
    private const int MoorilerkNpc        = 799522;
    private const int WellDriedGinsengNpc = 700954;
    private const int HugeCauldronObj     = 730359;
    private const int ChagarinerkNpc      = 798031;
    private const int SoupItemId          = 182212023;

    private readonly IItemDao _itemDao;

    public _28511TheSoupNutsy(IDataManager dataManager, IQuestDao questDao, QuestRewardService rewardService, IItemDao itemDao)
        : base(QuestIdConst, dataManager, questDao, rewardService)
    {
        _itemDao = itemDao;
    }

    public override void Register(QuestEngine engine)
    {
        engine.RegisterQuestNpc(MoorilerkNpc).OnQuestStart.Add(QuestId);
        engine.RegisterItemGet(SoupItemId, QuestId);
        foreach (int npc in new[] { MoorilerkNpc, WellDriedGinsengNpc, HugeCauldronObj, ChagarinerkNpc })
            engine.RegisterQuestNpc(npc).OnTalk.Add(QuestId);
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
            if (targetId == MoorilerkNpc)
            {
                if (dialog == DialogAction.QUEST_SELECT)
                    return await SendQuestDialogAsync(conn, targetObjId, 4762, ct);
                return await SendQuestStartDialogAsync(env, conn, ct);
            }
            return false;
        }

        if (entry.Status == QuestStatus.START)
        {
            if (targetId == WellDriedGinsengNpc)
                return true; // loot (Java: unconditional dialog swallow, no reward-drop system ported)

            if (targetId == HugeCauldronObj)
            {
                int var = entry.GetVar(0);

                // Java switch fallthrough: USE_OBJECT with var != 0 falls into CHECK_USER_HAS_QUEST_ITEM's checkQuestItems.
                if (dialog == DialogAction.USE_OBJECT && var == 0)
                    return await SendQuestDialogAsync(conn, targetObjId, 1011, ct);
                if (dialog == DialogAction.USE_OBJECT || dialog == DialogAction.CHECK_USER_HAS_QUEST_ITEM)
                    return await CheckQuestItemsAsync(env, conn, _itemDao, step: 0, nextStep: 0, reward: false, checkOkId: 1352, checkFailId: 10001, ct);
                if (dialog == DialogAction.SETPRO2)
                {
                    await GiveQuestItemAsync(player, conn, _itemDao, SoupItemId, 1, ct);
                    return await CloseDialogWindowAsync(conn, targetObjId, ct);
                }
                if (dialog == DialogAction.FINISH_DIALOG)
                    return await CloseDialogWindowAsync(conn, targetObjId, ct);
                return false;
            }
            return false;
        }

        if (entry.Status == QuestStatus.REWARD && targetId == ChagarinerkNpc)
        {
            if (dialog == DialogAction.USE_OBJECT)
                return await SendQuestDialogAsync(conn, targetObjId, 10002, ct);
            return await SendQuestEndDialogAsync(env, conn, ct);
        }
        return false;
    }

    public override async ValueTask<bool> OnItemGetAsync(Player player, int itemId, GsClientConnection conn, CancellationToken ct)
    {
        if (itemId != SoupItemId) return false;
        var entry = player.Quests.Get(QuestId);

        // Java: defaultOnGetItemEvent(env, 0, 1, false) then unconditional changeQuestStep(env, 1, 1, true) —
        // both are internally step-guarded (var must equal the "step" argument), so this bumps var 0->1
        // then, now that var==1, flips straight to REWARD.
        if (entry is not null && entry.Status == QuestStatus.START && entry.GetVar(0) == 0)
            await ChangeQuestStepAsync(conn, entry, 0, 1, toReward: false, ct);
        if (entry is not null && entry.GetVar(0) == 1)
            await ChangeQuestStepAsync(conn, entry, 0, 1, toReward: true, ct);
        return true;
    }
}
