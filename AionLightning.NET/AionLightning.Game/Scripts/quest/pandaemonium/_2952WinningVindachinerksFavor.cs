// Port of Java data/scripts/system/handlers/quest/pandaemonium/_2952WinningVindachinerksFavor.java.
// Accept default at Garkbinerk (279006); at Vindachinerk (279016) either hand in gathered
// quest_data.xml collect-items (CHECK_USER_HAS_QUEST_ITEM, or QUEST_SELECT with var != 0 — a dead
// fallthrough in Java's switch, since var never changes elsewhere in this quest, ported via the
// explicit OR below) or click through FINISH_DIALOG (a no-op re-show); turn in at Vindachinerk.
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

namespace Quest.Pandaemonium;

public sealed class _2952WinningVindachinerksFavor : QuestHandlerBase
{
    private const int QuestIdConst = 2952;
    private const int GarkbinerkNpc = 279006;
    private const int VindachinerkNpc = 279016;

    private readonly IItemDao _itemDao;

    public _2952WinningVindachinerksFavor(IDataManager dataManager, IQuestDao questDao, QuestRewardService rewardService, IItemDao itemDao)
        : base(QuestIdConst, dataManager, questDao, rewardService)
    {
        _itemDao = itemDao;
    }

    public override void Register(QuestEngine engine)
    {
        engine.RegisterQuestNpc(GarkbinerkNpc).OnQuestStart.Add(QuestId);
        engine.RegisterQuestNpc(GarkbinerkNpc).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(VindachinerkNpc).OnTalk.Add(QuestId);
    }

    public override async ValueTask<bool> OnDialogAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var entry = env.Player.Quests.Get(QuestId);
        int targetId = env.TargetId;
        int targetObjId = env.Target?.ObjectId ?? 0;
        var dialog = DialogActionLookup.FromId(env.DialogId);

        if (entry is null)
        {
            if (targetId != GarkbinerkNpc) return false;
            if (dialog == DialogAction.QUEST_SELECT)
                return await SendQuestDialogAsync(conn, targetObjId, 1011, ct);
            return await SendQuestStartDialogAsync(env, conn, ct);
        }

        if (entry.Status == QuestStatus.START)
        {
            if (targetId != VindachinerkNpc) return false;
            int var = entry.GetVar(0);

            if (dialog == DialogAction.CHECK_USER_HAS_QUEST_ITEM || (dialog == DialogAction.QUEST_SELECT && var != 0))
                return await CheckQuestItemsAsync(env, conn, _itemDao, 0, 0, reward: true, checkOkId: 5, checkFailId: 2716, ct);
            if (dialog == DialogAction.QUEST_SELECT && var == 0)
                return await SendQuestDialogAsync(conn, targetObjId, 2375, ct);
            if (dialog == DialogAction.FINISH_DIALOG)
                return await DefaultCloseDialogAsync(env, conn, 0, 0, ct);
            return false;
        }

        if (entry.Status == QuestStatus.REWARD && targetId == VindachinerkNpc)
            return await SendQuestEndDialogAsync(env, conn, ct);

        return false;
    }
}
