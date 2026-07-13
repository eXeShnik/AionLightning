// Port of Java data/scripts/system/handlers/quest/eltnen/_1043BalaurConspiracy.java (Balthazar).
// Talk Munawi (203901, var0 0->1) -> talk 204020 (var0 1->2, hands over 182201013) -> talk 204044
// (var0 2->3) which starts a 180s quest timer and spawns a random balaur; killing any spawned balaur
// while var0==3 spawns another; surviving until the timer ends advances var0 3->4; talk 204044 again
// (SETPRO4) flips to REWARD; turn in at 203901. Dying or logging out while var0==3 rolls back to var0==2.
// New capabilities used: RegisterOnDie / RegisterOnLogOut (player death & logout rollback) and the
// StartQuestTimer helper.
// Skips vs Java: (1) the three TeleportService2 relocations (Munawi/204020 setup teleports and the
// SETPRO4 turn-in teleport, all within the normal Eltnen world) are plain relocations — dropped with
// notes, state transitions kept. (2) The spawn's NPC-move-AI (setTarget 204044 + AIState.WALKING +
// moveToTargetObject + START_EMOTE2 broadcast) is cosmetic mob-approach movement, not core to the
// timed-survival progression — dropped (no NPC move-AI exposed to quest scripts). (3) The QUEST_FAILED
// system message on death/logout is a cosmetic notice — dropped (established precedent). The unused
// 700141 OnTalk registration is kept faithful to Java (no dialog case exists for it).
using System;
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

namespace Quest.Eltnen;

public sealed class _1043BalaurConspiracy : QuestHandlerBase
{
    private const int QuestIdConst = 1043;
    private const int MunawiNpc    = 203901;
    private const int GuideNpc     = 204020;
    private const int CommanderNpc = 204044;
    private const int UnusedNpc    = 700141;
    private const int QuestItem    = 182201013;
    private const int SpawnWorld   = 310040000;

    private static readonly int[] _mobs = [211628, 211630, 213575];
    private static readonly int[] _quests = [1300, 1031, 1032, 1033, 1034, 1036, 1037, 1035, 1038, 1039, 1040, 1041, 1042];

    private readonly IItemDao _itemDao;

    public _1043BalaurConspiracy(IDataManager dataManager, IQuestDao questDao, QuestRewardService rewardService, IItemDao itemDao)
        : base(QuestIdConst, dataManager, questDao, rewardService)
    {
        _itemDao = itemDao;
    }

    public override void Register(QuestEngine engine)
    {
        engine.RegisterOnZoneMissionEnd(QuestId);
        engine.RegisterOnLevelUp(QuestId);
        RegisterOnDie(engine);
        RegisterOnLogOut(engine);
        engine.RegisterOnQuestTimerEnd(QuestId);
        engine.RegisterQuestNpc(MunawiNpc).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(GuideNpc).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(CommanderNpc).OnTalk.Add(QuestId);
        foreach (int mob in _mobs)
            engine.RegisterQuestNpc(mob).OnKill.Add(QuestId);
        engine.RegisterQuestNpc(UnusedNpc).OnTalk.Add(QuestId);
    }

    public override ValueTask<bool> OnZoneMissionEndAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
        => DefaultOnZoneMissionEndEventAsync(env, conn, _quests, ct);

    public override ValueTask<bool> OnLevelUpAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
        => DefaultOnLvlUpEventAsync(env, conn, _quests, isZoneMission: true, ct);

    public override async ValueTask<bool> OnDialogAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var player      = env.Player;
        var entry       = player.Quests.Get(QuestId);
        if (entry is null) return false;

        int targetId    = env.TargetId;
        int targetObjId = env.Target?.ObjectId ?? 0;
        var dialog      = DialogActionLookup.FromId(env.DialogId);
        int var         = entry.GetVar(0);

        if (entry.Status == QuestStatus.START)
        {
            if (targetId == MunawiNpc)
            {
                if (dialog == DialogAction.QUEST_SELECT && var == 0)
                    return await SendQuestDialogAsync(conn, targetObjId, 1011, ct);
                if (dialog == DialogAction.SETPRO1)
                {
                    entry.SetVar(0, 1);
                    await UpdateQuestStatusAsync(conn, entry, ct);
                    // note: TeleportService2 relocation (Eltnen 1596/1529/317) dropped — plain relocation, state kept
                    return await SendQuestSelectionDialogAsync(conn, targetObjId, ct);
                }
                return false;
            }

            if (targetId == GuideNpc)
            {
                if (dialog == DialogAction.QUEST_SELECT && var == 1)
                    return await SendQuestDialogAsync(conn, targetObjId, 1352, ct);
                if (dialog == DialogAction.SETPRO2)
                {
                    entry.SetVar(0, 2);
                    await UpdateQuestStatusAsync(conn, entry, ct);
                    await GiveQuestItemAsync(player, conn, _itemDao, QuestItem, 1, ct);
                    // note: TeleportService2 relocation (Eltnen 2500/780/409) dropped — plain relocation, state kept
                    return await SendQuestSelectionDialogAsync(conn, targetObjId, ct);
                }
                return false;
            }

            if (targetId == CommanderNpc)
            {
                if (dialog == DialogAction.QUEST_SELECT)
                {
                    if (var == 2) return await SendQuestDialogAsync(conn, targetObjId, 1693, ct);
                    if (var == 4) return await SendQuestDialogAsync(conn, targetObjId, 2034, ct);
                    return false;
                }
                if (dialog == DialogAction.SETPRO3)
                {
                    entry.SetVar(0, 3);
                    await UpdateQuestStatusAsync(conn, entry, ct);
                    StartQuestTimer(env, conn, 180);
                    SpawnBalaur(player);
                    return await SendQuestSelectionDialogAsync(conn, targetObjId, ct);
                }
                if (dialog == DialogAction.SETPRO4)
                {
                    entry.Status = QuestStatus.REWARD;
                    await UpdateQuestStatusAsync(conn, entry, ct);
                    // note: TeleportService2 relocation (Eltnen 271/2787/272) dropped — plain relocation, state kept
                    return true;
                }
                return false;
            }

            return false;
        }

        if (entry.Status == QuestStatus.REWARD)
        {
            if (targetId != MunawiNpc) return false;
            if (dialog == DialogAction.QUEST_SELECT)
                return await SendQuestDialogAsync(conn, targetObjId, 2375, ct);
            if (dialog == DialogAction.SELECT_QUEST_REWARD)
            {
                await RemoveQuestItemAsync(player, conn, _itemDao, QuestItem, 1, ct);
                return await SendQuestEndDialogAsync(env, conn, ct);
            }
            return await SendQuestEndDialogAsync(env, conn, ct);
        }

        return false;
    }

    public override async ValueTask<bool> OnQuestTimerEndAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var entry = env.Player.Quests.Get(QuestId);
        if (entry is null || entry.Status != QuestStatus.START) return false;
        if (entry.GetVar(0) == 3)
        {
            entry.SetVar(0, 4);
            await UpdateQuestStatusAsync(conn, entry, ct);
            return true;
        }
        return false;
    }

    public override async ValueTask<bool> OnDieAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var entry = env.Player.Quests.Get(QuestId);
        if (entry is null || entry.Status != QuestStatus.START) return false;
        if (entry.GetVar(0) == 3)
        {
            entry.SetVar(0, 2);
            await UpdateQuestStatusAsync(conn, entry, ct);
            // note: QUEST_FAILED system message dropped — cosmetic notice
            return true;
        }
        return false;
    }

    public override async ValueTask<bool> OnLogOutAsync(QuestEnv env, GsClientConnection? conn, CancellationToken ct)
    {
        var player = env.Player;
        var entry  = player.Quests.Get(QuestId);
        if (entry is null || entry.Status != QuestStatus.START) return false;
        if (entry.GetVar(0) == 3)
        {
            entry.SetVar(0, 2);
            if (conn is not null)
                await UpdateQuestStatusAsync(conn, entry, ct);
            else
                await QuestDao.UpsertAsync(player.ObjectId, entry, ct); // logout: persist state without packets
            return true;
        }
        return false;
    }

    public override ValueTask<bool> OnKillAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var entry = env.Player.Quests.Get(QuestId);
        if (entry is null || entry.Status != QuestStatus.START) return ValueTask.FromResult(false);
        if (entry.GetVar(0) == 3 && System.Array.IndexOf(_mobs, env.TargetId) >= 0)
        {
            SpawnBalaur(env.Player);
            return ValueTask.FromResult(true);
        }
        return ValueTask.FromResult(false);
    }

    private void SpawnBalaur(Player player)
    {
        int mobToSpawn = _mobs[Random.Shared.Next(0, _mobs.Length)];
        float x = 0, y = 0;
        const float z = 217.48f;
        switch (mobToSpawn)
        {
            case 211628: x = 254.74f; y = 236.72f; break;
            case 211630: x = 257.92f; y = 237.39f; break;
            case 213575: x = 261.86f; y = 237.5f;  break;
        }
        SpawnQuestNpc(SpawnWorld, player.Position.InstanceId, mobToSpawn, x, y, z, 95);
        // note: Java NPC-move-AI (setTarget 204044 + AIState.WALKING + moveToTargetObject + START_EMOTE2 broadcast) dropped — cosmetic mob-approach, not core to timed survival
    }
}
