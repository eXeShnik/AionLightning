// Port of Java data/scripts/system/handlers/quest/event_quests/_80016EventSockHop.java (npc 799763).
// note: onBonusApply(MOVIE) is registered + ported for parity but never fires yet — the event-bonus
//   system (BonusType/EventService bonus dispatch) is not ported. Kept faithful (unreachable until it lands).
// note: Java EventService.checkQuestIsActive is a global event-toggle config that is not ported; the
//   IsQuestActive base helper reinterprets it as "player has a non-COMPLETE entry", so the level-up
//   start/abandon branches follow that mapping rather than a server event switch.
// note: Java QuestService.startEventQuest can re-run a COMPLETE event quest (bumping completeCount);
//   there is no event-restart primitive in this port, so StartMissionAsync is used — it only starts
//   the quest when the player has no entry yet. Re-runs past first completion are not modelled.
using System;
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

namespace Quest.EventQuests;

public sealed class _80016EventSockHop : QuestHandlerBase
{
    private const int QuestIdConst = 80016;
    private const int Npc          = 799763;

    private readonly IItemDao _itemDao;

    public _80016EventSockHop(IDataManager dataManager, IQuestDao questDao, QuestRewardService rewardService, IItemDao itemDao)
        : base(QuestIdConst, dataManager, questDao, rewardService)
    {
        _itemDao = itemDao;
    }

    public override void Register(QuestEngine engine)
    {
        engine.RegisterOnLevelUp(QuestId);
        engine.RegisterQuestNpc(Npc).OnQuestStart.Add(QuestId);
        engine.RegisterQuestNpc(Npc).OnTalk.Add(QuestId);
        RegisterOnBonusApply(engine, "MOVIE");
    }

    public override ValueTask<bool> OnLevelUpAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
        => OnLvlUpEventAsync(env, conn, ct);

    public override async ValueTask<bool> OnDialogAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var player = env.Player;
        int targetObjId = env.Target?.ObjectId ?? 0;
        var dialog = DialogActionLookup.FromId(env.DialogId);

        var entry = player.Quests.Get(QuestId);
        if ((entry is null || entry.Status == QuestStatus.NONE) && !await OnLvlUpEventAsync(env, conn, ct))
            return false;

        entry = player.Quests.Get(QuestId); // OnLvlUpEventAsync may have created the entry.

        if (entry is null || entry.Status == QuestStatus.NONE ||
            (entry.Status == QuestStatus.COMPLETE && entry.CompleteCount < 10))
        {
            if (env.TargetId == Npc)
            {
                switch (dialog)
                {
                    case DialogAction.USE_OBJECT:
                        return await SendQuestDialogAsync(conn, targetObjId, 1011, ct);
                    case DialogAction.QUEST_ACCEPT_1:
                        await StartMissionAsync(conn, player, QuestStatus.START, ct);
                        return await SendQuestDialogAsync(conn, targetObjId, 1003, ct);
                    default:
                        return await SendQuestStartDialogAsync(env, conn, ct);
                }
            }
            return false;
        }

        int var = entry.GetVar(0);

        if (entry.Status == QuestStatus.START && env.TargetId == Npc)
        {
            // Java switch fallthrough: USE_OBJECT/QUEST_SELECT with var==0 shows dialog 2375,
            // otherwise falls through into the CHECK_USER_HAS_QUEST_ITEM collect check.
            if (dialog == DialogAction.USE_OBJECT || dialog == DialogAction.QUEST_SELECT)
            {
                if (var == 0)
                    return await SendQuestDialogAsync(conn, targetObjId, 2375, ct);
                return await CheckQuestItemsAsync(env, conn, _itemDao, 0, 1, true, 5, 2716, ct);
            }
            if (dialog == DialogAction.CHECK_USER_HAS_QUEST_ITEM)
                return await CheckQuestItemsAsync(env, conn, _itemDao, 0, 1, true, 5, 2716, ct);
        }

        return await SendQuestRewardDialogAsync(env, conn, ct);
    }

    public override async ValueTask<HandlerResult> OnBonusApplyAsync(QuestEnv env, string bonusType, GsClientConnection conn, CancellationToken ct)
    {
        if (bonusType != "MOVIE" || env.QuestId != QuestId)
            return HandlerResult.Unknown;

        var player = env.Player;
        var entry = player.Quests.Get(QuestId);
        if (entry is { Status: QuestStatus.REWARD })
        {
            // note: Java appends the [Event] Hat Box (188051106) to the bonus rewardItems list when
            //   completeCount == 9. The bonus-apply hook exposes no reward-item list in this port, so
            //   that extra reward cannot be added here (unreachable until the event-bonus system lands).
            if (Random.Shared.Next(100) < 50)
                await PlayQuestMovieAsync(conn, player, 103, ct);
            else
                await PlayQuestMovieAsync(conn, player, 104, ct);
            return HandlerResult.Success;
        }
        return HandlerResult.Failed;
    }

    private async ValueTask<bool> OnLvlUpEventAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var player = env.Player;
        var entry = player.Quests.Get(QuestId);

        if (IsQuestActive(player, QuestId))
        {
            // note: Java QuestService.checkLevelRequirement checks a [min,max] band; only
            //   minlevel_permitted is modelled by the template here.
            if (Template is null || player.Level < Template.MinLevel)
                return false;

            if (entry is null || entry.Status == QuestStatus.NONE)
                return await StartMissionAsync(conn, player, QuestStatus.START, ct);
        }
        else if (entry is not null)
        {
            await AbandonQuestAsync(conn, player, ct);
        }
        return false;
    }

    // Java QuestService.abandonQuest: drop the quest entry and notify the client.
    private async ValueTask AbandonQuestAsync(GsClientConnection conn, Player player, CancellationToken ct)
    {
        if (player.Quests.Get(QuestId) is null) return;
        player.Quests.Remove(QuestId);
        await QuestDao.DeleteAsync(player.ObjectId, QuestId, ct);
        await conn.SendAsync(new SM_QUEST_ACTION(QuestId), ct);
    }

    // Java sendQuestRewardDialog(env, 799763, 0): reportDialogId 0 means a REWARD-status turn-in
    // always finishes the quest directly (no USE_OBJECT report page).
    private async ValueTask<bool> SendQuestRewardDialogAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var entry = env.Player.Quests.Get(QuestId);
        if (entry is not { Status: QuestStatus.REWARD } || env.TargetId != Npc) return false;
        return await SendQuestEndDialogAsync(env, conn, ct);
    }
}
