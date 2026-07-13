// Port of Java data/scripts/system/handlers/quest/beluslan/_4502EssenceOfFate.java (vlog).
// Start at Hresvelgr (204837). Use the Balaur Operation Orders (730192, var0 0->1). In Dark Poeta
// kill the Telepathy Controller (214894, var0 1->2), then the three power generators — Main (214895,
// var1=1), Auxiliary (214896, var2=1), Emergency (214897, var3=1). Brigade General Anuhart (214904)
// is registered for the kill event but, exactly as in Java, has no handler branch (it only drops the
// Concentrated Vitality collect item). Turn in at Heimdall (204182) via the collect-item check.
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

namespace Quest.Beluslan;

public sealed class _4502EssenceOfFate : QuestHandlerBase
{
    private const int QuestIdConst = 4502;
    private const int Hresvelgr    = 204837;
    private const int OperationOrders = 730192;
    private const int Heimdall     = 204182;
    private const int TelepathyController = 214894;
    private const int MainGenerator = 214895;
    private const int AuxGenerator  = 214896;
    private const int EmergencyGenerator = 214897;
    private const int Anuhart       = 214904;

    private readonly IItemDao _itemDao;

    public _4502EssenceOfFate(IDataManager dataManager, IQuestDao questDao, QuestRewardService rewardService, IItemDao itemDao)
        : base(QuestIdConst, dataManager, questDao, rewardService)
    {
        _itemDao = itemDao;
    }

    public override void Register(QuestEngine engine)
    {
        engine.RegisterQuestNpc(Hresvelgr).OnQuestStart.Add(QuestId);
        engine.RegisterQuestNpc(Hresvelgr).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(OperationOrders).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(Heimdall).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(TelepathyController).OnKill.Add(QuestId);
        engine.RegisterQuestNpc(MainGenerator).OnKill.Add(QuestId);
        engine.RegisterQuestNpc(AuxGenerator).OnKill.Add(QuestId);
        engine.RegisterQuestNpc(EmergencyGenerator).OnKill.Add(QuestId);
        engine.RegisterQuestNpc(Anuhart).OnKill.Add(QuestId);
    }

    public override async ValueTask<bool> OnDialogAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var entry       = env.Player.Quests.Get(QuestId);
        int targetId    = env.TargetId;
        int targetObjId = env.Target?.ObjectId ?? 0;
        var dialog      = DialogActionLookup.FromId(env.DialogId);

        if (entry is null || entry.Status == QuestStatus.NONE)
        {
            if (targetId == Hresvelgr)
            {
                if (dialog == DialogAction.QUEST_SELECT)
                    return await SendQuestDialogAsync(conn, targetObjId, 4762, ct);
                return await SendQuestStartDialogAsync(env, conn, ct);
            }
            return false;
        }

        if (entry.Status == QuestStatus.START)
        {
            int var = entry.GetVar(0);
            if (targetId == OperationOrders)
            {
                if (var == 0)
                {
                    if (dialog == DialogAction.USE_OBJECT)
                        return await SendQuestDialogAsync(conn, targetObjId, 1011, ct);
                    await ChangeQuestStepAsync(conn, entry, 0, 1, toReward: false, ct);
                    return await SendQuestDialogAsync(conn, targetObjId, 0, ct);
                }
                return false;
            }
            if (targetId == Heimdall)
            {
                if (dialog == DialogAction.QUEST_SELECT && var == 2)
                    return await SendQuestDialogAsync(conn, targetObjId, 1352, ct);
                // Java switch fallthrough: QUEST_SELECT (var != 2) drops into CHECK_USER_HAS_QUEST_ITEM
                if (dialog == DialogAction.QUEST_SELECT || dialog == DialogAction.CHECK_USER_HAS_QUEST_ITEM)
                    return await CheckQuestItemsAsync(env, conn, _itemDao, 2, 2, reward: true, 5, 10001, ct);
                if (dialog == DialogAction.FINISH_DIALOG)
                    return await SendQuestSelectionDialogAsync(conn, targetObjId, ct);
                return false;
            }
            return false;
        }

        if (entry.Status == QuestStatus.REWARD)
        {
            if (targetId == Heimdall)
                return await SendQuestEndDialogAsync(env, conn, ct);
        }
        return false;
    }

    public override async ValueTask<bool> OnKillAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var entry = env.Player.Quests.Get(QuestId);
        if (entry is null || entry.Status != QuestStatus.START) return false;

        int var  = entry.GetVar(0);
        int var1 = entry.GetVar(1);
        int var2 = entry.GetVar(2);
        int var3 = entry.GetVar(3);
        int targetId = env.TargetId;

        switch (targetId)
        {
            case TelepathyController:
                if (var == 1)
                    return await DefaultOnKillEventAsync(env, conn, TelepathyController, 1, 2, ct);
                return false;
            case MainGenerator:
                if (var == 2 && var1 != 1)
                {
                    await ChangeQuestStepAsync(conn, entry, 1, 1, toReward: false, ct);
                    return true;
                }
                return false;
            case AuxGenerator:
                if (var == 2 && var2 != 1)
                {
                    await ChangeQuestStepAsync(conn, entry, 2, 1, toReward: false, ct);
                    return true;
                }
                return false;
            case EmergencyGenerator:
                if (var == 2 && var3 != 1)
                {
                    await ChangeQuestStepAsync(conn, entry, 3, 1, toReward: false, ct);
                    return true;
                }
                return false;
        }
        return false;
    }
}
