// Port of Java data/scripts/system/handlers/quest/verteron/_1019FlyingReconnaissance.java
// (Mr. Poke, Rice, reworked vlog). Talk to Estino (203146), use a transformation potion inside the
// Tursin Outpost, talk to Spatalos/Meteina, lure the Tursin Loudmouth Boss (210158) to Meteina
// twice, burn the totem-pole flags (700037), set fire to the totem pole with a flint, kill either
// guardian, then turn in at Spatalos. Zone-mission-end/level-up gated on quest 1130.
using System;
using System.Collections.Generic;
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

namespace Quest.Verteron;

public sealed class _1019FlyingReconnaissance : QuestHandlerBase
{
    private const int QuestIdConst = 1019;
    private const int EstinoNpc    = 203146;
    private const int SpatalosNpc  = 203098;
    private const int MeteinaNpc   = 203147;
    private const int FlagNpc      = 700037;
    private const int ZilootaNpc   = 210697;
    private const int MunukaNpc    = 216891;
    private const int BossNpc      = 210158;
    private const int PotionItem   = 182200505;
    private const int FlintItem    = 182200023;
    private const string OutpostZone   = "TURSIN_OUTPOST_210030000";
    private const string TotemPoleZone = "TURSIN_TOTEM_POLE_210030000";
    private const float BossSpotX = 1552.7401f;
    private const float BossSpotY = 1160.3622f;
    private const float BossSpotZ = 114.06791f;

    private static readonly int[] _npcs = [EstinoNpc, SpatalosNpc, MeteinaNpc, FlagNpc];
    private static readonly int[] _mobs = [ZilootaNpc, MunukaNpc];

    private readonly IItemDao _itemDao;

    public _1019FlyingReconnaissance(IDataManager dataManager, IQuestDao questDao, QuestRewardService rewardService, IItemDao itemDao)
        : base(QuestIdConst, dataManager, questDao, rewardService)
    {
        _itemDao = itemDao;
    }

    public override void Register(QuestEngine engine)
    {
        engine.RegisterOnZoneMissionEnd(QuestId);
        engine.RegisterOnLevelUp(QuestId);
        foreach (int npc in _npcs)
            engine.RegisterQuestNpc(npc).OnTalk.Add(QuestId);
        engine.RegisterQuestItem(PotionItem, QuestId);
        engine.RegisterQuestItem(FlintItem, QuestId);
        foreach (int mob in _mobs)
            engine.RegisterQuestNpc(mob).OnKill.Add(QuestId);
        engine.RegisterQuestNpc(BossNpc).OnAttack.Add(QuestId);
    }

    public override ValueTask<bool> OnZoneMissionEndAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
        => DefaultOnZoneMissionEndEventAsync(env, conn, ct);

    public override ValueTask<bool> OnLevelUpAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
        => DefaultOnLvlUpEventAsync(env, conn, precedingQuestId: 1130, isZoneMission: true, ct);

    public override async ValueTask<bool> OnDialogAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var entry = env.Player.Quests.Get(QuestId);
        if (entry is null) return false;

        int targetObjId = env.Target?.ObjectId ?? 0;
        var dialog = DialogActionLookup.FromId(env.DialogId);

        if (entry.Status == QuestStatus.REWARD)
        {
            if (env.TargetId != SpatalosNpc) return false;
            if (dialog == DialogAction.USE_OBJECT)
                return await SendQuestDialogAsync(conn, targetObjId, 2034, ct);
            return await SendQuestEndDialogAsync(env, conn, ct);
        }

        if (entry.Status != QuestStatus.START) return false;
        int var = entry.GetVar(0);

        if (env.TargetId == EstinoNpc)
        {
            if (dialog == DialogAction.QUEST_SELECT && var == 0)
                return await SendQuestDialogAsync(conn, targetObjId, 1011, ct);
            if (dialog == DialogAction.SETPRO1)
                return await DefaultCloseDialogAsync(env, conn, _itemDao, 0, 1,
                    reward: false, sameNpc: false, giveItemId: PotionItem, giveItemCount: 1, removeItemId: 0, removeItemCount: 0, ct);
            return false;
        }

        if (env.TargetId == SpatalosNpc)
        {
            if (dialog == DialogAction.QUEST_SELECT && var == 2)
                return await SendQuestDialogAsync(conn, targetObjId, 1352, ct);
            if (dialog == DialogAction.SETPRO2)
                return await DefaultCloseDialogAsync(env, conn, 2, 3, ct);
            return false;
        }

        if (env.TargetId == MeteinaNpc)
        {
            if (dialog == DialogAction.QUEST_SELECT && var == 3)
                return await SendQuestDialogAsync(conn, targetObjId, 1438, ct);
            if (dialog == DialogAction.QUEST_SELECT && var == 5)
                return await SendQuestDialogAsync(conn, targetObjId, 1693, ct);
            if (dialog == DialogAction.SETPRO3)
                return await DefaultCloseDialogAsync(env, conn, 3, 4, ct);
            if (dialog == DialogAction.SETPRO4)
                return await DefaultCloseDialogAsync(env, conn, _itemDao, 5, 6,
                    reward: false, sameNpc: false, giveItemId: FlintItem, giveItemCount: 1, removeItemId: 0, removeItemCount: 0, ct);
            return false;
        }

        if (env.TargetId == FlagNpc)
        {
            if (dialog == DialogAction.USE_OBJECT && var >= 6 && var < 9)
                return await UseQuestObjectAsync(env, conn, var, var + 1, reward: false, dieObject: true, ct);
            return false;
        }

        return false;
    }

    public override async ValueTask<bool> OnAttackAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var entry = env.Player.Quests.Get(QuestId);
        if (entry is null || entry.Status != QuestStatus.START) return false;
        if (env.TargetId != BossNpc || env.Target is null) return false;

        float dx = env.Target.Position.X - BossSpotX;
        float dy = env.Target.Position.Y - BossSpotY;
        float dz = env.Target.Position.Z - BossSpotZ;
        if (MathF.Sqrt(dx * dx + dy * dy + dz * dz) > 30f) return false;

        int var = entry.GetVar(0);
        if (var == 11)
        {
            await PlayQuestMovieAsync(conn, env.Player, 22, ct);
            // Java also kills the boss via its AI controller (Npc.getController().onDie) — no NPC
            // controller/death-trigger infra exists yet; the quest still completes on this attack.
            await ChangeQuestStepAsync(conn, entry, 0, 10, toReward: true, ct);
            return true;
        }
        if (var == 4)
        {
            await PlayQuestMovieAsync(conn, env.Player, 13, ct);
            await ChangeQuestStepAsync(conn, entry, 0, 5, toReward: false, ct);
            return true;
        }
        return false;
    }

    public override async ValueTask<bool> OnItemUseAsync(Player player, int itemId, GsClientConnection conn, CancellationToken ct)
    {
        var entry = player.Quests.Get(QuestId);
        if (entry is null || entry.Status != QuestStatus.START) return false;
        int var = entry.GetVar(0);

        if (itemId == PotionItem && var == 1 && player.CurrentZones.Contains(OutpostZone))
        {
            var env = new QuestEnv(null, player, QuestId, 0);
            return await UseQuestObjectAsync(env, conn, 1, 2, reward: false, varNum: 0,
                addItemId: 0, addItemCount: 0, removeItemId: PotionItem, removeItemCount: 1, movieId: 18, dieObject: false, _itemDao, ct);
        }

        if (itemId == FlintItem && var == 9 && player.CurrentZones.Contains(TotemPoleZone))
        {
            var env = new QuestEnv(null, player, QuestId, 0);
            return await UseQuestObjectAsync(env, conn, 9, 10, reward: false, varNum: 0,
                addItemId: 0, addItemCount: 0, removeItemId: FlintItem, removeItemCount: 1, movieId: 0, dieObject: false, _itemDao, ct);
        }

        return false;
    }

    public override ValueTask<bool> OnKillAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
        => DefaultOnKillEventAsync(env, conn, (IReadOnlyCollection<int>)_mobs, 10, reward: true, ct);
}
