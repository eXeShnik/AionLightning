// Port of Java data/scripts/system/handlers/quest/sarpan/_41270StockSoupServedTooLate.java (Cheatkiller).
// Talk to 205762 to start (gives item 182213152); hand it to 205793 (var forced to 1, then
// flipped to REWARD, item removed, page 10 shown); turn in at 205762.
using System.Threading;
using System.Threading.Tasks;
using AionLightning.Game.Dao;
using AionLightning.Game.DataHolders;
using AionLightning.Game.Model;
using AionLightning.Game.Model.Quest;
using AionLightning.Game.Network.Aion;
using AionLightning.Game.QuestEngine;
using AionLightning.Game.QuestEngine.Handlers;
using AionLightning.Game.QuestEngine.Model;
using AionLightning.Game.Services;

namespace Quest.Sarpan;

public sealed class _41270StockSoupServedTooLate : QuestHandlerBase
{
    private const int QuestIdConst = 41270;
    private const int StartNpc      = 205762;
    private const int TurnInNpc     = 205793;
    private const int ItemId        = 182213152;

    private readonly IItemDao _itemDao;

    public _41270StockSoupServedTooLate(IDataManager dataManager, IQuestDao questDao, QuestRewardService rewardService, IItemDao itemDao)
        : base(QuestIdConst, dataManager, questDao, rewardService)
    {
        _itemDao = itemDao;
    }

    public override void Register(QuestEngine engine)
    {
        engine.RegisterQuestNpc(StartNpc).OnQuestStart.Add(QuestId);
        engine.RegisterQuestNpc(StartNpc).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(TurnInNpc).OnTalk.Add(QuestId);
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
            if (targetId != StartNpc) return false;
            if (dialog == DialogAction.QUEST_SELECT) return await SendQuestDialogAsync(conn, targetObjId, 1011, ct);
            return await StartWithItemAsync(player, conn, targetObjId, dialog, ct);
        }

        if (entry.Status == QuestStatus.START && targetId == TurnInNpc)
        {
            if (dialog == DialogAction.QUEST_SELECT && entry.GetVar(0) == 0)
                return await SendQuestDialogAsync(conn, targetObjId, 1352, ct);
            if (dialog == DialogAction.SETPRO1)
            {
                await RemoveQuestItemAsync(player, conn, _itemDao, ItemId, 1, ct);
                entry.SetVar(0, 1);
                await ChangeQuestStepAsync(conn, entry, varIdx: -1, newValue: 0, toReward: true, ct);
                return await SendQuestSelectionDialogAsync(conn, targetObjId, ct);
            }
            return false;
        }

        if (entry.Status == QuestStatus.REWARD && targetId == StartNpc)
        {
            if (dialog == DialogAction.USE_OBJECT) return await SendQuestDialogAsync(conn, targetObjId, 2375, ct);
            return await SendQuestEndDialogAsync(env, conn, ct);
        }

        return false;
    }

    /// <summary>
    /// Mirrors Java's item-bearing <c>sendQuestStartDialog(env, itemId, itemCount)</c>: creates the
    /// entry, gives the item, and shows the confirm page (QUEST_ACCEPT/QUEST_ACCEPT_1) or closes
    /// directly (QUEST_ACCEPT_SIMPLE) — same shape reused by the other item-start quests in this
    /// batch (QuestHandlerBase has no equivalent helper to call instead).
    /// </summary>
    private async ValueTask<bool> StartWithItemAsync(Player player, GsClientConnection conn, int targetObjId, DialogAction dialog, CancellationToken ct)
    {
        switch (dialog)
        {
            case DialogAction.QUEST_ACCEPT:
            case DialogAction.QUEST_ACCEPT_1:
                await StartMissionAsync(conn, player, QuestStatus.START, ct);
                await GiveQuestItemAsync(player, conn, _itemDao, ItemId, 1, ct);
                return await SendQuestDialogAsync(conn, targetObjId, 1003, ct);
            case DialogAction.QUEST_ACCEPT_SIMPLE:
                await StartMissionAsync(conn, player, QuestStatus.START, ct);
                await GiveQuestItemAsync(player, conn, _itemDao, ItemId, 1, ct);
                return await CloseDialogWindowAsync(conn, targetObjId, ct);
            case DialogAction.QUEST_REFUSE_1:
            case DialogAction.QUEST_REFUSE_2:
                return await SendQuestDialogAsync(conn, targetObjId, 1004, ct);
            case DialogAction.QUEST_REFUSE_SIMPLE:
                return await CloseDialogWindowAsync(conn, targetObjId, ct);
            default:
                return false;
        }
    }
}
