// Port of Java data/scripts/system/handlers/quest/eltnen/_1040ScoutingtheScouts.java (Rhys2002).
// Zone-mission quest, part of the Kaidan Fortress chain (1036/1300): talk to Tumblusen (203989,
// var 0->1, movie 183); kill Kaidan Scouts (212010/212011, var 1->4 combined pool); report to
// Telemachus (203901, var 5->6); talk to Mabangtah (204020) and Targatu (204024, var 6->10, killing
// the Watchtower Guard 204046 at var 8->9 plays movie 36); turn in at Tumblusen.
// Skip vs Java: onDieEvent (reverts var 7-10 back to 6 on player death, a punitive safety net around
// the scouting window) has no OnDie hook in this port - omitted, harmless to completion (see
// migration_plan.md).
using System.Collections.Generic;
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

namespace Quest.Eltnen;

public sealed class _1040ScoutingtheScouts : QuestHandlerBase
{
    private const int QuestIdConst   = 1040;
    private const int TumblusenNpc   = 203989;
    private const int TelemachusNpc  = 203901;
    private const int MabangtahNpc   = 204020;
    private const int TargatuNpc     = 204024;
    private const int WatchtowerGuardNpc = 204046;

    private static readonly int[] _scoutMobs = [212010, 212011];

    public _1040ScoutingtheScouts(IDataManager dataManager, IQuestDao questDao, QuestRewardService rewardService, IItemDao itemDao)
        : base(QuestIdConst, dataManager, questDao, rewardService)
    {
    }

    public override void Register(QuestEngine engine)
    {
        engine.RegisterOnZoneMissionEnd(QuestId);
        engine.RegisterOnLevelUp(QuestId);
        foreach (int npc in new[] { TumblusenNpc, TelemachusNpc, MabangtahNpc, TargatuNpc })
            engine.RegisterQuestNpc(npc).OnTalk.Add(QuestId);
        foreach (int mob in new[] { _scoutMobs[0], _scoutMobs[1], WatchtowerGuardNpc })
            engine.RegisterQuestNpc(mob).OnKill.Add(QuestId);
    }

    public override ValueTask<bool> OnZoneMissionEndAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
        => DefaultOnZoneMissionEndEventAsync(env, conn, precedingQuestId: 1036, ct);

    public override ValueTask<bool> OnLevelUpAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
        => DefaultOnLvlUpEventAsync(env, conn, (IReadOnlyCollection<int>)[1300, 1036], isZoneMission: true, ct);

    public override async ValueTask<bool> OnKillAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        if (await DefaultOnKillEventAsync(env, conn, (IReadOnlyCollection<int>)_scoutMobs, 1, 4, ct))
            return true;
        if (await DefaultOnKillEventAsync(env, conn, WatchtowerGuardNpc, 8, 9, ct))
        {
            await PlayQuestMovieAsync(conn, env.Player, 36, ct);
            return true;
        }
        return false;
    }

    public override async ValueTask<bool> OnDialogAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var player = env.Player;
        var entry  = player.Quests.Get(QuestId);
        if (entry is null) return false;

        int targetId    = env.TargetId;
        int targetObjId = env.Target?.ObjectId ?? 0;
        var dialog      = DialogActionLookup.FromId(env.DialogId);

        if (entry.Status == QuestStatus.REWARD)
        {
            if (targetId != TumblusenNpc) return false;
            return await SendQuestEndDialogAsync(env, conn, ct);
        }
        if (entry.Status != QuestStatus.START) return false;

        int var = entry.GetVar(0);

        if (targetId == TumblusenNpc)
        {
            if (dialog == DialogAction.QUEST_SELECT)
            {
                if (var == 0) return await SendQuestDialogAsync(conn, targetObjId, 1011, ct);
                if (var == 4) return await SendQuestDialogAsync(conn, targetObjId, 1352, ct);
                return await DefaultCloseDialogAsync(env, conn, 0, 1, ct);
            }
            if (dialog == DialogAction.SELECT_ACTION_1013)
            {
                if (var == 0) await PlayQuestMovieAsync(conn, player, 183, ct);
                return await DefaultCloseDialogAsync(env, conn, 0, 1, ct);
            }
            if (dialog == DialogAction.SETPRO1) return await DefaultCloseDialogAsync(env, conn, 0, 1, ct);
            if (dialog == DialogAction.SETPRO2) return await DefaultCloseDialogAsync(env, conn, 4, 5, ct);
            return false;
        }

        if (targetId == TelemachusNpc)
        {
            if (dialog == DialogAction.QUEST_SELECT && var == 5) return await SendQuestDialogAsync(conn, targetObjId, 1693, ct);
            if (dialog == DialogAction.SETPRO3 && var == 5) return await DefaultCloseDialogAsync(env, conn, 5, 6, ct);
            return false;
        }

        if (targetId == MabangtahNpc)
        {
            if (dialog == DialogAction.USE_OBJECT && var == 7) return await SendQuestDialogAsync(conn, targetObjId, 2035, ct);
            if (dialog == DialogAction.QUEST_SELECT && var == 6) return await SendQuestDialogAsync(conn, targetObjId, 2034, ct);
            if (dialog == DialogAction.QUEST_SELECT && var == 10) return await SendQuestDialogAsync(conn, targetObjId, 3057, ct);
            if (dialog == DialogAction.SETPRO4 && (var == 6 || var == 7))
            {
                entry.SetVar(0, 7);
                await UpdateQuestStatusAsync(conn, entry, ct);
                return true;
            }
            if (dialog == DialogAction.SETPRO7)
                return await DefaultCloseDialogAsync(env, conn, 10, 10, reward: true, sameNpc: false, ct);
            return false;
        }

        if (targetId == TargatuNpc)
        {
            if (dialog == DialogAction.QUEST_SELECT && var == 7) return await SendQuestDialogAsync(conn, targetObjId, 2375, ct);
            if (dialog == DialogAction.QUEST_SELECT && var == 9) return await SendQuestDialogAsync(conn, targetObjId, 2716, ct);
            if (dialog == DialogAction.SETPRO5) return await DefaultCloseDialogAsync(env, conn, 7, 8, ct);
            if (dialog == DialogAction.SETPRO6 && var == 9)
            {
                entry.SetVar(0, 10);
                await UpdateQuestStatusAsync(conn, entry, ct);
                return true;
            }
            return false;
        }

        return false;
    }
}
