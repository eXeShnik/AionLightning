// Port of Java data/scripts/system/handlers/quest/theobomos/_3036LetSeeWhatItDoes.java.
// Talk to Atropos (798155) to start (gives 182208026); use the object at 700398 to remove the
// item and flip to REWARD; return to Atropos to finish.
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

namespace Quest.Theobomos;

public sealed class _3036LetSeeWhatItDoes : QuestHandlerBase
{
    private const int QuestIdConst = 3036;
    private const int AtroposNpc   = 798155;
    private const int ObjectNpc    = 700398;
    private const int GiftItemId   = 182208026;

    private readonly IItemDao _itemDao;

    public _3036LetSeeWhatItDoes(IDataManager dataManager, IQuestDao questDao, QuestRewardService rewardService, IItemDao itemDao)
        : base(QuestIdConst, dataManager, questDao, rewardService)
    {
        _itemDao = itemDao;
    }

    public override void Register(QuestEngine engine)
    {
        engine.RegisterQuestNpc(AtroposNpc).OnQuestStart.Add(QuestId);
        engine.RegisterQuestNpc(AtroposNpc).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(ObjectNpc).OnTalk.Add(QuestId);
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
            if (targetId != AtroposNpc) return false;
            if (dialog == DialogAction.QUEST_SELECT)
                return await SendQuestDialogAsync(conn, targetObjId, 4762, ct);
            if (dialog == DialogAction.QUEST_ACCEPT_1)
            {
                if (await GiveQuestItemAsync(player, conn, _itemDao, GiftItemId, 1, ct))
                    return await SendQuestStartDialogAsync(env, conn, ct);
                return false;
            }
            return await SendQuestStartDialogAsync(env, conn, ct);
        }

        if (entry.Status == QuestStatus.START && targetId == ObjectNpc)
        {
            await RemoveQuestItemAsync(player, conn, _itemDao, GiftItemId, 1, ct);
            return await UseQuestObjectAsync(env, conn, 0, 1, reward: true, dieObject: false, ct);
        }

        if (entry.Status == QuestStatus.REWARD && targetId == AtroposNpc)
        {
            if (dialog == DialogAction.USE_OBJECT)
                return await SendQuestDialogAsync(conn, targetObjId, 10002, ct);
            return await SendQuestEndDialogAsync(env, conn, ct);
        }

        return false;
    }
}
