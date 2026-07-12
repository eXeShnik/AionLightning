// Port of Java data/scripts/system/handlers/quest/hero/_23500InspecttheKatalamBase.java (Alcapwnd).
// Elyos mirror of _13500ExaminetheKatalamBase: talk to the hub npc (800529), pick one of three
// branches via SETPRO1/2/3 (var 0 = 1/2/3, tracking item 182215271, briefing pages 1352/1693/2034),
// then turn in at whichever of the three follow-up NPCs (801239/801241/801244) matches. Note Java's
// changeQuestStep calls here use varIdx 1/2/3 (not 0 like the Asmodian sibling) while still reading
// var 0 for the reward-tier switch on turn-in — ported exactly as-is (each branch's changeQuestStep
// only ever touches its own dedicated var slot, so var 0 keeps the original branch selection intact,
// unlike _13500 where it's overwritten to the same value).
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

namespace Quest.Hero;

public sealed class _23500InspecttheKatalamBase : QuestHandlerBase
{
    private const int QuestIdConst = 23500;
    private const int HubNpc       = 800529;
    private const int FirstNpc     = 801239;
    private const int SecondNpc    = 801241;
    private const int ThirdNpc     = 801244;
    private const int TrackingItem = 182215271;

    private readonly IItemDao _itemDao;

    public _23500InspecttheKatalamBase(IDataManager dataManager, IQuestDao questDao, QuestRewardService rewardService, IItemDao itemDao)
        : base(QuestIdConst, dataManager, questDao, rewardService)
    {
        _itemDao = itemDao;
    }

    public override void Register(QuestEngine engine)
    {
        engine.RegisterQuestNpc(HubNpc).OnQuestStart.Add(QuestId);
        engine.RegisterQuestNpc(HubNpc).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(FirstNpc).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(SecondNpc).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(ThirdNpc).OnTalk.Add(QuestId);
    }

    public override async ValueTask<bool> OnDialogAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var player = env.Player;
        var entry = player.Quests.Get(QuestId);
        int targetId = env.TargetId;
        int targetObjId = env.Target?.ObjectId ?? 0;
        var dialog = DialogActionLookup.FromId(env.DialogId);

        if (targetId == HubNpc)
        {
            if (entry is null || entry.Status == QuestStatus.NONE)
            {
                if (dialog == DialogAction.QUEST_SELECT)
                    return await SendQuestDialogAsync(conn, targetObjId, 1011, ct);
                if (dialog == DialogAction.SELECT_ACTION_1013)
                    return await SendQuestDialogAsync(conn, targetObjId, 1013, ct);
                if (dialog == DialogAction.SETPRO1)
                {
                    await StartBranchAsync(conn, player, 1, ct);
                    await GiveQuestItemAsync(player, conn, _itemDao, TrackingItem, 1, ct);
                    return await SendQuestDialogAsync(conn, targetObjId, 1352, ct);
                }
                if (dialog == DialogAction.SETPRO2)
                {
                    await StartBranchAsync(conn, player, 2, ct);
                    await GiveQuestItemAsync(player, conn, _itemDao, TrackingItem, 1, ct);
                    return await SendQuestDialogAsync(conn, targetObjId, 1693, ct);
                }
                if (dialog == DialogAction.SETPRO3)
                {
                    await StartBranchAsync(conn, player, 3, ct);
                    await GiveQuestItemAsync(player, conn, _itemDao, TrackingItem, 1, ct);
                    return await SendQuestDialogAsync(conn, targetObjId, 2034, ct);
                }
                return await SendQuestStartDialogAsync(env, conn, ct);
            }
            return false;
        }

        if (entry is not null && entry.Status == QuestStatus.START)
        {
            if (targetId == FirstNpc)
            {
                if (dialog == DialogAction.QUEST_SELECT)
                    return await SendQuestDialogAsync(conn, targetObjId, 2375, ct);
                if (dialog == DialogAction.SELECT_QUEST_REWARD)
                {
                    await ChangeQuestStepAsync(conn, entry, 1, 1, toReward: true, ct);
                    return await SendQuestDialogAsync(conn, targetObjId, 5, ct);
                }
                return false;
            }
            if (targetId == SecondNpc)
            {
                if (dialog == DialogAction.QUEST_SELECT)
                    return await SendQuestDialogAsync(conn, targetObjId, 2716, ct);
                if (dialog == DialogAction.SELECT_QUEST_REWARD)
                {
                    await ChangeQuestStepAsync(conn, entry, 2, 2, toReward: true, ct);
                    return await SendQuestDialogAsync(conn, targetObjId, 6, ct);
                }
                return false;
            }
            if (targetId == ThirdNpc)
            {
                if (dialog == DialogAction.QUEST_SELECT)
                    return await SendQuestDialogAsync(conn, targetObjId, 3057, ct);
                if (dialog == DialogAction.SELECT_QUEST_REWARD)
                {
                    await ChangeQuestStepAsync(conn, entry, 3, 3, toReward: true, ct);
                    return await SendQuestDialogAsync(conn, targetObjId, 7, ct);
                }
                return false;
            }
            return false;
        }

        if (entry is not null && entry.Status == QuestStatus.REWARD)
        {
            if (targetId == FirstNpc || targetId == SecondNpc || targetId == ThirdNpc)
            {
                int rewardIndex = entry.GetVar(0) switch { 2 => 1, 3 => 2, _ => 0 };
                return await SendQuestEndDialogAsync(env, conn, rewardIndex, ct);
            }
        }
        return false;
    }

    /// <summary>Java's <c>QuestService.startQuest(env, QuestStatus.START, false, branchVar)</c> — creates
    /// the entry directly at START with quest var 0 already set to the chosen branch.</summary>
    private async ValueTask StartBranchAsync(GsClientConnection conn, Player player, int branchVar, CancellationToken ct)
    {
        var entry = new QuestEntry { QuestId = QuestId, Status = QuestStatus.START };
        entry.SetVar(0, branchVar);
        player.Quests.Add(entry);
        await QuestDao.UpsertAsync(player.ObjectId, entry, ct);
        await conn.SendAsync(new SM_QUEST_ACTION(entry.QuestId,
            SM_QUEST_ACTION.ActionType.Accept, (byte)entry.Status, entry.Step), ct);
        await conn.SendAsync(new SM_QUEST_LIST(player.Quests.Active), ct);
    }

    /// <summary>Java's explicit-reward-index <c>sendQuestEndDialog(env, reward)</c>: a numeric
    /// SELECTED_QUEST_REWARDx/SELECTED_QUEST_NOREWARD dialog id (8-23) grants that reward tier and
    /// completes; SELECT_QUEST_REWARD/USE_OBJECT instead just shows the tier-specific completion
    /// page (5 + rewardIndex).</summary>
    private async ValueTask<bool> SendQuestEndDialogAsync(QuestEnv env, GsClientConnection conn, int rewardIndex, CancellationToken ct)
    {
        var player = env.Player;
        var entry = player.Quests.Get(QuestId);
        int dialogId = env.DialogId;

        if (dialogId is >= 8 and <= 23)
        {
            if (entry is null || entry.Status != QuestStatus.REWARD) return false;
            return await FinishQuestAsync(conn, player, rewardIndex, ct);
        }
        if (dialogId == (int)DialogAction.SELECT_QUEST_REWARD || dialogId == (int)DialogAction.USE_OBJECT)
        {
            if (entry is not null && entry.Status == QuestStatus.REWARD)
                return await SendQuestDialogAsync(conn, env.Target?.ObjectId ?? 0, 5 + rewardIndex, ct);
        }
        return false;
    }
}
