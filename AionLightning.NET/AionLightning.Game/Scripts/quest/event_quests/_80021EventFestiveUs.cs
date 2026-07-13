// Port of Java data/scripts/system/handlers/quest/event_quests/_80021EventFestiveUs.java.
// Asmodian mirror of _80020EventSoloriusJoy: talk to rangidax (799784, gives item 182214014 on
// accept); report to zubinerk (799783, gives 2x 182214015 and removes 182214014); "scare" skanin
// (203618, var 1, removes 1x 182214015); "scare" grak (203650, var 2, removes the last 182214015,
// flips to REWARD); turn in back at rangidax.
// Skip vs Java: (1) the two sendEmotion(...) broadcasts (EmotionId.NO / EmotionId.PANIC) — no
// emotion-broadcast infra in this port, same skip as Scripts/quest/eltnen/_1468HannetsLostLove.cs;
// (2) the qs==null/NONE (start) gate's inline "!onLvlUpEvent(env)" pre-check and the
// COMPLETE-repeat-under-maxRepeatCount branch — QuestTemplate has no MaxRepeatCount field in this
// port and there is no EventService, so (like _2798SignontheDottedLine.cs's sendQuestNoneDialog
// helper) starting is approximated as "no active entry, always allowed" and COMPLETE is treated as
// terminal (not repeatable). RegisterOnLevelUp is kept for structural parity only.
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

namespace Quest.EventQuests;

public sealed class _80021EventFestiveUs : QuestHandlerBase
{
    private const int QuestIdConst = 80021;
    private const int RangidaxNpc  = 799784;
    private const int ZubinerkNpc  = 799783;
    private const int SkaninNpc    = 203618;
    private const int GrakNpc      = 203650;
    private const int StartItem    = 182214014;
    private const int RelayItem    = 182214015;

    private readonly IItemDao _itemDao;

    public _80021EventFestiveUs(IDataManager dataManager, IQuestDao questDao, QuestRewardService rewardService, IItemDao itemDao)
        : base(QuestIdConst, dataManager, questDao, rewardService)
    {
        _itemDao = itemDao;
    }

    public override void Register(QuestEngine engine)
    {
        engine.RegisterOnLevelUp(QuestId);
        engine.RegisterQuestNpc(RangidaxNpc).OnQuestStart.Add(QuestId);
        foreach (int npc in new[] { RangidaxNpc, ZubinerkNpc, SkaninNpc, GrakNpc })
            engine.RegisterQuestNpc(npc).OnTalk.Add(QuestId);
    }

    public override async ValueTask<bool> OnDialogAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var player = env.Player;
        var entry  = player.Quests.Get(QuestId);
        int targetObjId = env.Target?.ObjectId ?? 0;
        var dialog = DialogActionLookup.FromId(env.DialogId);

        if (entry is null || entry.Status == QuestStatus.NONE)
        {
            if (env.TargetId != RangidaxNpc) return false;
            if (dialog is DialogAction.USE_OBJECT or DialogAction.QUEST_SELECT)
                return await SendQuestDialogAsync(conn, targetObjId, 1011, ct);
            return await SendQuestNoneDialogAsync(env, conn, StartItem, 1, ct);
        }

        int var = entry.GetVar(0);

        if (entry.Status == QuestStatus.START)
        {
            if (env.TargetId == ZubinerkNpc)
            {
                if (dialog is DialogAction.USE_OBJECT or DialogAction.QUEST_SELECT)
                    return await SendQuestDialogAsync(conn, targetObjId, 1352, ct);
                if (dialog == DialogAction.SETPRO1)
                {
                    await DefaultCloseDialogAsync(env, conn, _itemDao, 0, 1, reward: false, sameNpc: false,
                        giveItemId: RelayItem, giveItemCount: 2, removeItemId: StartItem, removeItemCount: 1, ct);
                    return true;
                }
                return await SendQuestStartDialogAsync(env, conn, ct);
            }

            if (env.TargetId == SkaninNpc && var == 1)
            {
                if (dialog is DialogAction.USE_OBJECT or DialogAction.QUEST_SELECT)
                    return await SendQuestDialogAsync(conn, targetObjId, 1693, ct);
                if (dialog == DialogAction.SELECT_ACTION_1694)
                    return await SendQuestDialogAsync(conn, targetObjId, 1694, ct);
                if (dialog == DialogAction.SETPRO2)
                {
                    await DefaultCloseDialogAsync(env, conn, _itemDao, 1, 2, reward: false, sameNpc: false,
                        giveItemId: 0, giveItemCount: 0, removeItemId: RelayItem, removeItemCount: 1, ct);
                    return true;
                }
                return await SendQuestStartDialogAsync(env, conn, ct);
            }

            if (env.TargetId == GrakNpc && var == 2)
            {
                if (dialog == DialogAction.QUEST_SELECT)
                    return await SendQuestDialogAsync(conn, targetObjId, 2034, ct);
                if (dialog == DialogAction.SELECT_ACTION_2035)
                    return await SendQuestDialogAsync(conn, targetObjId, 2035, ct);
                if (dialog == DialogAction.SETPRO3)
                    return await DefaultCloseDialogAsync(env, conn, _itemDao, 2, 3, reward: true, sameNpc: false,
                        giveItemId: 0, giveItemCount: 0, removeItemId: RelayItem, removeItemCount: 1, ct);
                return await SendQuestStartDialogAsync(env, conn, ct);
            }
        }

        return await SendQuestRewardDialogAsync(env, conn, ct);
    }

    /// <summary>Java sendQuestNoneDialog(env, rangidax, itemId, itemCount) (4-arg overload): gives
    /// the starting item on accept before creating the entry.</summary>
    private async ValueTask<bool> SendQuestNoneDialogAsync(QuestEnv env, GsClientConnection conn, int itemId, long itemCount, CancellationToken ct)
    {
        var player = env.Player;
        if (env.DialogId == (int)DialogAction.QUEST_ACCEPT_1)
        {
            if (await GiveQuestItemAsync(player, conn, _itemDao, itemId, itemCount, ct))
                return await SendQuestStartDialogAsync(env, conn, ct);
            return true;
        }
        return await SendQuestStartDialogAsync(env, conn, ct);
    }

    /// <summary>Java sendQuestRewardDialog(env, rangidax, 2375): the turn-in fallback tried once
    /// every step check above fails to match.</summary>
    private async ValueTask<bool> SendQuestRewardDialogAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var entry = env.Player.Quests.Get(QuestId);
        if (entry is not { Status: QuestStatus.REWARD } || env.TargetId != RangidaxNpc) return false;

        int targetObjId = env.Target?.ObjectId ?? 0;
        if (DialogActionLookup.FromId(env.DialogId) == DialogAction.USE_OBJECT)
            return await SendQuestDialogAsync(conn, targetObjId, 2375, ct);
        return await SendQuestEndDialogAsync(env, conn, ct);
    }
}
