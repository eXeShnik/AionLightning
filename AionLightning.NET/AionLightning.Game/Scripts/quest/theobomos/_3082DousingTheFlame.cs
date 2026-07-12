// Port of Java data/scripts/system/handlers/quest/theobomos/_3082DousingTheFlame.java.
// Talk to Kudos (798116) to start; collect-check at the Flame (204030) gives the Water Bucket
// (182208060); use it on the Burning Statue (700416) to spawn the doused statue (700417) and
// finish the step; turn in at Atropos (798155).
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

public sealed class _3082DousingTheFlame : QuestHandlerBase
{
    private const int QuestIdConst   = 3082;
    private const int KudosNpc       = 798116;
    private const int FlameNpc       = 204030;
    private const int BurningStatue  = 700416;
    private const int DousedStatue   = 700417;
    private const int AtroposNpc     = 798155;
    private const int BucketItemId   = 182208060;

    private readonly IItemDao _itemDao;

    public _3082DousingTheFlame(IDataManager dataManager, IQuestDao questDao, QuestRewardService rewardService, IItemDao itemDao)
        : base(QuestIdConst, dataManager, questDao, rewardService)
    {
        _itemDao = itemDao;
    }

    public override void Register(QuestEngine engine)
    {
        engine.RegisterQuestNpc(KudosNpc).OnQuestStart.Add(QuestId);
        engine.RegisterQuestNpc(FlameNpc).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(BurningStatue).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(AtroposNpc).OnTalk.Add(QuestId);
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
            if (targetId != KudosNpc) return false;
            if (dialog == DialogAction.QUEST_SELECT)
                return await SendQuestDialogAsync(conn, targetObjId, 4762, ct);
            return await SendQuestStartDialogAsync(env, conn, ct);
        }

        if (entry.Status == QuestStatus.START)
        {
            if (targetId == FlameNpc)
            {
                if (dialog == DialogAction.QUEST_SELECT)
                {
                    int var = entry.GetVar(0);
                    if (var == 0) return await SendQuestDialogAsync(conn, targetObjId, 1011, ct);
                    if (var == 1) return await SendQuestDialogAsync(conn, targetObjId, 1352, ct);
                    return false;
                }
                if (dialog == DialogAction.CHECK_USER_HAS_QUEST_ITEM)
                    return await CheckQuestItemsAsync(env, conn, _itemDao, 0, 1, reward: false, checkOkId: 10000, checkFailId: 10001, giveItemId: BucketItemId, giveItemCount: 1, ct);
                if (dialog == DialogAction.SETPRO2)
                    return await DefaultCloseDialogAsync(env, conn, 1, 2, ct);
                return false;
            }

            if (targetId == BurningStatue && entry.GetVar(0) == 2)
            {
                var pos = env.Target!.Position;
                SpawnQuestNpc(pos.WorldId, pos.InstanceId, DousedStatue, pos.X, pos.Y, pos.Z, 0);
                await RemoveQuestItemAsync(player, conn, _itemDao, BucketItemId, 1, ct);
                return await UseQuestObjectAsync(env, conn, 2, 2, reward: true, dieObject: false, ct);
            }
            return false;
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
