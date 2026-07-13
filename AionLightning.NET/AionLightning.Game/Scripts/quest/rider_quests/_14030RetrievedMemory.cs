// Port of Java data/scripts/system/handlers/quest/rider_quests/_14030RetrievedMemory.java (pralinka).
// Eltnen chain: Deakan (203700, var0 0->1), Pernos (790001, var0 1->2, later 3->4 handing item
// 182215387), kill 214578 (var0 2->3), USE Wind Serpent (700551, var0==4) to enter instance world
// 310120000, Kaidan (205119, var0==4) flight-teleports and after ~43s advances var0 4->5, kill the
// instance mob pack (215396-215400/205021/205022) counting var0 5->55, at var0==54 spawn 215400,
// final kill of 215400 (var0 55->56), then USE the exit object (700552, var0==56) to finish (var0
// 56->57 + REWARD). Turn in at Deakan. Dying or leaving 310120000 while var0 in (4,56) reverts to 4.
// Instance entry uses EnterInstanceAsync (Java getNextAvailableInstance triad); in-instance spawns
// use SpawnQuestNpc; player-death revert uses the new OnDieAsync hook.
// Skips vs Java: (1) Kaidan's flight visuals (SkillEngine effect 1910, FLIGHT_TELEPORT creature
// state, START_FLYTELEPORT emotion) dropped — no flight-teleport infra; the 43s flight delay and
// var0 4->5 transition are preserved via a scheduled task (same fire-and-forget pattern as
// QuestHandlerBase.StartQuestTimer). (2) Exit object's TeleportService2 relocation to Sanctum
// (110010000) dropped — non-entry relocation, the var0 56->57 + REWARD flip is kept. (3) The
// QUEST_FAILED_$1 system message on the die/leave reset is a cosmetic notice — dropped.
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

namespace Quest.RiderQuests;

public sealed class _14030RetrievedMemory : QuestHandlerBase
{
    private const int QuestIdConst = 14030;
    private const int DeakanNpc    = 203700;
    private const int PernosNpc    = 790001;
    private const int SerpentObj   = 700551;
    private const int KaidanNpc    = 205119;
    private const int ExitObj      = 700552;
    private const int GateMob      = 214578;
    private const int FinalMob     = 215400;
    private const int ProtectItem  = 182215387;
    private const int InstanceWorld = 310120000;

    private static readonly int[] _packMobs = [215396, 215397, 215398, 215399, 215400, 205021, 205022];

    private readonly IItemDao _itemDao;

    public _14030RetrievedMemory(IDataManager dataManager, IQuestDao questDao, QuestRewardService rewardService, IItemDao itemDao)
        : base(QuestIdConst, dataManager, questDao, rewardService)
    {
        _itemDao = itemDao;
    }

    public override void Register(QuestEngine engine)
    {
        engine.RegisterOnLevelUp(QuestId);
        RegisterOnDie(engine);
        engine.RegisterOnEnterWorld(QuestId);
        foreach (int npc in new[] { PernosNpc, SerpentObj, KaidanNpc, ExitObj, DeakanNpc })
            engine.RegisterQuestNpc(npc).OnTalk.Add(QuestId);
        foreach (int mob in new[] { GateMob, 215396, 215397, 215398, 215399, 215400, 205021, 205022 })
            engine.RegisterQuestNpc(mob).OnKill.Add(QuestId);
    }

    public override ValueTask<bool> OnLevelUpAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
        => DefaultOnLvlUpEventAsync(env, conn, ct);

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
            if (targetId == DeakanNpc)
            {
                if (dialog == DialogAction.QUEST_SELECT && var == 0) return await SendQuestDialogAsync(conn, targetObjId, 1011, ct);
                if (dialog == DialogAction.SETPRO1) return await DefaultCloseDialogAsync(env, conn, 0, 1, ct);
                return false;
            }
            if (targetId == PernosNpc)
            {
                if (dialog == DialogAction.QUEST_SELECT)
                {
                    if (var == 1) return await SendQuestDialogAsync(conn, targetObjId, 1352, ct);
                    if (var == 3) return await SendQuestDialogAsync(conn, targetObjId, 2034, ct);
                    return false;
                }
                if (dialog == DialogAction.SETPRO2) return await DefaultCloseDialogAsync(env, conn, 1, 2, ct);
                if (dialog == DialogAction.SETPRO4)
                {
                    await GiveQuestItemAsync(player, conn, _itemDao, ProtectItem, 1, ct);
                    return await DefaultCloseDialogAsync(env, conn, 3, 4, ct);
                }
                return false;
            }
            if (targetId == SerpentObj)
            {
                if (dialog == DialogAction.USE_OBJECT && var == 4)
                    return await EnterInstanceAsync(player, conn, InstanceWorld, 52f, 174f, 229f, 0, ct);
                return false;
            }
            if (targetId == KaidanNpc)
            {
                if (dialog == DialogAction.QUEST_SELECT && var == 4) return await SendQuestDialogAsync(conn, targetObjId, 2375, ct);
                if (dialog == DialogAction.SETPRO5 && var == 4)
                {
                    await RemoveQuestItemAsync(player, conn, _itemDao, ProtectItem, 1, ct);
                    // note: flight visuals (SkillEngine effect 1910, FLIGHT_TELEPORT state, START_FLYTELEPORT emotion) dropped — no flight-teleport infra; 43s delay + var0 4->5 preserved
                    var scheduled = entry;
                    _ = Task.Run(async () =>
                    {
                        await Task.Delay(43000);
                        try { await ChangeQuestStepAsync(conn, scheduled, 0, 5, toReward: false, CancellationToken.None); } catch { }
                    });
                    return true;
                }
                return false;
            }
            if (targetId == ExitObj)
            {
                if (dialog == DialogAction.USE_OBJECT && var == 56)
                {
                    // note: TeleportService2 relocation to Sanctum (110010000) dropped — non-entry relocation; var0 56->57 + REWARD kept
                    return await UseQuestObjectAsync(env, conn, 56, 57, reward: true, dieObject: false, ct);
                }
                return false;
            }
        }
        else if (entry.Status == QuestStatus.REWARD)
        {
            if (targetId == DeakanNpc) return await SendQuestEndDialogAsync(env, conn, ct);
        }
        return false;
    }

    public override async ValueTask<bool> OnKillAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var player = env.Player;
        var entry  = player.Quests.Get(QuestId);
        if (entry is null || entry.Status != QuestStatus.START) return false;

        int var = entry.GetVar(0);
        if (var == 2)
            return await DefaultOnKillEventAsync(env, conn, GateMob, 2, 3, ct);
        if (var >= 5 && var < 55)
        {
            if (var == 54)
                SpawnQuestNpc(InstanceWorld, player.Position.InstanceId, FinalMob, 240f, 257f, 208.53946f, 68);
            return await DefaultOnKillEventAsync(env, conn, _packMobs, 5, 55, ct);
        }
        if (var == 55)
            return await DefaultOnKillEventAsync(env, conn, FinalMob, 55, 56, ct);
        return false;
    }

    public override async ValueTask<bool> OnDieAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var entry = env.Player.Quests.Get(QuestId);
        if (entry is null || entry.Status != QuestStatus.START) return false;

        int var = entry.GetVar(0);
        if (var > 4 && var < 56)
        {
            // note: QUEST_FAILED_$1 system message dropped — cosmetic notice
            await ChangeQuestStepAsync(conn, entry, 0, 4, toReward: false, ct);
            return true;
        }
        return false;
    }

    public override async ValueTask<bool> OnEnterWorldAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var player = env.Player;
        var entry  = player.Quests.Get(QuestId);
        if (player.Position.WorldId == InstanceWorld) return false;
        if (entry is null || entry.Status != QuestStatus.START) return false;

        int var = entry.GetVar(0);
        if (var > 4 && var < 56)
        {
            // note: QUEST_FAILED_$1 system message dropped — cosmetic notice
            await ChangeQuestStepAsync(conn, entry, 0, 4, toReward: false, ct);
            return true;
        }
        return false;
    }
}
