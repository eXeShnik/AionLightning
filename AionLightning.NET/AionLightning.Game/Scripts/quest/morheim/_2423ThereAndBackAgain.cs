// Port of Java data/scripts/system/handlers/quest/morheim/_2423ThereAndBackAgain.java.
// Start at 204326; at 204375 a collect-item check (var 1->2) gates progress, then SETPRO3 (var->3,
// reward) and SETPRO1 (var 0->1) each trigger a TeleportService2 hop to Verteron.
// Skip vs Java: no TeleportService exists in this port, so the automatic teleport is omitted —
// the quest var/status transition still happens normally; the player simply has to walk instead
// of being ferried there.
using System.Threading;
using System.Threading.Tasks;
using AionLightning.Game.Dao;
using AionLightning.Game.DataHolders;
using AionLightning.Game.Model.Quest;
using AionLightning.Game.Network.Aion;
using AionLightning.Game.Network.Aion.ServerPackets;
using AionLightning.Game.QuestEngine;
using AionLightning.Game.QuestEngine.Handlers;
using AionLightning.Game.QuestEngine.Model;
using AionLightning.Game.Services;

namespace Quest.Morheim;

public sealed class _2423ThereAndBackAgain : QuestHandlerBase
{
    private const int QuestIdConst = 2423;
    private const int StartNpc     = 204326;
    private const int TravelerNpc  = 204375;

    private readonly IItemDao _itemDao;

    public _2423ThereAndBackAgain(IDataManager dataManager, IQuestDao questDao, QuestRewardService rewardService, IItemDao itemDao)
        : base(QuestIdConst, dataManager, questDao, rewardService)
    {
        _itemDao = itemDao;
    }

    public override void Register(QuestEngine engine)
    {
        engine.RegisterQuestNpc(StartNpc).OnQuestStart.Add(QuestId);
        engine.RegisterQuestNpc(TravelerNpc).OnTalk.Add(QuestId);
    }

    public override async ValueTask<bool> OnDialogAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var entry = env.Player.Quests.Get(QuestId);
        int targetId = env.TargetId;
        int targetObjId = env.Target?.ObjectId ?? 0;
        var dialog = DialogActionLookup.FromId(env.DialogId);

        if (entry is null)
        {
            if (targetId != StartNpc) return false;
            if (dialog == DialogAction.QUEST_SELECT)
                return await SendQuestDialogAsync(conn, targetObjId, 4762, ct);
            return await SendQuestStartDialogAsync(env, conn, ct);
        }

        if (entry.Status == QuestStatus.START && targetId == TravelerNpc)
        {
            switch (dialog)
            {
                case DialogAction.QUEST_SELECT:
                    return entry.GetVar(0) switch
                    {
                        0 => await SendQuestDialogAsync(conn, targetObjId, 1011, ct),
                        1 => await SendQuestDialogAsync(conn, targetObjId, 1352, ct),
                        2 => await SendQuestDialogAsync(conn, targetObjId, 1693, ct),
                        _ => false,
                    };
                case DialogAction.CHECK_USER_HAS_QUEST_ITEM:
                    return await CheckQuestItemsAsync(env, conn, _itemDao, 1, 2, false, 10000, 10001, ct);
                case DialogAction.SETPRO3:
                    entry.SetVar(0, 3);
                    return await DefaultCloseDialogAsync(env, conn, 3, 3, reward: true, sameNpc: false, ct);
                case DialogAction.SELECT_ACTION_1779:
                    return await SendQuestDialogAsync(conn, targetObjId, 1779, ct);
                case DialogAction.SETPRO1:
                    return await DefaultCloseDialogAsync(env, conn, 0, 1, ct);
                default:
                    return false;
            }
        }

        if (entry.Status == QuestStatus.REWARD && targetId == TravelerNpc)
        {
            if (dialog == DialogAction.USE_OBJECT)
                return await SendQuestDialogAsync(conn, targetObjId, 10002, ct);
            return await SendQuestEndDialogAsync(env, conn, ct);
        }
        return false;
    }
}
