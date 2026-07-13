// Port of Java data/scripts/system/handlers/quest/verteron/_1146DelicateMandrake.java (Mr.Poke,
// Dune11, modified xTz, Undertrey, reworked vlog). Accept from Gano (203123), who hands over a
// fragile Mandrake seedling and starts a 120s timer; deliver it intact to Krodis (203139) before
// the timer expires or the quest is auto-abandoned.
// Java bug: case USE_OBJECT falls through (missing break) into SELECT_QUEST_REWARD's body when
// the player no longer holds the mandrake item, silently auto-completing the quest instead of
// showing nothing. Ported here with an explicit guard instead. Case QUEST_ACCEPT_1's fallthrough
// into QUEST_REFUSE_1 on failure (bag full / already active) looks intentional (show the "refuse"
// page as a fallback) and is kept.
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

namespace Quest.Verteron;

public sealed class _1146DelicateMandrake : QuestHandlerBase
{
    private const int QuestIdConst = 1146;
    private const int GanoNpc      = 203123;
    private const int KrodisNpc    = 203139;
    private const int MandrakeItem = 182200519;

    private readonly IItemDao _itemDao;

    public _1146DelicateMandrake(IDataManager dataManager, IQuestDao questDao, QuestRewardService rewardService, IItemDao itemDao)
        : base(QuestIdConst, dataManager, questDao, rewardService)
    {
        _itemDao = itemDao;
    }

    public override void Register(QuestEngine engine)
    {
        engine.RegisterQuestNpc(GanoNpc).OnQuestStart.Add(QuestId);
        engine.RegisterQuestNpc(GanoNpc).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(KrodisNpc).OnTalk.Add(QuestId);
        engine.RegisterOnQuestTimerEnd(QuestId);
    }

    public override async ValueTask<bool> OnDialogAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var player = env.Player;
        var entry  = player.Quests.Get(QuestId);
        int targetObjId = env.Target?.ObjectId ?? 0;
        var dialog = DialogActionLookup.FromId(env.DialogId);

        if (entry is null || entry.Status == QuestStatus.NONE)
        {
            if (env.TargetId != GanoNpc) return false;
            switch (dialog)
            {
                case DialogAction.QUEST_SELECT:
                    return await SendQuestDialogAsync(conn, targetObjId, 1011, ct);
                case DialogAction.ASK_QUEST_ACCEPT:
                    return await SendQuestDialogAsync(conn, targetObjId, 4, ct);
                case DialogAction.QUEST_ACCEPT_1:
                {
                    if (await GiveQuestItemAsync(player, conn, _itemDao, MandrakeItem, 1, ct)
                        && await SendQuestStartDialogAsync(env, conn, ct))
                    {
                        StartQuestTimer(env, conn, 120);
                        return true; // SendQuestStartDialogAsync already opened dialog 1003
                    }
                    return await SendQuestDialogAsync(conn, targetObjId, 1004, ct);
                }
                case DialogAction.QUEST_REFUSE_1:
                    return await SendQuestDialogAsync(conn, targetObjId, 1004, ct);
                case DialogAction.FINISH_DIALOG:
                    return await SendQuestSelectionDialogAsync(conn, targetObjId, ct);
                default:
                    return false;
            }
        }

        if (entry.Status == QuestStatus.START)
        {
            if (env.TargetId != KrodisNpc) return false;
            switch (dialog)
            {
                case DialogAction.USE_OBJECT:
                    if ((player.Inventory.FindByItemId(MandrakeItem)?.Count ?? 0) > 0)
                        return await SendQuestDialogAsync(conn, targetObjId, 2375, ct);
                    return false;
                case DialogAction.SELECT_QUEST_REWARD:
                    await RemoveQuestItemAsync(player, conn, _itemDao, MandrakeItem, 1, ct);
                    await ChangeQuestStepAsync(conn, entry, 0, 0, toReward: true, ct);
                    // Java also cancels the pending quest timer here (no cancellation handle in
                    // this port; its later fire is a harmless no-op once status != START).
                    return await SendQuestDialogAsync(conn, targetObjId, 5, ct);
                default:
                    return false;
            }
        }

        if (entry.Status == QuestStatus.REWARD && env.TargetId == KrodisNpc)
            return await SendQuestEndDialogAsync(env, conn, ct);

        return false;
    }

    public override async ValueTask<bool> OnQuestTimerEndAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var player = env.Player;
        var entry  = player.Quests.Get(QuestId);
        if (entry is null || entry.Status != QuestStatus.START) return false;

        await RemoveQuestItemAsync(player, conn, _itemDao, MandrakeItem, 1, ct);

        player.Quests.Remove(QuestId);
        await QuestDao.DeleteAsync(player.ObjectId, QuestId, ct);
        await conn.SendAsync(new SM_QUEST_ACTION(QuestId), ct);
        await conn.SendAsync(new SM_QUEST_LIST(player.Quests.Active), ct);
        return true;
    }
}
