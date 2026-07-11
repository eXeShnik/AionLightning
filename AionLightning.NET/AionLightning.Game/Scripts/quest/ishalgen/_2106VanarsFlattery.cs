// Port of Java data/scripts/system/handlers/quest/ishalgen/_2106VanarsFlattery.java.
// Vanar (203502) start (gives 182203106); accepting sends the player to the turn-in area, then
// turn in at NehumON (203517).
// Skips vs Java (documented): the TeleportService2 hop on SETPRO1 (no teleport service — the
// player walks instead; quest still completes) and canRepeat() (no repeat modeling yet —
// treated as a one-time quest).
using System.Linq;
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

namespace Quest.Ishalgen;

public sealed class _2106VanarsFlattery : QuestHandlerBase
{
    private const int QuestIdConst = 2106;
    private const int VanarNpc     = 203502;
    private const int NeumonNpc    = 203517;
    private const int GiftItemId   = 182203106;

    private readonly IItemDao _itemDao;

    public _2106VanarsFlattery(IDataManager dataManager, IQuestDao questDao, QuestRewardService rewardService, IItemDao itemDao)
        : base(QuestIdConst, dataManager, questDao, rewardService)
    {
        _itemDao = itemDao;
    }

    public override void Register(QuestEngine engine)
    {
        engine.RegisterQuestNpc(VanarNpc).OnQuestStart.Add(QuestId);
        engine.RegisterQuestNpc(VanarNpc).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(NeumonNpc).OnTalk.Add(QuestId);
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
            if (targetId == VanarNpc)
            {
                switch (dialog)
                {
                    case DialogAction.QUEST_SELECT:
                        return await SendQuestDialogAsync(conn, targetObjId, 4762, ct);
                    case DialogAction.ASK_QUEST_ACCEPT:
                        return await SendQuestDialogAsync(conn, targetObjId, 4, ct);
                    case DialogAction.QUEST_ACCEPT_1:
                        if (await GiveQuestItemAsync(player, conn, _itemDao, GiftItemId, 1, ct))
                            return await SendQuestStartDialogAsync(env, conn, ct);
                        return false;
                    case DialogAction.QUEST_REFUSE_1:
                        return await SendQuestDialogAsync(conn, targetObjId, 1004, ct);
                    case DialogAction.FINISH_DIALOG:
                        return await CloseDialogWindowAsync(conn, targetObjId, ct);
                    default:
                        return false;
                }
            }
            return false;
        }

        if (entry.Status == QuestStatus.START && targetId == VanarNpc)
        {
            switch (dialog)
            {
                case DialogAction.QUEST_SELECT:
                    return await SendQuestDialogAsync(conn, targetObjId, 1003, ct);
                case DialogAction.SETPRO1:
                    // Java teleports to (576,2538,272) in world 220010000 here — skipped (no service).
                    await ChangeQuestStepAsync(conn, entry, 0, 1, toReward: true, ct);
                    return await CloseDialogWindowAsync(conn, targetObjId, ct);
                default:
                    return false;
            }
        }

        if (entry.Status == QuestStatus.REWARD && targetId == NeumonNpc)
            return await SendQuestEndDialogAsync(env, conn, ct);

        return false;
    }
}
