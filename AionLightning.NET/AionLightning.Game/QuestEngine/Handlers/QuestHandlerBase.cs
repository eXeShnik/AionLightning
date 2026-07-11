using AionLightning.Game.Dao;
using AionLightning.Game.DataHolders;
using AionLightning.Game.Model;
using AionLightning.Game.Model.Quest;
using AionLightning.Game.Model.Templates.Quest;
using AionLightning.Game.Network.Aion;
using AionLightning.Game.Network.Aion.ServerPackets;
using AionLightning.Game.QuestEngine.Model;
using AionLightning.Game.Services;

namespace AionLightning.Game.QuestEngine.Handlers;

/// <summary>
/// Base class for data-driven quest handlers (Java <c>questEngine.handlers.QuestHandler</c> port).
/// Provides the dialog/step helpers shared by every template: showing dialog pages, accepting a
/// quest, changing a var/step, and handing off reward payout to <see cref="QuestRewardService"/>.
/// </summary>
public abstract class QuestHandlerBase : IQuestHandler
{
    protected readonly IDataManager       DataManager;
    protected readonly IQuestDao          QuestDao;
    protected readonly QuestRewardService RewardService;

    public int QuestId { get; }

    protected QuestHandlerBase(int questId, IDataManager dataManager, IQuestDao questDao, QuestRewardService rewardService)
    {
        QuestId       = questId;
        DataManager   = dataManager;
        QuestDao      = questDao;
        RewardService = rewardService;
    }

    /// <summary>The static quest_data.xml template for this quest, or null if not defined there.</summary>
    protected QuestTemplate? Template => DataManager.Quests.GetTemplate(QuestId);

    public abstract void Register(QuestEngine engine);

    public virtual ValueTask<bool> OnDialogAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct) => ValueTask.FromResult(false);
    public virtual ValueTask<bool> OnKillAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct) => ValueTask.FromResult(false);
    public virtual ValueTask<bool> OnItemGetAsync(Player player, int itemId, GsClientConnection conn, CancellationToken ct) => ValueTask.FromResult(false);
    public virtual ValueTask<bool> OnSkillUseAsync(Player player, int skillId, GsClientConnection conn, CancellationToken ct) => ValueTask.FromResult(false);

    /// <summary>Opens an NPC dialog page (Java QuestHandler.sendDialogPacket). Always returns true (handled).</summary>
    protected async ValueTask<bool> SendQuestDialogAsync(GsClientConnection conn, int targetObjId, int dialogPageId, CancellationToken ct)
    {
        await conn.SendAsync(new SM_DIALOG_WINDOW(targetObjId, dialogPageId, QuestId), ct);
        return true;
    }

    /// <summary>Closes the dialog window (Java QuestHandler.closeDialogWindow).</summary>
    protected ValueTask<bool> CloseDialogWindowAsync(GsClientConnection conn, int targetObjId, CancellationToken ct)
        => SendQuestDialogAsync(conn, targetObjId, 0, ct);

    /// <summary>
    /// Handles the accept/refuse leg of quest start (Java QuestHandler.sendQuestStartDialog).
    /// QUEST_ACCEPT/QUEST_ACCEPT_1 create the entry (status START) and show the accept-confirm
    /// page (1003); QUEST_REFUSE variants close the dialog. Any other action is unhandled.
    /// </summary>
    protected async ValueTask<bool> SendQuestStartDialogAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var player   = env.Player;
        int targetObjId = env.Target?.ObjectId ?? 0;

        switch (DialogActionLookup.FromId(env.DialogId))
        {
            case DialogAction.QUEST_ACCEPT:
            case DialogAction.QUEST_ACCEPT_1:
            {
                if (player.Quests.Contains(QuestId)) return false;

                var entry = new QuestEntry { QuestId = QuestId, Status = QuestStatus.START };
                player.Quests.Add(entry);
                await QuestDao.UpsertAsync(player.ObjectId, entry, ct);

                await conn.SendAsync(new SM_QUEST_ACTION(entry.QuestId,
                    SM_QUEST_ACTION.ActionType.Accept, (byte)entry.Status, entry.Step), ct);
                await conn.SendAsync(new SM_QUEST_LIST(player.Quests.Active), ct);

                return await SendQuestDialogAsync(conn, targetObjId, 1003, ct);
            }

            case DialogAction.QUEST_REFUSE:
            case DialogAction.QUEST_REFUSE_1:
            case DialogAction.QUEST_REFUSE_2:
            case DialogAction.QUEST_REFUSE_SIMPLE:
                return await CloseDialogWindowAsync(conn, targetObjId, ct);

            default:
                return false;
        }
    }

    /// <summary>
    /// Handles quest completion (Java QuestHandler.sendQuestEndDialog). Refuses unless the quest
    /// is in REWARD status (exploit guard) and the action is SELECT_QUEST_REWARD; delegates the
    /// actual payout to <see cref="QuestRewardService"/> so it isn't duplicated per template.
    /// </summary>
    protected async ValueTask<bool> SendQuestEndDialogAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var player = env.Player;
        var entry  = player.Quests.Get(QuestId);
        if (entry is null || entry.Status != QuestStatus.REWARD) return false;

        if (DialogActionLookup.FromId(env.DialogId) != DialogAction.SELECT_QUEST_REWARD) return false;

        var template = Template;
        if (template is null) return false;

        return await RewardService.GrantAndCompleteAsync(conn, player, entry, template, env.RewardIndex, ct);
    }

    /// <summary>Sets a quest var and/or transitions to REWARD, then broadcasts the update (Java changeQuestStep).</summary>
    protected async ValueTask ChangeQuestStepAsync(GsClientConnection conn, QuestEntry entry, int varIdx, int newValue, bool toReward, CancellationToken ct)
    {
        if (varIdx >= 0) entry.SetVar(varIdx, newValue);
        if (toReward) entry.Status = QuestStatus.REWARD;
        await UpdateQuestStatusAsync(conn, entry, ct);
    }

    /// <summary>Persists the entry and sends the SM_QUEST_ACTION step-update (Java updateQuestStatus).</summary>
    protected async ValueTask UpdateQuestStatusAsync(GsClientConnection conn, QuestEntry entry, CancellationToken ct)
    {
        var player = conn.ActivePlayer;
        if (player is null) return;

        await QuestDao.UpsertAsync(player.ObjectId, entry, ct);
        await conn.SendAsync(new SM_QUEST_ACTION(entry.QuestId,
            SM_QUEST_ACTION.ActionType.StepUpdate, (byte)entry.Status, entry.Step), ct);
        if (entry.Status is QuestStatus.REWARD or QuestStatus.COMPLETE)
            await conn.SendAsync(new SM_QUEST_LIST(player.Quests.Active), ct);
    }
}
