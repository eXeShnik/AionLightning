// Port of Java data/scripts/system/handlers/quest/theobomos/_1092JosnacksDilemma.java.
// Zone-mission quest chained off 1091: report to Atropos (798155) then Josnack (798206, var 1->2,
// SETPRO2 teleports the player away); interact with both statue stones (700389/700388) to advance
// var 0 to 3 once both flags are set; killing 700390 at var 4 (while short of 6x collect item
// 182208012) spawns two statue-fragment sources (214552, dropping 182208033 on kill, registered as
// a handler-side quest drop); Atropos checks the 6 collected fragments and completes.
// Skip vs Java: TeleportService2.teleportTo (Josnack SETPRO2) is omitted - no TeleportService2
// exists in this port (same precedent as quest/eltnen/_1482ATeleportationAdventure.cs); the var
// transition is kept so the quest stays completable without the teleport. onDieEvent (resets var
// 0 from 2 back to 1 on player death) is omitted - this port has no player-death quest hook
// (registerOnDie has no IQuestHandler equivalent yet).
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

namespace Quest.Theobomos;

public sealed class _1092JosnacksDilemma : QuestHandlerBase
{
    private const int QuestIdConst    = 1092;
    private const int AtroposNpc      = 798155;
    private const int JosnackNpc      = 798206;
    private const int StoneAboveNpc   = 700389;
    private const int StonePlatformNpc = 700388;
    private const int KillTargetNpc   = 700390;
    private const int StatueFragmentSourceNpc = 214552;
    private const int FragmentItemId  = 182208012;
    private const int DropItemId      = 182208033;

    private readonly IItemDao _itemDao;

    public _1092JosnacksDilemma(IDataManager dataManager, IQuestDao questDao, QuestRewardService rewardService, IItemDao itemDao)
        : base(QuestIdConst, dataManager, questDao, rewardService)
    {
        _itemDao = itemDao;
    }

    public override void Register(QuestEngine engine)
    {
        engine.RegisterOnZoneMissionEnd(QuestId);
        engine.RegisterOnLevelUp(QuestId);
        engine.RegisterQuestNpc(KillTargetNpc).OnKill.Add(QuestId);
        RegisterQuestDrop(engine, StatueFragmentSourceNpc, DropItemId, 1, 100);
        engine.RegisterQuestNpc(AtroposNpc).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(JosnackNpc).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(StoneAboveNpc).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(StonePlatformNpc).OnTalk.Add(QuestId);
    }

    public override ValueTask<bool> OnZoneMissionEndAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
        => DefaultOnZoneMissionEndEventAsync(env, conn, ct);

    public override ValueTask<bool> OnLevelUpAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
        => DefaultOnLvlUpEventAsync(env, conn, precedingQuestId: 1091, isZoneMission: true, ct);

    public override async ValueTask<bool> OnDialogAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var player = env.Player;
        var entry  = player.Quests.Get(QuestId);
        if (entry is null) return false;

        int targetId = env.TargetId;
        int targetObjId = env.Target?.ObjectId ?? 0;

        if (entry.Status == QuestStatus.REWARD)
            return targetId == AtroposNpc && await SendQuestEndDialogAsync(env, conn, ct);

        if (entry.Status != QuestStatus.START) return false;

        var dialog = DialogActionLookup.FromId(env.DialogId);
        int var = entry.GetVar(0);

        if (targetId == AtroposNpc)
        {
            if (dialog == DialogAction.QUEST_SELECT)
            {
                if (var == 0) return await SendQuestDialogAsync(conn, targetObjId, 1011, ct);
                if (var == 3) return await SendQuestDialogAsync(conn, targetObjId, 2034, ct);
                if (var == 4) return await SendQuestDialogAsync(conn, targetObjId, 2375, ct);
                return true;
            }
            if (dialog == DialogAction.SETPRO1)
                return await DefaultCloseDialogAsync(env, conn, 0, 1, ct);
            if (dialog == DialogAction.SETPRO4)
                return await DefaultCloseDialogAsync(env, conn, 3, 4, ct);
            if (dialog == DialogAction.CHECK_USER_HAS_QUEST_ITEM)
                return await CheckQuestItemsAsync(env, conn, _itemDao, 4, 4, true, 10001, 10008, ct);
            return false;
        }

        if (targetId == JosnackNpc)
        {
            if (dialog == DialogAction.QUEST_SELECT)
            {
                if (var == 1) return await SendQuestDialogAsync(conn, targetObjId, 1352, ct);
                if (var == 2) return await SendQuestDialogAsync(conn, targetObjId, 1693, ct);
                await PlayQuestMovieAsync(conn, player, 364, ct);
                return false;
            }
            if (dialog == DialogAction.SELECT_ACTION_1353)
            {
                await PlayQuestMovieAsync(conn, player, 364, ct);
                return false;
            }
            if (dialog == DialogAction.SETPRO2 && var == 1)
            {
                await DefaultCloseDialogAsync(env, conn, 1, 2, ct);
                return true;
            }
            return false;
        }

        if (targetId == StoneAboveNpc)
        {
            if (dialog == DialogAction.USE_OBJECT && var == 2 && entry.GetVar(1) == 0)
            {
                entry.SetVar(1, 1);
                await UpdateQuestStatusAsync(conn, entry, ct);
                if (entry.GetVar(2) == 1)
                {
                    entry.SetVar(0, 3);
                    await UpdateQuestStatusAsync(conn, entry, ct);
                }
                return true;
            }
            return false;
        }

        if (targetId == StonePlatformNpc)
        {
            if (dialog == DialogAction.USE_OBJECT && var == 2 && entry.GetVar(2) == 0)
            {
                entry.SetVar(2, 1);
                if (entry.GetVar(1) == 1)
                    entry.SetVar(0, 3);
                await UpdateQuestStatusAsync(conn, entry, ct);
                return true;
            }
            return false;
        }

        return false;
    }

    public override async ValueTask<bool> OnKillAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var player = env.Player;
        var entry  = player.Quests.Get(QuestId);
        if (entry is null || entry.Status != QuestStatus.START) return false;
        if (env.TargetId != KillTargetNpc || entry.Step != 4) return false;
        if ((player.Inventory.FindByItemId(FragmentItemId)?.Count ?? 0) >= 6) return false;

        var pos = player.Position;
        SpawnQuestNpc(pos.WorldId, pos.InstanceId, StatueFragmentSourceNpc, 239.66934f, 2734.8235f, 76.56028f, 81);
        SpawnQuestNpc(pos.WorldId, pos.InstanceId, StatueFragmentSourceNpc, 239.33173f, 2739.9043f, 77.56217f, 30);
        return true;
    }
}
