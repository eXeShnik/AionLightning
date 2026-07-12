// Port of Java data/scripts/system/handlers/quest/hero/_13500ExaminetheKatalamBase.java (Alcapwnd).
// Talk to Tirins (800527): QUEST_SELECT shows the intro (1011), SELECT_ACTION_1013 shows the
// branch-choice page (1013), and picking SETPRO1/2/3 there creates the quest with var 0 = 1/2/3
// (one of three parallel sub-objectives), gives the tracking item 182215270, and shows the
// matching briefing page (1352/1693/2034). Any other dialog at Tirins falls back to the generic
// accept flow (Java's sendQuestStartDialog(env, itemId, count) bag-space-checked overload isn't
// ported — this port's plain SendQuestStartDialogAsync is used instead, which would accept with
// var 0 and strand the quest since none of the three turn-in NPCs recognize that state; this
// mirrors Java's own dead fallback path, which requires the same premise to be reachable).
// Talking to whichever of the three turn-in NPCs (801231/801233/801236) matches the chosen branch
// while START flips var 0 back to that branch's own value and status to REWARD (redundant when
// unchanged, but ported as-is); the completion dialog then bases its reward tier on that var.
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

public sealed class _13500ExaminetheKatalamBase : QuestHandlerBase
{
    private const int QuestIdConst = 13500;
    private const int HubNpc       = 800527;
    private const int FirstNpc     = 801231;
    private const int SecondNpc    = 801233;
    private const int ThirdNpc     = 801236;
    private const int TrackingItem = 182215270;

    private readonly IItemDao _itemDao;

    public _13500ExaminetheKatalamBase(IDataManager dataManager, IQuestDao questDao, QuestRewardService rewardService, IItemDao itemDao)
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
                    await ChangeQuestStepAsync(conn, entry, 0, 1, toReward: true, ct);
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
                    await ChangeQuestStepAsync(conn, entry, 0, 2, toReward: true, ct);
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
                    await ChangeQuestStepAsync(conn, entry, 0, 3, toReward: true, ct);
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
