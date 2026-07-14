// Port of Java data/scripts/system/handlers/quest/event_quests/_80033EventAvertingTheGaze.java (Asmodian, npc 799781).
// note: Java EventService.checkQuestIsActive (global event-toggle config) is not ported; the IsQuestActive
//   base helper reinterprets it as "player has a non-COMPLETE entry", so the parent-quest gate follows
//   that mapping rather than a server event switch.
// note: Java QuestService.startEventQuest can re-run a COMPLETE event quest; there is no event-restart
//   primitive in this port, so StartMissionAsync is used — it only starts the quest when the player has
//   no entry yet.
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

namespace Quest.EventQuests;

public sealed class _80033EventAvertingTheGaze : QuestHandlerBase
{
    private const int QuestIdConst = 80033;
    private const int Npc          = 799781;
    private const int ParentQuest  = 80032;
    private const int CharmCard     = 188051133; // [Event] Charm Card
    private const int BeritrasGaze  = 164002015;

    private readonly IItemDao _itemDao;

    public _80033EventAvertingTheGaze(IDataManager dataManager, IQuestDao questDao, QuestRewardService rewardService, IItemDao itemDao)
        : base(QuestIdConst, dataManager, questDao, rewardService)
    {
        _itemDao = itemDao;
    }

    public override void Register(QuestEngine engine)
    {
        engine.RegisterQuestNpc(Npc).OnQuestStart.Add(QuestId);
        engine.RegisterQuestNpc(Npc).OnTalk.Add(QuestId);
        engine.RegisterQuestItem(CharmCard, QuestId);
    }

    public override async ValueTask<bool> OnDialogAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var player = env.Player;
        int targetObjId = env.Target?.ObjectId ?? 0;
        var dialog = DialogActionLookup.FromId(env.DialogId);

        var entry = player.Quests.Get(QuestId);
        if (entry is null || entry.Status == QuestStatus.NONE) return false;

        if (entry.Status == QuestStatus.START && env.TargetId == Npc)
        {
            if (dialog == DialogAction.QUEST_SELECT)
                return await SendQuestDialogAsync(conn, targetObjId, 1011, ct);
            if (dialog == DialogAction.QUEST_ACCEPT_1)
            {
                long gaze = player.Inventory.FindByItemId(BeritrasGaze)?.Count ?? 0;
                return await SendQuestDialogAsync(conn, targetObjId, gaze > 0 ? 2375 : 2716, ct);
            }
            if (dialog == DialogAction.SELECT_QUEST_REWARD)
            {
                if (entry.GetVar(0) == 0)
                    await DefaultCloseDialogAsync(env, conn, _itemDao, 0, 1, reward: true, sameNpc: true,
                        giveItemId: 0, giveItemCount: 0, removeItemId: BeritrasGaze, removeItemCount: 1, ct);
                return await SendQuestDialogAsync(conn, targetObjId, 5, ct);
            }
            if (dialog == DialogAction.SELECTED_QUEST_NOREWARD)
                return await SendQuestRewardDialogAsync(env, conn, 5, ct);
            return await SendQuestStartDialogAsync(env, conn, ct);
        }

        return await SendQuestRewardDialogAsync(env, conn, 0, ct);
    }

    public override async ValueTask<bool> OnItemUseAsync(Player player, int itemId, GsClientConnection conn, CancellationToken ct)
    {
        // Java: bail when the parent quest (which awards the Charm Cards) isn't active (HandlerResult.FAILED).
        if (!IsQuestActive(player, ParentQuest)) return false;

        // The same Charm Card item is registered for both the Elyos (80030) and Asmodian (this) handlers;
        // gate on race so we only start the Asmodian chain here.
        if (itemId == CharmCard && player.Race == Race.ASMODIANS)
        {
            // note: Java schedules the body 10s later via ThreadPoolManager; that delay is cosmetic and dropped.
            long gaze = player.Inventory.FindByItemId(BeritrasGaze)?.Count ?? 0;
            if (gaze > 0) // got a Beritra's Gaze -> start this quest
                await StartMissionAsync(conn, player, QuestStatus.START, ct);

            // note: Java also (re)starts the sibling Asmodian event quests 80037/80038/80039 here (each when
            //   the player holds enough of its restart item) via QuestService.startEventQuest on that
            //   quest's own QuestEnv. Starting another quest's entry from this handler isn't cleanly
            //   expressible (each sibling owns its level/collect gate), and those quests already
            //   self-(re)start from their own onLevelUp item-count checks, so the cross-starts are not
            //   replicated here.
            return true; // Java HandlerResult.SUCCESS
        }
        return false; // Java HandlerResult.UNKNOWN
    }

    // Java sendQuestRewardDialog(env, 799781, reportDialogId): a REWARD-status turn-in shows
    // reportDialogId on USE_OBJECT (when non-zero), else finishes the quest.
    private async ValueTask<bool> SendQuestRewardDialogAsync(QuestEnv env, GsClientConnection conn, int reportDialogId, CancellationToken ct)
    {
        var entry = env.Player.Quests.Get(QuestId);
        if (entry is not { Status: QuestStatus.REWARD } || env.TargetId != Npc) return false;

        int targetObjId = env.Target?.ObjectId ?? 0;
        if (reportDialogId != 0 && DialogActionLookup.FromId(env.DialogId) == DialogAction.USE_OBJECT)
            return await SendQuestDialogAsync(conn, targetObjId, reportDialogId, ct);
        return await SendQuestEndDialogAsync(env, conn, ct);
    }
}
