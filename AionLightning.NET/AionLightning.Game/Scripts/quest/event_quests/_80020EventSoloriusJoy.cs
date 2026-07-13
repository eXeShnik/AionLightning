// Port of Java data/scripts/system/handlers/quest/event_quests/_80020EventSoloriusJoy.java.
// Talk to portzebnerk (799769, gives item 182214012 on accept); report to squeegenerk (799768,
// gives 2x 182214013 and removes 182214012); "scare" jenel (203170, var 1, removes 1x 182214013);
// "scare" neltonia (203140, var 2, removes the last 182214013, flips to REWARD); turn in back at
// portzebnerk.
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

public sealed class _80020EventSoloriusJoy : QuestHandlerBase
{
    private const int QuestIdConst   = 80020;
    private const int PortzebnerkNpc = 799769;
    private const int SqueegenerkNpc = 799768;
    private const int JenelNpc       = 203170;
    private const int NeltoniaNpc    = 203140;
    private const int StartItem      = 182214012;
    private const int RelayItem      = 182214013;

    private readonly IItemDao _itemDao;

    public _80020EventSoloriusJoy(IDataManager dataManager, IQuestDao questDao, QuestRewardService rewardService, IItemDao itemDao)
        : base(QuestIdConst, dataManager, questDao, rewardService)
    {
        _itemDao = itemDao;
    }

    public override void Register(QuestEngine engine)
    {
        engine.RegisterOnLevelUp(QuestId);
        engine.RegisterQuestNpc(PortzebnerkNpc).OnQuestStart.Add(QuestId);
        foreach (int npc in new[] { PortzebnerkNpc, SqueegenerkNpc, JenelNpc, NeltoniaNpc })
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
            if (env.TargetId != PortzebnerkNpc) return false;
            if (dialog is DialogAction.USE_OBJECT or DialogAction.QUEST_SELECT)
                return await SendQuestDialogAsync(conn, targetObjId, 1011, ct);
            return await SendQuestNoneDialogAsync(env, conn, StartItem, 1, ct);
        }

        int var = entry.GetVar(0);

        if (entry.Status == QuestStatus.START)
        {
            if (env.TargetId == SqueegenerkNpc)
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

            if (env.TargetId == JenelNpc && var == 1)
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

            if (env.TargetId == NeltoniaNpc && var == 2)
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

    /// <summary>Java sendQuestNoneDialog(env, 799769, itemId, itemCount) (4-arg overload): gives the
    /// starting item on accept before creating the entry.</summary>
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

    /// <summary>Java sendQuestRewardDialog(env, portzebnerk, 2375): the turn-in fallback tried once
    /// every step check above fails to match.</summary>
    private async ValueTask<bool> SendQuestRewardDialogAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var entry = env.Player.Quests.Get(QuestId);
        if (entry is not { Status: QuestStatus.REWARD } || env.TargetId != PortzebnerkNpc) return false;

        int targetObjId = env.Target?.ObjectId ?? 0;
        if (DialogActionLookup.FromId(env.DialogId) == DialogAction.USE_OBJECT)
            return await SendQuestDialogAsync(conn, targetObjId, 2375, ct);
        return await SendQuestEndDialogAsync(env, conn, ct);
    }
}
