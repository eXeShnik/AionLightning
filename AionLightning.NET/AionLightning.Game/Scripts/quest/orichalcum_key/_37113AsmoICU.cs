// Port of Java data/scripts/system/handlers/quest/orichalcum_key/_37113AsmoICU.java (Cheatkiller).
// Accepted directly (targetId 0, QUEST_ACCEPT_1) - no NPC quest-offer dialog. Kill AsmoICU mob
// (217172) five times (var 0 -> 5); turn in at AsmoICU (799906): var == 5 shows page 1352,
// SELECT_QUEST_REWARD flips to REWARD via the same npc.
// Skip vs Java: talking to 700969 while grouped with an active mentor (player.isInGroup2() +
// member.isMentor() within GroupConfig.GROUP_MAX_DISTANCE) despawns 700969 (scheduleRespawn +
// onDelete - no NPC controller/respawn-scheduling infra in this port either, see e.g.
// _4077PorgusRoundup/_30455JurdinsRelease) and spawns 217172 at its spot for the mentee to kill; the
// STR_MSG_DailyQuest_Ask_Mentee notice to non-mentor members is likewise not portable - no
// mentor/mentee or Group2 model exists. Matching the documented approach already taken by
// QuestEngine.Handlers.Templates.MentorMonsterHuntHandler (grant the mentor-gated effect
// unconditionally rather than silently no-op the whole branch), talking to 700969 always spawns the
// killable 217172 here.
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

namespace Quest.OrichalcumKey;

public sealed class _37113AsmoICU : QuestHandlerBase
{
    private const int QuestIdConst = 37113;
    private const int TalkNpc      = 700969;
    private const int TurnInNpc    = 799906;
    private const int MobNpc       = 217172;

    public _37113AsmoICU(IDataManager dataManager, IQuestDao questDao, QuestRewardService rewardService, IItemDao itemDao)
        : base(QuestIdConst, dataManager, questDao, rewardService)
    {
    }

    public override void Register(QuestEngine engine)
    {
        engine.RegisterQuestNpc(TalkNpc).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(TurnInNpc).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(MobNpc).OnKill.Add(QuestId);
    }

    public override ValueTask<bool> OnKillAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
        => DefaultOnKillEventAsync(env, conn, MobNpc, startVar: 0, endVar: 5, ct);

    public override async ValueTask<bool> OnDialogAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var player      = env.Player;
        var entry       = player.Quests.Get(QuestId);
        int targetId    = env.TargetId;
        int targetObjId = env.Target?.ObjectId ?? 0;
        var dialog      = DialogActionLookup.FromId(env.DialogId);

        if (entry is null && targetId == 0 && dialog == DialogAction.QUEST_ACCEPT_1)
        {
            await StartMissionAsync(conn, player, QuestStatus.START, ct);
            return await CloseDialogWindowAsync(conn, 0, ct);
        }

        if (entry is not null && entry.Status == QuestStatus.START)
        {
            if (targetId == TalkNpc && env.Target is not null)
            {
                // Mentor-group gate not portable - see header; the transform is granted unconditionally.
                var pos = env.Target.Position;
                SpawnQuestNpc(pos.WorldId, pos.InstanceId, MobNpc, pos.X, pos.Y, pos.Z, (byte)pos.Heading);
                return true;
            }
            if (targetId == TurnInNpc)
            {
                if (dialog == DialogAction.QUEST_SELECT && entry.GetVar(0) == 5)
                    return await SendQuestDialogAsync(conn, targetObjId, 1352, ct);
                if (dialog == DialogAction.SELECT_QUEST_REWARD)
                    return await DefaultCloseDialogAsync(env, conn, 5, 5, reward: true, sameNpc: true, ct);
            }
        }
        else if (entry is not null && entry.Status == QuestStatus.REWARD)
        {
            if (targetId == TurnInNpc)
            {
                if (dialog == DialogAction.USE_OBJECT)
                    return await SendQuestDialogAsync(conn, targetObjId, 5, ct);
                return await SendQuestEndDialogAsync(env, conn, ct);
            }
        }

        return false;
    }
}
