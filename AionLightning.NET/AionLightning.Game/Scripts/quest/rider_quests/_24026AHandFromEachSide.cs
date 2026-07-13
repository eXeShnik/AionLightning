// Port of Java data/scripts/system/handlers/quest/rider_quests/_24026AHandFromEachSide.java (pralinka).
// Morheim zone-mission (level-up gated on 24021-24025): Vidar (204301, var0 0->1 giving item
// 182215371), Kimeia (204403, var0 1->2 giving 182215372), Kargate (204432, var0==2): SETPRO3 sets
// var0 3, opens the selection dialog, starts a 180s quest timer and spawns a defender wave in
// instance world 320040000; each kill of a wave mob (280818/211624/213578/213579) spawns another;
// timer expiry advances var0 3->4; Kargate SETPRO4 (var0==4) removes 182215371 and flips to REWARD.
// Turn in at Vidar. Dying or logging out at var0==3 cancels the wave and reverts to var0 2.
// In-instance spawns use SpawnQuestNpc; player-death revert uses OnDieAsync; logout revert uses
// OnLogOutAsync (persists without packets); the quest timer uses StartQuestTimer.
// Skips vs Java: (1) the spawned mob's AI targeting/approach (setTarget(204432) + WALKING state +
// moveToTargetObject + START_EMOTE2) is dropped — SpawnQuestNpc doesn't return the Npc reference and
// there is no scriptable NPC-AI control (same precedent as morheim._2041HoldTheFrontLine). (2) the
// three TeleportService2 relocations (SETPRO1/SETPRO2 within Morheim, SETPRO4 to Morheim main) are
// non-entry relocations — dropped, the var/status transitions are kept. (3) QuestService.questTimerEnd
// (cancel-on-death) has no equivalent; the fire-and-forget timer still no-ops once var0 != 3.
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

namespace Quest.RiderQuests;

public sealed class _24026AHandFromEachSide : QuestHandlerBase
{
    private const int QuestIdConst = 24026;
    private const int VidarNpc     = 204301;
    private const int KimeiaNpc    = 204403;
    private const int KargateNpc   = 204432;
    private const int Item371      = 182215371;
    private const int Item372      = 182215372;
    private const int SpawnWorld   = 320040000;
    private const int TimerSeconds = 180;

    // Rnd.get(0, 2) picks index 0..2 inclusive — 213579 (index 3) is never selected, matching Java.
    private static readonly int[] _mobs = [280818, 211624, 213578, 213579];
    private static readonly int[] _rewardQuests = [24021, 24022, 24023, 24024, 24025];

    private readonly IItemDao _itemDao;

    public _24026AHandFromEachSide(IDataManager dataManager, IQuestDao questDao, QuestRewardService rewardService, IItemDao itemDao)
        : base(QuestIdConst, dataManager, questDao, rewardService)
    {
        _itemDao = itemDao;
    }

    public override void Register(QuestEngine engine)
    {
        engine.RegisterOnZoneMissionEnd(QuestId);
        engine.RegisterOnLevelUp(QuestId);
        foreach (int mob in _mobs) engine.RegisterQuestNpc(mob).OnKill.Add(QuestId);
        engine.RegisterOnQuestTimerEnd(QuestId);
        RegisterOnDie(engine);
        RegisterOnLogOut(engine);
        engine.RegisterQuestNpc(VidarNpc).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(KimeiaNpc).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(KargateNpc).OnTalk.Add(QuestId);
    }

    public override ValueTask<bool> OnZoneMissionEndAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
        => DefaultOnZoneMissionEndEventAsync(env, conn, _rewardQuests, ct);

    public override ValueTask<bool> OnLevelUpAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
        => DefaultOnLvlUpEventAsync(env, conn, _rewardQuests, isZoneMission: true, ct);

    private void Spawn(Player player)
    {
        int mobToSpawn = _mobs[Random.Shared.Next(0, 3)];
        float x = 0, y = 0;
        const float z = 217.48f;
        switch (mobToSpawn)
        {
            case 280818: x = 254.74f; y = 236.72f; break;
            case 211624: x = 257.92f; y = 237.39f; break;
            case 213578: x = 261.86f; y = 237.5f;  break;
            case 213579: x = 268.86f; y = 243.5f;  break;
        }
        SpawnQuestNpc(SpawnWorld, player.Position.InstanceId, mobToSpawn, x, y, z, 95);
        // note: spawned mob's AI targeting/approach (setTarget/WALKING/moveToTargetObject/START_EMOTE2) dropped — no scriptable NPC-AI control
    }

    public override async ValueTask<bool> OnDialogAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var player = env.Player;
        var entry  = player.Quests.Get(QuestId);
        if (entry is null) return false;

        int targetId    = env.TargetId;
        int targetObjId = env.Target?.ObjectId ?? 0;
        var dialog      = DialogActionLookup.FromId(env.DialogId);
        int var         = entry.GetVar(0);

        if (entry.Status == QuestStatus.START)
        {
            if (targetId == VidarNpc)
            {
                if (dialog == DialogAction.QUEST_SELECT && var == 0) return await SendQuestDialogAsync(conn, targetObjId, 1011, ct);
                if (dialog == DialogAction.SETPRO1)
                {
                    await GiveQuestItemAsync(player, conn, _itemDao, Item371, 1, ct);
                    // note: TeleportService2 relocation within Morheim (220020000) dropped — non-entry relocation
                    return await DefaultCloseDialogAsync(env, conn, 0, 1, ct);
                }
                return false;
            }
            if (targetId == KimeiaNpc)
            {
                if (dialog == DialogAction.QUEST_SELECT && var == 1) return await SendQuestDialogAsync(conn, targetObjId, 1352, ct);
                if (dialog == DialogAction.SETPRO2)
                {
                    await GiveQuestItemAsync(player, conn, _itemDao, Item372, 1, ct);
                    // note: TeleportService2 relocation within Morheim (220020000) dropped — non-entry relocation
                    return await DefaultCloseDialogAsync(env, conn, 1, 2, ct);
                }
                return false;
            }
            if (targetId == KargateNpc)
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
                    await SendQuestSelectionDialogAsync(conn, targetObjId, ct);
                    StartQuestTimer(env, conn, TimerSeconds);
                    Spawn(player);
                    return true;
                }
                if (dialog == DialogAction.SETPRO4)
                {
                    await RemoveQuestItemAsync(player, conn, _itemDao, Item371, 1, ct);
                    entry.Status = QuestStatus.REWARD;
                    await UpdateQuestStatusAsync(conn, entry, ct);
                    // note: TeleportService2 relocation to Morheim main (220020000) dropped — non-entry relocation
                    return true;
                }
                return false;
            }
        }
        else if (entry.Status == QuestStatus.REWARD)
        {
            if (targetId == VidarNpc)
            {
                if (dialog == DialogAction.USE_OBJECT) return await SendQuestDialogAsync(conn, targetObjId, 2375, ct);
                return await SendQuestEndDialogAsync(env, conn, ct);
            }
        }
        return false;
    }

    public override async ValueTask<bool> OnKillAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var player = env.Player;
        var entry  = player.Quests.Get(QuestId);
        if (entry is null || entry.Status != QuestStatus.START) return false;
        if (entry.GetVar(0) == 3 && Array.IndexOf(_mobs, env.TargetId) >= 0)
        {
            Spawn(player);
            return true;
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
            // note: QuestService.questTimerEnd cancel dropped — fire-and-forget timer no-ops once var0 != 3
            entry.SetVar(0, 2);
            await UpdateQuestStatusAsync(conn, entry, ct);
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
            await QuestDao.UpsertAsync(player.ObjectId, entry, ct); // logout hook: mutate + persist only, no packets
            return true;
        }
        return false;
    }
}
