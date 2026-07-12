// Port of Java data/scripts/system/handlers/quest/danaria/_23009AFreshCut.java.
// Simple single-npc (801108) quest: accept, then on SELECT_QUEST_REWARD checks the player is
// carrying more than 8 of item 182213412 before flipping to reward and removing 9 of them.
// Java calls changeQuestStep(env, 0, 1, true) here - since reward=true "ignores" nextStep in the
// shared helper's Java original, the var is left at 0 (only the status flips); ported with a raw
// status flip (no var write) to match exactly, rather than the var-writing DefaultCloseDialogAsync.
// Skip vs Java: the failure branch's raw chat text ("You dont have enough quest items") has no
// localized system-message id and no plain server-text packet precedent in this port; skipped,
// the check-item dialog is simply re-shown so the flow itself is unaffected.
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

namespace Quest.Danaria;

public sealed class _23009AFreshCut : QuestHandlerBase
{
    private const int QuestIdConst = 23009;
    private const int Npc          = 801108;
    private const int ItemId       = 182213412;

    private readonly IItemDao _itemDao;

    public _23009AFreshCut(IDataManager dataManager, IQuestDao questDao, QuestRewardService rewardService, IItemDao itemDao)
        : base(QuestIdConst, dataManager, questDao, rewardService)
    {
        _itemDao = itemDao;
    }

    public override void Register(QuestEngine engine)
    {
        engine.RegisterQuestNpc(Npc).OnQuestStart.Add(QuestId);
        engine.RegisterQuestNpc(Npc).OnTalk.Add(QuestId);
    }

    public override async ValueTask<bool> OnDialogAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var player = env.Player;
        var entry  = player.Quests.Get(QuestId);
        int targetId    = env.TargetId;
        int targetObjId = env.Target?.ObjectId ?? 0;
        var dialog      = DialogActionLookup.FromId(env.DialogId);

        if (targetId != Npc) return false;

        if (entry is null || entry.Status == QuestStatus.NONE)
        {
            if (dialog == DialogAction.QUEST_SELECT)
                return await SendQuestDialogAsync(conn, targetObjId, 1011, ct);
            return await SendQuestStartDialogAsync(env, conn, ct);
        }

        if (entry.Status == QuestStatus.START)
        {
            if (dialog == DialogAction.QUEST_SELECT)
                return await SendQuestDialogAsync(conn, targetObjId, 2375, ct);
            if (dialog == DialogAction.SELECT_QUEST_REWARD)
            {
                if ((player.Inventory.FindByItemId(ItemId)?.Count ?? 0) > 8)
                {
                    entry.Status = QuestStatus.REWARD;
                    await UpdateQuestStatusAsync(conn, entry, ct);
                    await RemoveQuestItemAsync(player, conn, _itemDao, ItemId, 9, ct);
                    return await SendQuestEndDialogAsync(env, conn, ct);
                }
                return await SendQuestDialogAsync(conn, targetObjId, 2375, ct);
            }
        }

        if (entry.Status == QuestStatus.REWARD)
            return await SendQuestEndDialogAsync(env, conn, ct);

        return false;
    }
}
