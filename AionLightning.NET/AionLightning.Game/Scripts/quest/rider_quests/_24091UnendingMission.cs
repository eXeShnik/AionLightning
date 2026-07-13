// Port of Java data/scripts/system/handlers/quest/rider_quests/_24091UnendingMission.java (pralinka).
// Asmodian finale chain (level-up gated on 24090): Oriata of the Past (802061, var0 0->1) spawns
// Marchutan (802064); Marchutan (802064, var0==1) gives item 182215415 and (relocates the player)
// var0 1->2; Aimah (205617, 2->3), Agehia (798800, 3->4 and 5->6), Vidar (204052, 4->5), Garnon
// (205987, var0==6) flips to REWARD. Turn in at Tepes (800170). Entering world 300500000 at var0==0
// spawns Oriata of the Past; logging out at var0 <= 2 resets to var0 0.
// In-world spawns use SpawnQuestNpc; logout revert uses OnLogOutAsync (persists without packets).
// Skips vs Java: (1) npc.getController().onDelete() despawns of 802061/802064 dropped — no despawn
// API for quest scripts. (2) Marchutan's SETPRO2 TeleportService2 relocation to the fixed-instance
// world 600020000 (instanceId 1, 1376/1451/600) is a fixed-channel relocation, not a
// getNextAvailableInstance entry — dropped, the item grant + var0 1->2 transition is kept.
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

public sealed class _24091UnendingMission : QuestHandlerBase
{
    private const int QuestIdConst = 24091;
    private const int OriataPast61 = 802061;
    private const int MarchutanNpc = 802064;
    private const int AimahNpc     = 205617;
    private const int AgehiaNpc    = 798800;
    private const int VidarNpc     = 204052;
    private const int GarnonNpc    = 205987;
    private const int TepesNpc     = 800170;
    private const int RewardItem   = 182215415;
    private const int Instance500  = 300500000;

    private readonly IItemDao _itemDao;

    public _24091UnendingMission(IDataManager dataManager, IQuestDao questDao, QuestRewardService rewardService, IItemDao itemDao)
        : base(QuestIdConst, dataManager, questDao, rewardService)
    {
        _itemDao = itemDao;
    }

    public override void Register(QuestEngine engine)
    {
        engine.RegisterOnZoneMissionEnd(QuestId);
        engine.RegisterOnLevelUp(QuestId);
        RegisterOnLogOut(engine);
        engine.RegisterOnEnterWorld(QuestId);
        foreach (int npc in new[] { OriataPast61, MarchutanNpc, AimahNpc, AgehiaNpc, VidarNpc, GarnonNpc, TepesNpc })
            engine.RegisterQuestNpc(npc).OnTalk.Add(QuestId);
    }

    public override ValueTask<bool> OnZoneMissionEndAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
        => DefaultOnZoneMissionEndEventAsync(env, conn, ct);

    public override ValueTask<bool> OnLevelUpAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
        => DefaultOnLvlUpEventAsync(env, conn, 24090, isZoneMission: true, ct);

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
            if (targetId == OriataPast61)
            {
                if (dialog == DialogAction.QUEST_SELECT && var == 0) return await SendQuestDialogAsync(conn, targetObjId, 1011, ct);
                if (dialog == DialogAction.SETPRO1)
                {
                    SpawnQuestNpc(Instance500, player.Position.InstanceId, MarchutanNpc, 253f, 245f, 125f, 119);
                    // note: npc.getController().onDelete() despawn dropped — no despawn API
                    return await DefaultCloseDialogAsync(env, conn, 0, 1, ct);
                }
                return false;
            }
            if (targetId == MarchutanNpc)
            {
                if (dialog == DialogAction.QUEST_SELECT && var == 1) return await SendQuestDialogAsync(conn, targetObjId, 1352, ct);
                if (dialog == DialogAction.SETPRO2)
                {
                    await GiveQuestItemAsync(player, conn, _itemDao, RewardItem, 1, ct);
                    // note: TeleportService2 relocation to fixed-instance world 600020000 (instanceId 1, 1376/1451/600) dropped — fixed-channel relocation; var0 1->2 kept
                    // note: npc.getController().onDelete() despawn dropped — no despawn API
                    return await DefaultCloseDialogAsync(env, conn, 1, 2, ct);
                }
                return false;
            }
            if (targetId == AimahNpc)
            {
                if (dialog == DialogAction.QUEST_SELECT && var == 2) return await SendQuestDialogAsync(conn, targetObjId, 1693, ct);
                if (dialog == DialogAction.SETPRO3) return await DefaultCloseDialogAsync(env, conn, 2, 3, ct);
                return false;
            }
            if (targetId == AgehiaNpc)
            {
                if (dialog == DialogAction.QUEST_SELECT)
                {
                    if (var == 3) return await SendQuestDialogAsync(conn, targetObjId, 2034, ct);
                    if (var == 5) return await SendQuestDialogAsync(conn, targetObjId, 2716, ct);
                    return false;
                }
                if (dialog == DialogAction.SETPRO4) return await DefaultCloseDialogAsync(env, conn, 3, 4, ct);
                if (dialog == DialogAction.SETPRO6) return await DefaultCloseDialogAsync(env, conn, 5, 6, ct);
                return false;
            }
            if (targetId == VidarNpc)
            {
                if (dialog == DialogAction.QUEST_SELECT && var == 4) return await SendQuestDialogAsync(conn, targetObjId, 2375, ct);
                if (dialog == DialogAction.SETPRO5) return await DefaultCloseDialogAsync(env, conn, 4, 5, ct);
                return false;
            }
            if (targetId == GarnonNpc)
            {
                if (dialog == DialogAction.QUEST_SELECT && var == 6) return await SendQuestDialogAsync(conn, targetObjId, 3057, ct);
                if (dialog == DialogAction.SETPRO7) return await DefaultCloseDialogAsync(env, conn, 6, 6, reward: true, sameNpc: false, ct);
                return false;
            }
        }
        else if (entry.Status == QuestStatus.REWARD)
        {
            if (targetId == TepesNpc) return await SendQuestEndDialogAsync(env, conn, ct);
        }
        return false;
    }

    public override ValueTask<bool> OnEnterWorldAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var player = env.Player;
        var entry  = player.Quests.Get(QuestId);
        if (entry is null || entry.Status != QuestStatus.START) return ValueTask.FromResult(false);
        if (player.Position.WorldId == Instance500 && entry.GetVar(0) == 0)
            SpawnQuestNpc(Instance500, player.Position.InstanceId, OriataPast61, 255f, 245f, 125f, 55);
        return ValueTask.FromResult(false);
    }

    public override async ValueTask<bool> OnLogOutAsync(QuestEnv env, GsClientConnection? conn, CancellationToken ct)
    {
        var player = env.Player;
        var entry  = player.Quests.Get(QuestId);
        if (entry is null || entry.Status != QuestStatus.START) return false;
        if (entry.GetVar(0) <= 2)
        {
            entry.SetVar(0, 0);
            await QuestDao.UpsertAsync(player.ObjectId, entry, ct); // logout hook: mutate + persist only, no packets
            return true;
        }
        return false;
    }
}
