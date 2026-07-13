// Port of Java data/scripts/system/handlers/quest/hidden_truth/_1099AnImportantChoice.java (vlog/Bobobear/apozema).
// Talk Pernos (790001, var0 0->1, hands over 182206066 + 182206067) -> use the Fissure of Destiny
// (700551) at var0==1 to enter instance world 310120000 -> talk Hermione (205119) SETPRO2 which (after
// a 43s flight) advances var0 1->2 -> kill 215396/215397/215398/215399 to count var0 2->52 (the 51st
// step spawns boss 215400) -> kill 215400 (var0 52->53) -> use the Artifact of Memory (700552) at
// var0==53 to play movie 429, spawn Lephar (205118) and consume 182206058 (var0 53->54) -> talk Lephar
// (SETPRO2/3) to flip to REWARD -> turn in at Fasimedes (203700). Dying or leaving world 310120000
// while var0>1 rolls var0 back to 1.
// New capability used: EnterInstanceAsync (getNextAvailableInstance + register + teleport) and
// RegisterOnDie.
// Skips vs Java: (1) Pernos SETPRO1's TeleportService2 relocation to 400010000 and Lephar's SETPRO2/3
// relocation to 110010000 are plain relocations — dropped with notes, state kept. (2) Hermione's
// flight-teleport visual (CreatureState.FLIGHT_TELEPORT + START_FLYTELEPORT emotion) is cosmetic —
// dropped; only the 43s-delayed var transition is kept (established precedent reshanta/_1075NewWings).
// (3) The QUEST_FAILED system message on death/world-leave is a cosmetic notice — dropped.
using System;
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

namespace Quest.HiddenTruth;

public sealed class _1099AnImportantChoice : QuestHandlerBase
{
    private const int QuestIdConst  = 1099;
    private const int PernosNpc     = 790001;
    private const int FissureNpc    = 700551;
    private const int HermioneNpc   = 205119;
    private const int ArtifactNpc   = 700552;
    private const int LepharNpc     = 205118;
    private const int FasimedesNpc  = 203700;
    private const int BossMob       = 215400;
    private const int ArtifactItem  = 182206058;
    private const int InstanceWorld = 310120000;

    private static readonly int[] _countMobs = [215396, 215397, 215398, 215399];

    private readonly IItemDao _itemDao;

    public _1099AnImportantChoice(IDataManager dataManager, IQuestDao questDao, QuestRewardService rewardService, IItemDao itemDao)
        : base(QuestIdConst, dataManager, questDao, rewardService)
    {
        _itemDao = itemDao;
    }

    public override void Register(QuestEngine engine)
    {
        engine.RegisterOnLevelUp(QuestId);
        RegisterOnDie(engine);
        engine.RegisterOnEnterWorld(QuestId);
        foreach (int npc in new[] { PernosNpc, FissureNpc, HermioneNpc, ArtifactNpc, LepharNpc, FasimedesNpc })
            engine.RegisterQuestNpc(npc).OnTalk.Add(QuestId);
        foreach (int mob in new[] { 215396, 215397, 215398, 215399, BossMob })
            engine.RegisterQuestNpc(mob).OnKill.Add(QuestId);
    }

    public override ValueTask<bool> OnLevelUpAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
        => DefaultOnLvlUpEventAsync(env, conn, precedingQuestId: 1098, ct);

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
            if (targetId == PernosNpc)
            {
                if (dialog == DialogAction.QUEST_SELECT && var == 0)
                    return await SendQuestDialogAsync(conn, targetObjId, 1011, ct);
                if (dialog == DialogAction.SETPRO1)
                {
                    if (!await GiveQuestItemAsync(player, conn, _itemDao, 182206066, 1, ct)
                        || !await GiveQuestItemAsync(player, conn, _itemDao, 182206067, 1, ct))
                        return false;
                    // note: TeleportService2 relocation to 400010000 (2247/2189/2191) dropped — plain relocation, state kept
                    return await DefaultCloseDialogAsync(env, conn, 0, 1, ct);
                }
                return false;
            }

            if (targetId == FissureNpc && dialog == DialogAction.USE_OBJECT && var == 1)
            {
                await EnterInstanceAsync(player, conn, InstanceWorld, 56.932575f, 178.72818f, 228.89743f, 7, ct);
                return true;
            }

            if (targetId == HermioneNpc)
            {
                if (dialog == DialogAction.QUEST_SELECT && var == 1)
                    return await SendQuestDialogAsync(conn, targetObjId, 1352, ct);
                if (dialog == DialogAction.SETPRO2 && var == 1)
                {
                    // note: flight-teleport visual (CreatureState.FLIGHT_TELEPORT + START_FLYTELEPORT emotion) dropped — cosmetic
                    ScheduleFlight(env, conn);
                    return true;
                }
                return false;
            }

            if (targetId == ArtifactNpc && dialog == DialogAction.USE_OBJECT && var == 53)
            {
                await PlayQuestMovieAsync(conn, player, 429, ct);
                SpawnQuestNpc(InstanceWorld, player.Position.InstanceId, LepharNpc, 302.19955f, 290.99936f, 207.37636f, 74);
                return await UseQuestObjectAsync(env, conn, 53, 54, reward: false, varNum: 0,
                    addItemId: 0, addItemCount: 0, removeItemId: ArtifactItem, removeItemCount: 1,
                    movieId: 0, dieObject: false, _itemDao, ct);
            }

            if (targetId == LepharNpc)
            {
                if (dialog == DialogAction.QUEST_SELECT && var == 54)
                    return await SendQuestDialogAsync(conn, targetObjId, 1352, ct);
                if (dialog == DialogAction.SETPRO2 || dialog == DialogAction.SETPRO3)
                {
                    // note: TeleportService2 relocation to 110010000 (1313/1512/568) dropped — plain relocation, state kept
                    return await DefaultCloseDialogAsync(env, conn, 54, 54, reward: true, sameNpc: false, ct);
                }
                return false;
            }

            return false;
        }

        if (entry.Status == QuestStatus.REWARD)
        {
            if (targetId == FasimedesNpc) return await SendQuestEndDialogAsync(env, conn, ct);
        }
        return false;
    }

    public override async ValueTask<bool> OnKillAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var player = env.Player;
        var entry  = player.Quests.Get(QuestId);
        if (entry is null || entry.Status != QuestStatus.START) return false;

        int var = entry.GetVar(0);
        if (var >= 2 && var < 52)
        {
            if (var == 51)
                SpawnQuestNpc(InstanceWorld, player.Position.InstanceId, BossMob, 240.0f, 257.0f, 208.53946f, 0);
            return await DefaultOnKillEventAsync(env, conn, _countMobs, 2, 52, ct);
        }
        if (var == 52)
            return await DefaultOnKillEventAsync(env, conn, BossMob, 52, 53, ct);
        return false;
    }

    public override async ValueTask<bool> OnDieAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var entry = env.Player.Quests.Get(QuestId);
        if (entry is null || entry.Status != QuestStatus.START) return false;
        if (entry.GetVar(0) > 1)
        {
            await ChangeQuestStepAsync(conn, entry, 0, 1, toReward: false, ct);
            // note: QUEST_FAILED system message dropped — cosmetic notice
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
        if (entry.GetVar(0) > 1)
        {
            await ChangeQuestStepAsync(conn, entry, 0, 1, toReward: false, ct);
            // note: QUEST_FAILED system message dropped — cosmetic notice
            return true;
        }
        return false;
    }

    /// <summary>Java Hermione SETPRO2: after the flight-teleport animation (43s), advance var0 1->2.</summary>
    private void ScheduleFlight(QuestEnv env, GsClientConnection conn)
    {
        var player = env.Player;
        _ = Task.Run(async () =>
        {
            await Task.Delay(TimeSpan.FromSeconds(43));
            try
            {
                var entry = player.Quests.Get(QuestId);
                if (entry is null || entry.Status != QuestStatus.START || entry.GetVar(0) != 1) return;
                await ChangeQuestStepAsync(conn, entry, 0, 2, toReward: false, CancellationToken.None);
            }
            catch { }
        });
    }
}
