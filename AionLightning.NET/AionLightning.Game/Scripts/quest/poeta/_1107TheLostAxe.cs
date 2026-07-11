// Port of Java data/scripts/system/handlers/quest/poeta/_1107TheLostAxe.java (MrPoke).
// Quest-item quest: using the Lost Axe (182200501) opens the accept dialog (page 4); accept
// starts it; return the axe to Melanalu (203075) via SELECT_QUEST_REWARD to finish.
// Uses the Batch 0.1 OnItemUse trigger hook + FinishQuestAsync.
// Skip vs Java: the SM_ITEM_USAGE_ANIMATION broadcast on item use is omitted (no such packet
// yet) — purely cosmetic, the dialog still opens.
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AionLightning.Game.Dao;
using AionLightning.Game.DataHolders;
using AionLightning.Game.Model;
using AionLightning.Game.Model.Quest;
using AionLightning.Game.Network.Aion;
using AionLightning.Game.Network.Aion.ServerPackets;
using AionLightning.Game.QuestEngine;
using AionLightning.Game.QuestEngine.Handlers;
using AionLightning.Game.QuestEngine.Model;
using AionLightning.Game.Services;

namespace Quest.Poeta;

public sealed class _1107TheLostAxe : QuestHandlerBase
{
    private const int QuestIdConst = 1107;
    private const int NpcId        = 203075;
    private const int AxeItemId    = 182200501;

    private readonly IItemDao _itemDao;

    public _1107TheLostAxe(IDataManager dataManager, IQuestDao questDao, QuestRewardService rewardService, IItemDao itemDao)
        : base(QuestIdConst, dataManager, questDao, rewardService)
    {
        _itemDao = itemDao;
    }

    public override void Register(QuestEngine engine)
    {
        engine.RegisterQuestNpc(NpcId).OnTalk.Add(QuestId);
        engine.RegisterQuestItem(AxeItemId, QuestId);
    }

    // Java onItemUseEvent: using the axe when the quest isn't started opens the accept dialog.
    public override async ValueTask<bool> OnItemUseAsync(Player player, int itemId, GsClientConnection conn, CancellationToken ct)
    {
        if (itemId != AxeItemId) return false;
        var entry = player.Quests.Get(QuestId);
        if (entry is null || entry.Status == QuestStatus.NONE)
        {
            // dialog page 4 = ASK_QUEST_ACCEPT; env target 0 (item source)
            await conn.SendAsync(new SM_DIALOG_WINDOW(0, 4, QuestId), ct);
        }
        return true;
    }

    public override async ValueTask<bool> OnDialogAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var player = env.Player;
        var entry  = player.Quests.Get(QuestId);
        int targetId = env.TargetId;
        var dialog   = DialogActionLookup.FromId(env.DialogId);

        // Item-sourced accept (targetId 0): create the quest at START
        if (targetId == 0)
        {
            if (dialog == DialogAction.QUEST_ACCEPT_1)
            {
                await StartMissionAsync(conn, player, QuestStatus.START, ct);
                await conn.SendAsync(new SM_DIALOG_WINDOW(0, 0), ct);
                return true;
            }
            await conn.SendAsync(new SM_DIALOG_WINDOW(0, 0), ct);
            return false;
        }

        if (targetId == NpcId && entry is not null)
        {
            int targetObjId = env.Target?.ObjectId ?? 0;
            if (dialog == DialogAction.QUEST_SELECT && entry.Status == QuestStatus.START)
                return await SendQuestDialogAsync(conn, targetObjId, 2375, ct);

            if (env.DialogId == (int)DialogAction.SELECT_QUEST_REWARD
                && entry.Status != QuestStatus.COMPLETE && entry.Status != QuestStatus.NONE)
            {
                await RemoveQuestItemAsync(player, conn, _itemDao, AxeItemId, 1, ct);
                entry.SetVar(0, 1);
                entry.Status = QuestStatus.REWARD;
                await UpdateQuestStatusAsync(conn, entry, ct);
                return await SendQuestEndDialogAsync(env, conn, ct);
            }
            return await SendQuestEndDialogAsync(env, conn, ct);
        }
        return false;
    }
}
