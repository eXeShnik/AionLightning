// Port of Java data/scripts/system/handlers/quest/the_circle/_47103AGlobeTrottingLesson.java
// (Cheatkiller). Board-accept daily (targetId 0): talking to 700971 spawns the hunt target 217173
// in place, kill 5 of them (defaultOnKillEvent 0->5), turn in at 799921.
// Skip vs Java: the mentor/mentee gate on the 700971 branch (isInGroup2/isMentor/GROUP_MAX_DISTANCE)
// has no equivalent in this port's Player/PlayerGroup model (see MentorMonsterHuntHandler's
// documented limitation) - dropped, so SpawnQuestNpc fires unconditionally on talk instead of only
// for a mentor within range of a mentee. Also skipped: Java deletes/respawn-schedules the original
// 700971 placeholder via its Npc controller (npc.getController().scheduleRespawn()/onDelete()) -
// this port's quest-script layer has no handle on NPC controller lifecycle, so the placeholder is
// left in place (spawning 217173 alongside it) rather than being swapped out.
using System.Threading;
using System.Threading.Tasks;
using AionLightning.Game.Dao;
using AionLightning.Game.DataHolders;
using AionLightning.Game.Model.Quest;
using AionLightning.Game.Network.Aion;
using AionLightning.Game.Network.Aion.ServerPackets;
using AionLightning.Game.QuestEngine;
using AionLightning.Game.QuestEngine.Handlers;
using AionLightning.Game.QuestEngine.Model;
using AionLightning.Game.Services;

namespace Quest.TheCircle;

public sealed class _47103AGlobeTrottingLesson : QuestHandlerBase
{
    private const int QuestIdConst = 47103;
    private const int StartNpc     = 700971;
    private const int TurnInNpc    = 799921;
    private const int HuntNpc      = 217173;

    public _47103AGlobeTrottingLesson(IDataManager dataManager, IQuestDao questDao, QuestRewardService rewardService, IItemDao itemDao)
        : base(QuestIdConst, dataManager, questDao, rewardService)
    {
    }

    public override void Register(QuestEngine engine)
    {
        engine.RegisterQuestNpc(StartNpc).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(TurnInNpc).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(HuntNpc).OnKill.Add(QuestId);
    }

    public override ValueTask<bool> OnKillAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
        => DefaultOnKillEventAsync(env, conn, HuntNpc, 0, 5, ct);

    public override async ValueTask<bool> OnDialogAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var player      = env.Player;
        var entry       = player.Quests.Get(QuestId);
        int targetId    = env.TargetId;
        int targetObjId = env.Target?.ObjectId ?? 0;
        var dialog      = DialogActionLookup.FromId(env.DialogId);

        if (entry is null || entry.Status == QuestStatus.NONE)
        {
            if (targetId == 0 && env.DialogId == (int)DialogAction.QUEST_ACCEPT_1)
            {
                await StartMissionAsync(conn, player, QuestStatus.START, ct);
                await conn.SendAsync(new SM_DIALOG_WINDOW(0, 0), ct);
                return true;
            }
            return false;
        }

        if (entry.Status == QuestStatus.START)
        {
            if (targetId == StartNpc && env.Target is not null)
            {
                var pos = env.Target.Position;
                SpawnQuestNpc(pos.WorldId, pos.InstanceId, HuntNpc, pos.X, pos.Y, pos.Z, (byte)pos.Heading);
                return true;
            }

            if (targetId != TurnInNpc) return false;
            if (dialog == DialogAction.QUEST_SELECT && entry.GetVar(0) == 5)
                return await SendQuestDialogAsync(conn, targetObjId, 1352, ct);
            if (dialog == DialogAction.SELECT_QUEST_REWARD)
                return await DefaultCloseDialogAsync(env, conn, 5, 5, reward: true, sameNpc: true, ct);
            return false;
        }

        if (entry.Status == QuestStatus.REWARD)
        {
            if (targetId != TurnInNpc) return false;
            if (dialog == DialogAction.USE_OBJECT)
                return await SendQuestDialogAsync(conn, targetObjId, 5, ct);
            return await SendQuestEndDialogAsync(env, conn, ct);
        }

        return false;
    }
}
