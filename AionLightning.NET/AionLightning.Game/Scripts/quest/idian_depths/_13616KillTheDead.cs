// Port of Java data/scripts/system/handlers/quest/idian_depths/_13616KillTheDead.java (pralinka).
// Talk to 801543 to start; kill the Hollow Soldiers (230877/230987), which drop the collect item
// 182213495 (RegisterQuestDrop, quest_data.xml quest_drop, 5 required); handing in at 801543 via
// the collect-item check flips straight to REWARD (var stays 0); turn in at 801543.
// Skip: Java also registers OnEnterZone(South Grand Passage) to bump var 0->1 as a decorative
// tracker step - no other logic reads that var (the collect-item check doesn't gate on it), and
// this engine has no onEnterZone hook, so the bump is omitted (harmless).
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

namespace Quest.IdianDepths;

public sealed class _13616KillTheDead : QuestHandlerBase
{
    private const int QuestIdConst = 13616;
    private const int TurnInNpc    = 801543;
    private const int MobA         = 230877;
    private const int MobB         = 230987;
    private const int CollectItem  = 182213495;

    private readonly IItemDao _itemDao;

    public _13616KillTheDead(IDataManager dataManager, IQuestDao questDao, QuestRewardService rewardService, IItemDao itemDao)
        : base(QuestIdConst, dataManager, questDao, rewardService)
    {
        _itemDao = itemDao;
    }

    public override void Register(QuestEngine engine)
    {
        engine.RegisterQuestNpc(TurnInNpc).OnQuestStart.Add(QuestId);
        engine.RegisterQuestNpc(TurnInNpc).OnTalk.Add(QuestId);
        RegisterQuestDrop(engine, MobA, CollectItem, 1, 100);
        RegisterQuestDrop(engine, MobB, CollectItem, 1, 100);
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
            if (targetId != TurnInNpc) return false;
            if (dialog == DialogAction.QUEST_SELECT)
                return await SendQuestDialogAsync(conn, targetObjId, 4762, ct);
            if (dialog == DialogAction.QUEST_ACCEPT_SIMPLE)
                return await SendQuestStartDialogAsync(env, conn, ct);
            return false;
        }

        if (entry.Status == QuestStatus.START)
        {
            if (targetId == TurnInNpc && dialog == DialogAction.QUEST_SELECT)
                return await CheckQuestItemsAsync(env, conn, _itemDao, 0, 0, reward: true, checkOkId: 5, checkFailId: 1438, ct);
            return false;
        }

        if (entry.Status == QuestStatus.REWARD && targetId == TurnInNpc)
        {
            if (dialog == DialogAction.QUEST_SELECT)
                return await SendQuestDialogAsync(conn, targetObjId, 10001, ct);
            return await SendQuestEndDialogAsync(env, conn, ct);
        }

        return false;
    }
}
