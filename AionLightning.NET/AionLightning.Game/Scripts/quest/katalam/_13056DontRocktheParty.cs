// Port of Java data/scripts/system/handlers/quest/katalam/_13056DontRocktheParty.java (Evil_dnk).
// Talk to 801094 to start; CHECK_USER_HAS_QUEST_ITEM gates the turn-in on quest_data.xml's
// collect_item list (page 10002 on success, 10001 on failure); SELECT_QUEST_REWARD flips to
// REWARD and shows the end dialog.
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

namespace Quest.Katalam;

public sealed class _13056DontRocktheParty : QuestHandlerBase
{
    private const int QuestIdConst = 13056;
    private const int NpcId        = 801094;

    private readonly IItemDao _itemDao;

    public _13056DontRocktheParty(IDataManager dataManager, IQuestDao questDao, QuestRewardService rewardService, IItemDao itemDao)
        : base(QuestIdConst, dataManager, questDao, rewardService)
    {
        _itemDao = itemDao;
    }

    public override void Register(QuestEngine engine)
    {
        engine.RegisterQuestNpc(NpcId).OnQuestStart.Add(QuestId);
        engine.RegisterQuestNpc(NpcId).OnTalk.Add(QuestId);
    }

    public override async ValueTask<bool> OnDialogAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var entry = env.Player.Quests.Get(QuestId);
        int targetId = env.TargetId;
        int targetObjId = env.Target?.ObjectId ?? 0;
        var dialog = DialogActionLookup.FromId(env.DialogId);

        if (entry is null || entry.Status == QuestStatus.NONE)
        {
            if (targetId != NpcId) return false;
            if (dialog == DialogAction.QUEST_SELECT)
                return await SendQuestDialogAsync(conn, targetObjId, 4762, ct);
            return await SendQuestStartDialogAsync(env, conn, ct);
        }

        if (entry.Status == QuestStatus.START && targetId == NpcId)
        {
            if (dialog == DialogAction.QUEST_SELECT)
                return await SendQuestDialogAsync(conn, targetObjId, 1011, ct);
            if (dialog == DialogAction.CHECK_USER_HAS_QUEST_ITEM)
                return await CheckQuestItemsAsync(env, conn, _itemDao, 0, 0, false, 10002, 10001, ct);
            if (dialog == DialogAction.SELECT_QUEST_REWARD)
            {
                await ChangeQuestStepAsync(conn, entry, 0, 1, toReward: true, ct);
                return await SendQuestEndDialogAsync(env, conn, ct);
            }
            return false;
        }

        if (entry.Status == QuestStatus.REWARD && targetId == NpcId)
            return await SendQuestEndDialogAsync(env, conn, ct);

        return false;
    }
}
