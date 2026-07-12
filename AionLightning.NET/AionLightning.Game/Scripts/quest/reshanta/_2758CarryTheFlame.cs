// Port of Java data/scripts/system/handlers/quest/reshanta/_2758CarryTheFlame.java (Cheatkiller).
// Offered at 279000 (OnQuestStart only — dispatched via the direct-questId path, not OnTalk, same
// as _1724/_1726/_1727); accepting gives item 182205645 and starts a 900s quest timer (Batch 0.3
// StartQuestTimer). Turn in the flame at 790016 within the window to finish; failing to do so
// removes the item and resets progress.
// Fixed a latent Java bug: the timer-expiry handler additionally required var0 > 1 before failing
// the quest, but var0 is set to exactly 1 by both the accept step and the (redundant) success step
// and never goes higher — that extra check was permanently false, so the timer never actually
// failed the quest in the original. Fixed by gating on status == START alone (which already becomes
// false once the player succeeds, so the intent — "fail only if still in progress" — is preserved).
// Skip vs Java: QuestService.questTimerEnd's explicit cancel-on-success has no equivalent (this
// port's timer is a fire-and-forget delay), and the QUEST_FAILED_$1 system message on timeout is
// dropped (SM_SYSTEM_MESSAGE's constructors are private outside its own file) — both are cosmetic;
// the timer callback still no-ops correctly once the quest is no longer START.
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

namespace Quest.Reshanta;

public sealed class _2758CarryTheFlame : QuestHandlerBase
{
    private const int QuestIdConst = 2758;
    private const int StartNpc     = 279000;
    private const int TurnInNpc    = 790016;
    private const int ItemId       = 182205645;
    private const int TimerSeconds = 900;

    private readonly IItemDao _itemDao;

    public _2758CarryTheFlame(IDataManager dataManager, IQuestDao questDao, QuestRewardService rewardService, IItemDao itemDao)
        : base(QuestIdConst, dataManager, questDao, rewardService)
    {
        _itemDao = itemDao;
    }

    public override void Register(QuestEngine engine)
    {
        engine.RegisterQuestNpc(StartNpc).OnQuestStart.Add(QuestId);
        engine.RegisterQuestNpc(TurnInNpc).OnTalk.Add(QuestId);
        engine.RegisterOnQuestTimerEnd(QuestId);
    }

    public override async ValueTask<bool> OnDialogAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var player = env.Player;
        var entry  = player.Quests.Get(QuestId);
        int targetId = env.TargetId;
        int targetObjId = env.Target?.ObjectId ?? 0;
        var dialog = DialogActionLookup.FromId(env.DialogId);

        if (targetId == StartNpc)
        {
            if (entry is null)
            {
                if (dialog == DialogAction.QUEST_SELECT)
                    return await SendQuestDialogAsync(conn, targetObjId, 4762, ct);
                return await SendQuestStartDialogAsync(env, conn, ct);
            }
            if (entry.Status == QuestStatus.START)
            {
                if (dialog == DialogAction.QUEST_SELECT)
                    return await SendQuestDialogAsync(conn, targetObjId, 1011, ct);
                if (dialog == DialogAction.SETPRO1)
                {
                    if (!await GiveQuestItemAsync(player, conn, _itemDao, ItemId, 1, ct)) return true;
                    StartQuestTimer(env, conn, TimerSeconds);
                    return await DefaultCloseDialogAsync(env, conn, 0, 1, ct);
                }
            }
            return false;
        }

        if (targetId == TurnInNpc)
        {
            if (entry is { Status: QuestStatus.START })
            {
                if (dialog == DialogAction.QUEST_SELECT)
                    return await SendQuestDialogAsync(conn, targetObjId, 1352, ct);
                if (dialog == DialogAction.SET_SUCCEED)
                {
                    await RemoveQuestItemAsync(player, conn, _itemDao, ItemId, 1, ct);
                    entry.SetVar(0, 1);
                    entry.Status = QuestStatus.REWARD;
                    await UpdateQuestStatusAsync(conn, entry, ct);
                    return await SendQuestDialogAsync(conn, targetObjId, 5, ct);
                }
            }
            else if (entry is { Status: QuestStatus.REWARD })
            {
                if (dialog == DialogAction.USE_OBJECT)
                    return await SendQuestDialogAsync(conn, targetObjId, 5, ct);
                return await SendQuestEndDialogAsync(env, conn, ct);
            }
        }

        return false;
    }

    public override async ValueTask<bool> OnQuestTimerEndAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var player = env.Player;
        var entry  = player.Quests.Get(QuestId);
        if (entry is not { Status: QuestStatus.START }) return false;

        await RemoveQuestItemAsync(player, conn, _itemDao, ItemId, 1, ct);
        entry.SetVar(0, 0);
        await UpdateQuestStatusAsync(conn, entry, ct);
        return true;
    }
}
