// Port of Java data/scripts/system/handlers/quest/altgard/_2230AFriendlyWager.java (HellBoy).
// Talk to the wager npc (203621): ASK_QUEST_ACCEPT/QUEST_ACCEPT_1/QUEST_REFUSE_1 dialog chain,
// SETPRO1 starts the quest and a 1800s quest timer (cosmetic only - no onQuestTimerEnd override in
// Java either, so expiry is a no-op), then a single collect-item check flips straight to REWARD.
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

namespace Quest.Altgard;

public sealed class _2230AFriendlyWager : QuestHandlerBase
{
    private const int QuestIdConst = 2230;
    private const int WagerNpc     = 203621;

    private readonly IItemDao _itemDao;

    public _2230AFriendlyWager(IDataManager dataManager, IQuestDao questDao, QuestRewardService rewardService, IItemDao itemDao)
        : base(QuestIdConst, dataManager, questDao, rewardService)
    {
        _itemDao = itemDao;
    }

    public override void Register(QuestEngine engine)
    {
        engine.RegisterQuestNpc(WagerNpc).OnQuestStart.Add(QuestId);
        engine.RegisterQuestNpc(WagerNpc).OnTalk.Add(QuestId);
    }

    public override async ValueTask<bool> OnDialogAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var player      = env.Player;
        var entry       = player.Quests.Get(QuestId);
        int targetObjId = env.Target?.ObjectId ?? 0;
        var dialog      = DialogActionLookup.FromId(env.DialogId);

        if (env.TargetId != WagerNpc) return false;

        if (entry is null || entry.Status == QuestStatus.NONE)
        {
            switch (dialog)
            {
                case DialogAction.QUEST_SELECT:
                    return await SendQuestDialogAsync(conn, targetObjId, 1011, ct);
                case DialogAction.ASK_QUEST_ACCEPT:
                    return await SendQuestDialogAsync(conn, targetObjId, 4, ct);
                case DialogAction.QUEST_ACCEPT_1:
                    return await SendQuestDialogAsync(conn, targetObjId, 1003, ct);
                case DialogAction.QUEST_REFUSE_1:
                    return await SendQuestDialogAsync(conn, targetObjId, 1004, ct);
                case DialogAction.SETPRO1:
                    if (!await StartMissionAsync(conn, player, QuestStatus.START, ct)) return false;
                    StartQuestTimer(env, conn, 1800);
                    return true;
            }
        }

        if (entry is null) return false;

        if (entry.Status == QuestStatus.START)
        {
            int var = entry.GetVar(0);
            if (dialog == DialogAction.QUEST_SELECT && var == 0)
                return await SendQuestDialogAsync(conn, targetObjId, 2375, ct);
            if (dialog == DialogAction.CHECK_USER_HAS_QUEST_ITEM && var == 0)
                return await CheckQuestItemsAsync(env, conn, _itemDao, 0, 0, reward: true, checkOkId: 5, checkFailId: 2716, ct);
        }

        return await SendQuestRewardDialogAsync(env, conn, ct);
    }

    /// <summary>Java sendQuestRewardDialog(env, WagerNpc, 0): reportDialogId is 0 so the report-dialog
    /// branch never applies - only the turn-in fallback matters.</summary>
    private async ValueTask<bool> SendQuestRewardDialogAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var entry = env.Player.Quests.Get(QuestId);
        if (entry is not { Status: QuestStatus.REWARD } || env.TargetId != WagerNpc) return false;
        return await SendQuestEndDialogAsync(env, conn, ct);
    }
}
