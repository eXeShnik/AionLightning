// Port of Java data/scripts/system/handlers/quest/gelkmaros_armor/_21051TroubleinStone.java (zhkchi, reworked vlog).
// Single-npc collect-turn-in armor quest at Aquila (799291): accept, hand in the quest_data.xml
// collect_items (CHECK_USER_HAS_QUEST_ITEM -> checkQuestItems, ok page 5 / fail page 2716), turn in.
// note: Java's qs.canRepeat() daily-repeat re-entry is approximated as "no active entry", matching
// the documented canRepeat gap precedent (e.g. theobomos/_3074DangerousProbability).
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

namespace Quest.GelkmarosArmor;

public sealed class _21051TroubleinStone : QuestHandlerBase
{
    private const int QuestIdConst = 21051;
    private const int AquilaNpc    = 799291;

    private readonly IItemDao _itemDao;

    public _21051TroubleinStone(IDataManager dataManager, IQuestDao questDao, QuestRewardService rewardService, IItemDao itemDao)
        : base(QuestIdConst, dataManager, questDao, rewardService)
    {
        _itemDao = itemDao;
    }

    public override void Register(QuestEngine engine)
    {
        engine.RegisterQuestNpc(AquilaNpc).OnQuestStart.Add(QuestId);
        engine.RegisterQuestNpc(AquilaNpc).OnTalk.Add(QuestId);
    }

    public override async ValueTask<bool> OnDialogAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var player = env.Player;
        var entry  = player.Quests.Get(QuestId);
        int targetId    = env.TargetId;
        int targetObjId = env.Target?.ObjectId ?? 0;
        var dialog      = DialogActionLookup.FromId(env.DialogId);

        if (entry is null || entry.Status == QuestStatus.NONE)
        {
            if (targetId == AquilaNpc)
            {
                if (dialog == DialogAction.QUEST_SELECT)
                    return await SendQuestDialogAsync(conn, targetObjId, 1011, ct);
                return await SendQuestStartDialogAsync(env, conn, ct);
            }
            return false;
        }

        if (entry.Status == QuestStatus.START)
        {
            if (targetId == AquilaNpc)
            {
                if (dialog == DialogAction.QUEST_SELECT)
                    return await SendQuestDialogAsync(conn, targetObjId, 2375, ct);
                if (dialog == DialogAction.CHECK_USER_HAS_QUEST_ITEM)
                    return await CheckQuestItemsAsync(env, conn, _itemDao, 0, 0, true, 5, 2716, ct);
                if (dialog == DialogAction.FINISH_DIALOG)
                    return await SendQuestSelectionDialogAsync(conn, targetObjId, ct);
            }
            return false;
        }

        if (entry.Status == QuestStatus.REWARD && targetId == AquilaNpc)
            return await SendQuestEndDialogAsync(env, conn, ct);

        return false;
    }
}
