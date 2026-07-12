// Port of Java data/scripts/system/handlers/quest/elementis_forest/_30455JurdinsRelease.java (Ritsu).
// Talk to 205492 to start; at 282203, SETPRO1 despawns it and spawns mob 217249 at its position
// (var 0->1); killing 217249 (var1->2) spawns npc 282204 at the kill spot; talking to 282204
// (SET_SUCCEED) flips straight to REWARD; turn in at 205492. Identical structure to
// _30405Cursebreakers - same npcs/mob, a different quest_data.xml reward set (Jurdin himself).
// Skip vs Java: despawning 282203 via its AI controller (scheduleRespawn/onDelete) has no
// equivalent (no NPC controller subsystem yet) - harmless, the old npc simply persists alongside
// the new spawn.
// Java bug fixed: the QUEST_SELECT cases at 282203 and 282204 fell through (missing break) into the
// SETPRO1/SET_SUCCEED bodies whenever var didn't match, which would unconditionally re-run the
// mob-spawn side effect on a stale QUEST_SELECT click; written here without the fallthrough.
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

namespace Quest.ElementisForest;

public sealed class _30455JurdinsRelease : QuestHandlerBase
{
    private const int QuestIdConst = 30455;
    private const int StartNpc     = 205492;
    private const int CursedNpc    = 282203;
    private const int FreedNpc     = 282204;
    private const int MobNpc       = 217249;

    public _30455JurdinsRelease(IDataManager dataManager, IQuestDao questDao, QuestRewardService rewardService, IItemDao itemDao)
        : base(QuestIdConst, dataManager, questDao, rewardService)
    {
    }

    public override void Register(QuestEngine engine)
    {
        engine.RegisterQuestNpc(StartNpc).OnQuestStart.Add(QuestId);
        engine.RegisterQuestNpc(StartNpc).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(CursedNpc).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(FreedNpc).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(MobNpc).OnKill.Add(QuestId);
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
            if (targetId != StartNpc) return false;
            if (dialog == DialogAction.QUEST_SELECT)
                return await SendQuestDialogAsync(conn, targetObjId, 4762, ct);
            return await SendQuestStartDialogAsync(env, conn, ct);
        }

        if (entry.Status == QuestStatus.START)
        {
            int var = entry.GetVar(0);

            if (targetId == CursedNpc)
            {
                switch (dialog)
                {
                    case DialogAction.QUEST_SELECT:
                        return var == 0 && await SendQuestDialogAsync(conn, targetObjId, 1011, ct);
                    case DialogAction.SETPRO1:
                        if (var != 0 || env.Target is null) return false;
                        var pos = env.Target.Position;
                        // Java despawns 282203 here (scheduleRespawn + onDelete) - skipped, see header.
                        SpawnQuestNpc(pos.WorldId, pos.InstanceId, MobNpc, pos.X, pos.Y, pos.Z, (byte)pos.Heading);
                        return await DefaultCloseDialogAsync(env, conn, 0, 1, ct);
                    default:
                        return false;
                }
            }

            if (targetId == FreedNpc)
            {
                switch (dialog)
                {
                    case DialogAction.QUEST_SELECT:
                        return var == 2 && await SendQuestDialogAsync(conn, targetObjId, 1693, ct);
                    case DialogAction.SET_SUCCEED:
                        entry.Status = QuestStatus.REWARD;
                        await UpdateQuestStatusAsync(conn, entry, ct);
                        return await CloseDialogWindowAsync(conn, targetObjId, ct);
                    default:
                        return false;
                }
            }
            return false;
        }

        if (entry.Status == QuestStatus.REWARD && targetId == StartNpc)
        {
            if (dialog == DialogAction.USE_OBJECT)
                return await SendQuestDialogAsync(conn, targetObjId, 10002, ct);
            return await SendQuestEndDialogAsync(env, conn, ct);
        }
        return false;
    }

    public override async ValueTask<bool> OnKillAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var entry = env.Player.Quests.Get(QuestId);
        if (entry is null || entry.Status != QuestStatus.START) return false;
        if (env.TargetId != MobNpc || entry.GetVar(0) != 1 || env.Target is null) return false;

        var pos = env.Target.Position;
        entry.SetVar(0, entry.GetVar(0) + 1);
        await UpdateQuestStatusAsync(conn, entry, ct);
        SpawnQuestNpc(pos.WorldId, pos.InstanceId, FreedNpc, pos.X, pos.Y, pos.Z, (byte)pos.Heading);
        return false; // Java's onKillEvent always returns false here too
    }
}
