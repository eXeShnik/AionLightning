// Port of Java data/scripts/system/handlers/quest/empyrean_crucible/_28212ATestOfBlood.java (Cheatkiller).
// Asmodian mirror of 18212 FirstBlood. Started at 205986; kill any opposing player inside either
// coliseum arena instance world (300350000, 300360000) to get quest item 182212221 and flip straight
// to REWARD (Java defaultOnKillRankedEvent(env, 0, 1, true) with an always-true startVar==0 gate, no
// rank check).
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

namespace Quest.EmpyreanCrucible;

public sealed class _28212ATestOfBlood : QuestHandlerBase
{
    private const int QuestIdConst = 28212;
    private const int StartNpc     = 205986;
    private const int ItemId       = 182212221;
    private const int ArenaWorld1  = 300350000;
    private const int ArenaWorld2  = 300360000;

    private readonly IItemDao _itemDao;

    public _28212ATestOfBlood(IDataManager dataManager, IQuestDao questDao, QuestRewardService rewardService, IItemDao itemDao)
        : base(QuestIdConst, dataManager, questDao, rewardService)
    {
        _itemDao = itemDao;
    }

    public override void Register(QuestEngine engine)
    {
        engine.RegisterQuestNpc(StartNpc).OnQuestStart.Add(QuestId);
        engine.RegisterQuestNpc(StartNpc).OnTalk.Add(QuestId);
        engine.RegisterKillInWorld(ArenaWorld1, QuestId);
        engine.RegisterKillInWorld(ArenaWorld2, QuestId);
    }

    public override async ValueTask<bool> OnPlayerKillAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var player = env.Player;
        var entry  = player.Quests.Get(QuestId);
        if (entry is null || entry.Status != QuestStatus.START) return false;

        await GiveQuestItemAsync(player, conn, _itemDao, ItemId, 1, ct);

        // Java defaultOnKillRankedEvent(env, 0, 1, true): var range [0, 0) is empty, so only the
        // var == endVar-1 (0) branch ever fires - a single qualifying kill flips straight to REWARD.
        if (entry.GetVar(0) == 0)
        {
            await ChangeQuestStepAsync(conn, entry, -1, 0, toReward: true, ct);
            return true;
        }
        return false;
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
            if (dialog == DialogAction.QUEST_SELECT)
                return await SendQuestDialogAsync(conn, targetObjId, 4762, ct);
            return await SendQuestStartDialogAsync(env, conn, ct);
        }
        if (entry.Status == QuestStatus.REWARD)
        {
            if (dialog == DialogAction.USE_OBJECT)
                return await SendQuestDialogAsync(conn, targetObjId, 10002, ct);
            await RemoveQuestItemAsync(player, conn, _itemDao, ItemId, 1, ct);
            return await SendQuestEndDialogAsync(env, conn, ct);
        }
        return false;
    }
}
