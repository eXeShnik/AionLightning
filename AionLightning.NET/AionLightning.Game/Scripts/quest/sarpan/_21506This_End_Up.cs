// Port of Java data/scripts/system/handlers/quest/sarpan/_21506This_End_Up.java (Cheatkiller).
// Talk to the delivery box (205711) to start - accepting starts a 10-minute quest timer and gives
// item 182213103; hand it in at 205715 to flip straight to REWARD; turn in at the same NPC.
// If the timer expires first, the quest resets to NONE (Java QUEST_FAILED system message skipped -
// see below).
// Skip vs Java: the timer-expiry branch sends SystemMessageId.QUEST_FAILED_$1 with the quest name
// as a parameter - SM_SYSTEM_MESSAGE only exposes fixed factory methods in this port (its
// constructor is private) and none covers a generic code+string-param message, so the state reset
// happens silently instead of showing the failure toast. Also skips Java's explicit
// QuestService.questTimerEnd(env) call on turn-in (no timer-cancellation API is exposed) - harmless
// since the timer handler already no-ops once status has left START.
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

namespace Quest.Sarpan;

public sealed class _21506This_End_Up : QuestHandlerBase
{
    private const int QuestIdConst = 21506;
    private const int DeliveryBoxNpc = 205711;
    private const int TurnInNpc      = 205715;
    private const int PackageItemId  = 182213103;
    private const int TimerSeconds   = 600;

    private readonly IItemDao _itemDao;

    public _21506This_End_Up(IDataManager dataManager, IQuestDao questDao, QuestRewardService rewardService, IItemDao itemDao)
        : base(QuestIdConst, dataManager, questDao, rewardService)
    {
        _itemDao = itemDao;
    }

    public override void Register(QuestEngine engine)
    {
        engine.RegisterQuestNpc(DeliveryBoxNpc).OnQuestStart.Add(QuestId);
        engine.RegisterQuestNpc(TurnInNpc).OnTalk.Add(QuestId);
        engine.RegisterOnQuestTimerEnd(QuestId);
    }

    public override async ValueTask<bool> OnDialogAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var player      = env.Player;
        var entry       = player.Quests.Get(QuestId);
        int targetId    = env.TargetId;
        int targetObjId = env.Target?.ObjectId ?? 0;
        var dialog      = DialogActionLookup.FromId(env.DialogId);

        if (entry is null || entry.Status == QuestStatus.NONE)
        {
            if (targetId != DeliveryBoxNpc) return false;
            if (dialog == DialogAction.QUEST_SELECT) return await SendQuestDialogAsync(conn, targetObjId, 1011, ct);
            if (dialog == DialogAction.QUEST_ACCEPT_SIMPLE)
            {
                StartQuestTimer(env, conn, TimerSeconds);
                await StartMissionAsync(conn, player, QuestStatus.START, ct);
                await GiveQuestItemAsync(player, conn, _itemDao, PackageItemId, 1, ct);
                return await CloseDialogWindowAsync(conn, targetObjId, ct);
            }
            return await SendQuestStartDialogAsync(env, conn, ct);
        }

        if (entry.Status == QuestStatus.START && targetId == TurnInNpc)
        {
            if (dialog == DialogAction.QUEST_SELECT && entry.GetVar(0) == 0) return await SendQuestDialogAsync(conn, targetObjId, 2375, ct);
            if (dialog == DialogAction.SELECT_QUEST_REWARD)
            {
                await RemoveQuestItemAsync(player, conn, _itemDao, PackageItemId, 1, ct);
                return await DefaultCloseDialogAsync(env, conn, 0, 1, reward: true, sameNpc: true, ct);
            }
            return false;
        }

        if (entry.Status == QuestStatus.REWARD && targetId == TurnInNpc)
        {
            if (dialog == DialogAction.USE_OBJECT) return await SendQuestDialogAsync(conn, targetObjId, 5, ct);
            return await SendQuestEndDialogAsync(env, conn, ct);
        }

        return false;
    }

    public override async ValueTask<bool> OnQuestTimerEndAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var entry = env.Player.Quests.Get(QuestId);
        if (entry is null || entry.Status != QuestStatus.START) return false;

        entry.Status = QuestStatus.NONE;
        entry.SetVar(0, 0);
        await UpdateQuestStatusAsync(conn, entry, ct);
        // Skip vs Java: SM_SYSTEM_MESSAGE(QUEST_FAILED_$1, questName) - no matching factory exposed; see header.
        return true;
    }
}
