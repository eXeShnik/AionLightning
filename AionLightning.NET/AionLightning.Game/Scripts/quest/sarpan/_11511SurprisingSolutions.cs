// Port of Java data/scripts/system/handlers/quest/sarpan/_11511SurprisingSolutions.java (zhkchi).
// Talk to Killios's Aide (205989) to start; hand in to Beshmundir (205746) at var 0->1; return to
// 205989, which checks the quest_data.xml collect-items (simple variant - no fail dialog, just
// closes the window) to flip to REWARD; turn in at 205989.
// Skip vs Java: qs.canRepeat() (repeatable-quest gate) is approximated as "no active entry", same
// simplification as every other repeatable quest already in this codebase (no repeat-cooldown infra
// ported). Also keeps Java's onGetItemEvent dead code as-is for 1:1 fidelity: `qs.setQuestVarById(1,
// var)` reassigns var to itself instead of incrementing it, so `var == 15` can never be reached -
// completion only ever happens via the CHECK_USER_HAS_QUEST_ITEM_SIMPLE dialog branch below.
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

namespace Quest.Sarpan;

public sealed class _11511SurprisingSolutions : QuestHandlerBase
{
    private const int QuestIdConst   = 11511;
    private const int AideNpc        = 205989;
    private const int BeshmundirNpc  = 205746;
    private const int TrackedItemId  = 182213115;

    private readonly IItemDao _itemDao;

    public _11511SurprisingSolutions(IDataManager dataManager, IQuestDao questDao, QuestRewardService rewardService, IItemDao itemDao)
        : base(QuestIdConst, dataManager, questDao, rewardService)
    {
        _itemDao = itemDao;
    }

    public override void Register(QuestEngine engine)
    {
        engine.RegisterQuestNpc(AideNpc).OnQuestStart.Add(QuestId);
        engine.RegisterQuestNpc(AideNpc).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(BeshmundirNpc).OnTalk.Add(QuestId);
        engine.RegisterItemGet(TrackedItemId, QuestId);
    }

    public override async ValueTask<bool> OnDialogAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var player      = env.Player;
        var entry       = player.Quests.Get(QuestId);
        int targetId    = env.TargetId;
        int targetObjId = env.Target?.ObjectId ?? 0;
        var dialog      = DialogActionLookup.FromId(env.DialogId);

        if (entry is null || entry.Status == QuestStatus.NONE)
        {
            if (targetId != AideNpc) return false;
            if (dialog == DialogAction.QUEST_SELECT) return await SendQuestDialogAsync(conn, targetObjId, 1011, ct);
            return await SendQuestStartDialogAsync(env, conn, ct);
        }

        if (entry.Status == QuestStatus.START)
        {
            if (targetId == BeshmundirNpc)
            {
                if (dialog == DialogAction.QUEST_SELECT) return await SendQuestDialogAsync(conn, targetObjId, 1352, ct);
                if (dialog == DialogAction.SETPRO1) return await DefaultCloseDialogAsync(env, conn, 0, 1, ct);
                return false;
            }
            if (targetId == AideNpc)
            {
                if (dialog == DialogAction.QUEST_SELECT) return await SendQuestDialogAsync(conn, targetObjId, 2375, ct);
                if (dialog == DialogAction.CHECK_USER_HAS_QUEST_ITEM_SIMPLE)
                    return await CheckQuestItemsAsync(env, conn, _itemDao, 1, 1, reward: true, checkOkId: 5, checkFailId: 0, ct);
                return false;
            }
            return false;
        }

        if (entry.Status == QuestStatus.REWARD && targetId == AideNpc)
            return await SendQuestEndDialogAsync(env, conn, ct);

        return false;
    }

    public override ValueTask<bool> OnItemGetAsync(Player player, int itemId, GsClientConnection conn, CancellationToken ct)
    {
        var entry = player.Quests.Get(QuestId);
        if (entry is null || entry.Status != QuestStatus.START) return ValueTask.FromResult(false);

        // Java bug: `qs.setQuestVarById(1, var)` reassigns var to itself instead of incrementing -
        // var 1 can never reach 15, so this branch is unreachable dead code; kept 1:1.
        return ValueTask.FromResult(false);
    }
}
