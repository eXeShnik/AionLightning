// Port of Java data/scripts/system/handlers/quest/gelkmaros/_21075FatedHeartbreak.java (Cheatkiller).
// Talk to 799409 to start; relay at 798392 (SETPRO1 gives item 182207917 and goes to 799410,
// var 0->1; SETPRO2 gives the same item and goes to 204138, var 0->2); either 799410 (var 1) or
// 204138 (var 2) removes the item and flips to REWARD; turn in at 799409.
// Java bug fixed: the original stored the branch-taken reward tier in an instance field
// (`rewardIndex`) — since quest handlers are shared singletons across every player, that field
// would leak between concurrent players on the same quest. This port derives the reward index
// from the persisted quest var (0 for the 799410 branch, 1 for the 204138 branch) at turn-in time.
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

namespace Quest.Gelkmaros;

public sealed class _21075FatedHeartbreak : QuestHandlerBase
{
    private const int QuestIdConst = 21075;
    private const int StartNpc     = 799409;
    private const int BranchNpc    = 798392;
    private const int PathANpc     = 799410;
    private const int PathBNpc     = 204138;
    private const int ItemId       = 182207917;

    private readonly IItemDao _itemDao;

    public _21075FatedHeartbreak(IDataManager dataManager, IQuestDao questDao, QuestRewardService rewardService, IItemDao itemDao)
        : base(QuestIdConst, dataManager, questDao, rewardService)
    {
        _itemDao = itemDao;
    }

    public override void Register(QuestEngine engine)
    {
        engine.RegisterQuestNpc(StartNpc).OnQuestStart.Add(QuestId);
        engine.RegisterQuestNpc(StartNpc).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(BranchNpc).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(PathANpc).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(PathBNpc).OnTalk.Add(QuestId);
    }

    public override async ValueTask<bool> OnDialogAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var player = env.Player;
        var entry  = player.Quests.Get(QuestId);
        int targetId = env.TargetId;
        int targetObjId = env.Target?.ObjectId ?? 0;
        var dialog = DialogActionLookup.FromId(env.DialogId);

        if (entry is null)
        {
            if (targetId == StartNpc)
            {
                if (dialog == DialogAction.QUEST_SELECT)
                    return await SendQuestDialogAsync(conn, targetObjId, 4762, ct);
                return await SendQuestStartDialogAsync(env, conn, ct);
            }
            return false;
        }

        if (entry.Status == QuestStatus.START)
        {
            if (targetId == BranchNpc)
            {
                if (dialog == DialogAction.QUEST_SELECT)
                    return await SendQuestDialogAsync(conn, targetObjId, 1011, ct);
                if (dialog == DialogAction.SETPRO1)
                {
                    await GiveQuestItemAsync(player, conn, _itemDao, ItemId, 1, ct);
                    return await DefaultCloseDialogAsync(env, conn, 0, 1, ct);
                }
                if (dialog == DialogAction.SETPRO2)
                {
                    await GiveQuestItemAsync(player, conn, _itemDao, ItemId, 1, ct);
                    return await DefaultCloseDialogAsync(env, conn, 0, 2, ct);
                }
                return false;
            }

            if (targetId == PathANpc)
            {
                if (dialog == DialogAction.QUEST_SELECT && entry.GetVar(0) == 1)
                    return await SendQuestDialogAsync(conn, targetObjId, 1352, ct);
                if (dialog == DialogAction.SET_SUCCEED)
                {
                    await RemoveQuestItemAsync(player, conn, _itemDao, ItemId, 1, ct);
                    return await DefaultCloseDialogAsync(env, conn, 1, 1, reward: true, sameNpc: false, ct);
                }
                return false;
            }

            if (targetId == PathBNpc)
            {
                if (dialog == DialogAction.QUEST_SELECT && entry.GetVar(0) == 2)
                    return await SendQuestDialogAsync(conn, targetObjId, 1693, ct);
                if (dialog == DialogAction.SET_SUCCEED)
                {
                    await RemoveQuestItemAsync(player, conn, _itemDao, ItemId, 1, ct);
                    return await DefaultCloseDialogAsync(env, conn, 2, 2, reward: true, sameNpc: false, ct);
                }
                return false;
            }
        }

        if (entry.Status == QuestStatus.REWARD && targetId == StartNpc)
        {
            if (dialog == DialogAction.USE_OBJECT)
                return await SendQuestDialogAsync(conn, targetObjId, 10002, ct);

            int rewardIndex = entry.GetVar(0) == 2 ? 1 : 0;
            if (dialog == DialogAction.SELECT_QUEST_REWARD)
                return await SendQuestDialogAsync(conn, targetObjId, 5 + rewardIndex, ct);
            if (env.DialogId == (int)DialogAction.SELECTED_QUEST_NOREWARD)
                return await FinishQuestAsync(conn, player, rewardIndex, ct);
            return false;
        }

        return false;
    }
}
