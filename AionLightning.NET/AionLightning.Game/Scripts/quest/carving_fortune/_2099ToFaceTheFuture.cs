// Port of Java data/scripts/system/handlers/quest/carving_fortune/_2099ToFaceTheFuture.java (Bobobear).
// Asmodian mirror of hidden_truth/_1099. Talk Munin (203550, var0 0->1, hands over 182207093 +
// 182207094) -> use the Fissure of Destiny (700551) at var0==1 to enter instance world 320140000 ->
// talk Hagen (205020) SETPRO2 which (after a 43s flight) advances var0 1->2 -> kill
// 798342/798343/798344/798345 to count var0 2->52 (the 51st step spawns boss 798346) -> kill 798346
// (var0 52->53) -> talk Lephar (205118) SETPRO2/3 to flip to REWARD -> turn in at Vidar (204052).
// Dying or leaving world 320140000 while var0>1 rolls var0 back to 1.
// New capability used: EnterInstanceAsync (getNextAvailableInstance + register + teleport) and
// RegisterOnDie.
// Skips vs Java: (1) Hagen's flight-teleport visual (CreatureState.FLIGHT_TELEPORT + START_FLYTELEPORT
// emotion) is cosmetic — dropped; only the 43s-delayed var transition is kept. (2) The QUEST_FAILED
// system message on death/world-leave is a cosmetic notice — dropped.
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

namespace Quest.CarvingFortune;

public sealed class _2099ToFaceTheFuture : QuestHandlerBase
{
    private const int QuestIdConst  = 2099;
    private const int MuninNpc      = 203550;
    private const int HagenNpc      = 205020;
    private const int LepharNpc     = 205118;
    private const int FissureNpc    = 700551;
    private const int VidarNpc      = 204052;
    private const int BossMob       = 798346;
    private const int InstanceWorld = 320140000;
    private const int ItemA         = 182207093;
    private const int ItemB         = 182207094;

    private static readonly int[] _countMobs = [798342, 798343, 798344, 798345];

    private readonly IItemDao _itemDao;

    public _2099ToFaceTheFuture(IDataManager dataManager, IQuestDao questDao, QuestRewardService rewardService, IItemDao itemDao)
        : base(QuestIdConst, dataManager, questDao, rewardService)
    {
        _itemDao = itemDao;
    }

    public override void Register(QuestEngine engine)
    {
        engine.RegisterOnLevelUp(QuestId);
        RegisterOnDie(engine);
        engine.RegisterOnEnterWorld(QuestId);
        foreach (int npc in new[] { MuninNpc, HagenNpc, LepharNpc, FissureNpc, VidarNpc })
            engine.RegisterQuestNpc(npc).OnTalk.Add(QuestId);
        foreach (int mob in new[] { 798342, 798343, 798344, 798345, BossMob })
            engine.RegisterQuestNpc(mob).OnKill.Add(QuestId);
    }

    public override ValueTask<bool> OnLevelUpAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
        => DefaultOnLvlUpEventAsync(env, conn, precedingQuestId: 2098, ct);

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
            if (targetId == MuninNpc)
            {
                if (dialog == DialogAction.QUEST_SELECT && var == 0)
                    return await SendQuestDialogAsync(conn, targetObjId, 1011, ct);
                if (dialog == DialogAction.SETPRO1)
                {
                    if (!await GiveQuestItemAsync(player, conn, _itemDao, ItemA, 1, ct)
                        || !await GiveQuestItemAsync(player, conn, _itemDao, ItemB, 1, ct))
                        return false;
                    return await DefaultCloseDialogAsync(env, conn, 0, 1, ct);
                }
                return false;
            }

            if (targetId == FissureNpc && dialog == DialogAction.USE_OBJECT && var == 1)
            {
                await EnterInstanceAsync(player, conn, InstanceWorld, 52f, 174f, 229f, 0, ct);
                return true;
            }

            if (targetId == HagenNpc)
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

            if (targetId == LepharNpc)
            {
                if (dialog == DialogAction.QUEST_SELECT && var == 53)
                    return await SendQuestDialogAsync(conn, targetObjId, 1352, ct);
                if (dialog == DialogAction.SETPRO2 || dialog == DialogAction.SETPRO3)
                    return await DefaultCloseDialogAsync(env, conn, 53, 53, reward: true, sameNpc: false, ct);
                return false;
            }

            return false;
        }

        if (entry.Status == QuestStatus.REWARD)
        {
            if (targetId == VidarNpc)
            {
                await RemoveQuestItemAsync(player, conn, _itemDao, ItemA, 1, ct);
                await RemoveQuestItemAsync(player, conn, _itemDao, ItemB, 1, ct);
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

        int var = entry.GetVar(0);
        if (var >= 2 && var < 52)
        {
            if (var == 51)
                SpawnQuestNpc(InstanceWorld, player.Position.InstanceId, BossMob, 240f, 257f, 208.53946f, 68);
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

    /// <summary>Java Hagen SETPRO2: after the flight-teleport animation (43s), advance var0 1->2.</summary>
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
