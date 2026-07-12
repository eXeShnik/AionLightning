// Port of Java data/scripts/system/handlers/quest/rider_quests/_14015NotBlindedByVengeance.java (pralinka).
// Zone-mission sub-quest of 14010: single-npc collect-item turn-in at 203098 (collect check flips
// straight to REWARD); turn in at the same npc.
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

namespace Quest.RiderQuests;

public sealed class _14015NotBlindedByVengeance : QuestHandlerBase
{
    private const int QuestIdConst = 14015;
    private const int QuestNpc = 203098;

    private readonly IItemDao _itemDao;

    public _14015NotBlindedByVengeance(IDataManager dataManager, IQuestDao questDao, QuestRewardService rewardService, IItemDao itemDao)
        : base(QuestIdConst, dataManager, questDao, rewardService)
    {
        _itemDao = itemDao;
    }

    public override void Register(QuestEngine engine)
    {
        engine.RegisterOnZoneMissionEnd(QuestId);
        engine.RegisterOnLevelUp(QuestId);
        engine.RegisterQuestNpc(QuestNpc).OnTalk.Add(QuestId);
    }

    public override ValueTask<bool> OnZoneMissionEndAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
        => DefaultOnZoneMissionEndEventAsync(env, conn, ct);

    public override ValueTask<bool> OnLevelUpAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
        => DefaultOnLvlUpEventAsync(env, conn, 14010, isZoneMission: true, ct);

    public override async ValueTask<bool> OnDialogAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var entry = env.Player.Quests.Get(QuestId);
        if (entry is null || env.TargetId != QuestNpc) return false;

        var dialog = DialogActionLookup.FromId(env.DialogId);
        int targetObjId = env.Target?.ObjectId ?? 0;

        if (entry.Status == QuestStatus.START)
        {
            if (dialog == DialogAction.QUEST_SELECT)
                return entry.GetVar(0) == 0 && await SendQuestDialogAsync(conn, targetObjId, 1011, ct);
            if (dialog == DialogAction.CHECK_USER_HAS_QUEST_ITEM)
                return await CheckQuestItemsAsync(env, conn, _itemDao, 0, 1, reward: true, checkOkId: 5, checkFailId: 1097, ct);
            return false;
        }

        if (entry.Status == QuestStatus.REWARD)
            return await SendQuestEndDialogAsync(env, conn, ct);

        return false;
    }
}
