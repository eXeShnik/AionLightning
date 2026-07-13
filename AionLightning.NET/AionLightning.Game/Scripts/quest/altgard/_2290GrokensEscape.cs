// Port of Java data/scripts/system/handlers/quest/altgard/_2290GrokensEscape.java (Mr.Poke, reworked vlog).
// Accept from Groken (203608) via the simplified-accept leg, which starts the quest and immediately
// escorts Groken to the sailboat (700178) (var0 0->1, on reach var0 1->3 + movie 69, lost/logout rolls
// 1->0); talk Manir (203607) at var0 3 -> REWARD; turn in at Manir. Uses the follow/escort subsystem.
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

namespace Quest.Altgard;

public sealed class _2290GrokensEscape : QuestHandlerBase
{
    private const int QuestIdConst = 2290;
    private const int GrokenNpc    = 203608;
    private const int SailboatNpc  = 700178;
    private const int ManirNpc     = 203607;

    public _2290GrokensEscape(IDataManager dataManager, IQuestDao questDao, QuestRewardService rewardService, IItemDao itemDao)
        : base(QuestIdConst, dataManager, questDao, rewardService) { }

    public override void Register(QuestEngine engine)
    {
        engine.RegisterQuestNpc(GrokenNpc).OnQuestStart.Add(QuestId);
        RegisterOnLogOut(engine);
        engine.RegisterQuestNpc(GrokenNpc).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(SailboatNpc).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(ManirNpc).OnTalk.Add(QuestId);
        RegisterOnReachTarget(engine);
        RegisterOnLostTarget(engine);
    }

    public override async ValueTask<bool> OnDialogAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var player = env.Player;
        var entry  = player.Quests.Get(QuestId);

        int targetId    = env.TargetId;
        int targetObjId = env.Target?.ObjectId ?? 0;
        var dialog      = DialogActionLookup.FromId(env.DialogId);

        if (entry is null || entry.Status == QuestStatus.NONE)
        {
            if (targetId == GrokenNpc)
            {
                if (dialog == DialogAction.QUEST_SELECT) return await SendQuestDialogAsync(conn, targetObjId, 1011, ct);
                if (dialog == DialogAction.ASK_QUEST_ACCEPT) return await SendQuestDialogAsync(conn, targetObjId, 4, ct);
                if (dialog == DialogAction.QUEST_ACCEPT_1) return await SendQuestDialogAsync(conn, targetObjId, 1003, ct);
                if (dialog == DialogAction.QUEST_REFUSE_1) return await SendQuestDialogAsync(conn, targetObjId, 1004, ct);
                if (dialog == DialogAction.FINISH_DIALOG) return await SendQuestSelectionDialogAsync(conn, targetObjId, ct);
                if (dialog == DialogAction.SELECT_ACTION_1012)
                {
                    if (await StartMissionAsync(conn, player, QuestStatus.START, ct))
                    {
                        StartFollowToNpc(env, conn, (Npc)env.Target!, SailboatNpc);
                        return await DefaultCloseDialogAsync(env, conn, 0, 1, ct);
                    }
                    return false;
                }
                return await SendQuestStartDialogAsync(env, conn, ct);
            }
            return false;
        }

        if (entry.Status == QuestStatus.START)
        {
            if (targetId == GrokenNpc && dialog == DialogAction.QUEST_SELECT && entry.GetVar(0) == 0)
            {
                StartFollowToNpc(env, conn, (Npc)env.Target!, SailboatNpc);
                return await DefaultCloseDialogAsync(env, conn, 0, 1, ct);
            }
            if (targetId == ManirNpc)
            {
                if (dialog == DialogAction.QUEST_SELECT && entry.GetVar(0) == 3)
                    return await SendQuestDialogAsync(conn, targetObjId, 1693, ct);
                if (dialog == DialogAction.SELECT_QUEST_REWARD)
                    return await DefaultCloseDialogAsync(env, conn, 3, 3, reward: true, sameNpc: true, ct);
            }
            return false;
        }

        if (entry.Status == QuestStatus.REWARD && targetId == ManirNpc)
        {
            if (dialog == DialogAction.QUEST_SELECT) return await SendQuestDialogAsync(conn, targetObjId, 5, ct);
            return await SendQuestEndDialogAsync(env, conn, ct);
        }

        return false;
    }

    public override async ValueTask<bool> OnLogOutAsync(QuestEnv env, GsClientConnection? conn, CancellationToken ct)
    {
        var player = env.Player;
        var entry  = player.Quests.Get(QuestId);
        if (entry is null || entry.Status != QuestStatus.START) return false;
        if (entry.GetVar(0) == 1)
        {
            entry.SetVar(0, 0);
            if (conn is not null) await UpdateQuestStatusAsync(conn, entry, ct);
            else await QuestDao.UpsertAsync(player.ObjectId, entry, ct);
        }
        return false;
    }

    public override async ValueTask<bool> OnNpcReachTargetAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var entry = env.Player.Quests.Get(QuestId);
        if (entry is null || entry.Status != QuestStatus.START || entry.GetVar(0) != 1) return false;
        await ChangeQuestStepAsync(conn, entry, 0, 3, toReward: false, ct);
        await PlayQuestMovieAsync(conn, env.Player, 69, ct);
        return true;
    }

    public override async ValueTask<bool> OnNpcLostTargetAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var entry = env.Player.Quests.Get(QuestId);
        if (entry is null || entry.Status != QuestStatus.START || entry.GetVar(0) != 1) return false;
        await ChangeQuestStepAsync(conn, entry, 0, 0, toReward: false, ct);
        return true;
    }
}
