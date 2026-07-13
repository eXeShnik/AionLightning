// Port of Java data/scripts/system/handlers/quest/rider_quests/_14031AHyper_vention.java.
// Eltnen chain (level-up gated on 14030): Fasimedes (203700, var0 0->1), Iostes (801216, 1->2),
// Pernos (790001, 2->3 giving item 182215388), then USE 182215388 inside LF1_ITEMUSEAREA_Q14031
// (3->4), Khidia (203183, 4->5 giving 182215389), USE 182215389 inside LF1A_ITEMUSEAREA_Q14031
// (5->6), Tumblusen (203989, 6->7 giving 182215390), USE 182215390 inside LF2_ITEMUSEAREA_Q14031
// (7->8), Tumblusen again (8->9) enters instance world 310040000 and spawns boss 233878; kill 233878
// (var0==9) spawns the Large Teleport (730888); USE it (var0==10) spawns the Shattered Large Teleport
// (730898) and advances 10->11; USE that (var0==11) flips to REWARD. Turn in at Fasimedes. Dying or
// leaving 310040000 while var0 in [9,11) reverts to var0 8.
// Instance entry uses EnterInstanceAsync; in-instance/world spawns use SpawnQuestNpc; player-death
// revert uses OnDieAsync; logout revert uses OnLogOutAsync (persists without sending packets).
// Skips vs Java: (1) SM_ITEM_USAGE_ANIMATION cast-time animation + 3s schedule on each item-use
// dropped — the item removal and var++ apply immediately (same precedent as UseQuestObjectAsync).
// (2) npc.getController().onDelete() despawns of the teleport objects dropped — no despawn API.
// (3) TeleportService2 relocation to Eltnen (210020000) on the final USE dropped — non-entry
// relocation, the REWARD flip is kept. (4) QUEST_FAILED_$1 system message dropped — cosmetic notice.
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

public sealed class _14031AHyper_vention : QuestHandlerBase
{
    private const int QuestIdConst = 14031;
    private const int FasimedesNpc = 203700;
    private const int IostesNpc    = 801216;
    private const int PernosNpc    = 790001;
    private const int KhidiaNpc    = 203183;
    private const int TumblusenNpc = 203989;
    private const int LargeTele    = 730888;
    private const int ShatteredTele = 730898;
    private const int BossNpc      = 233878;
    private const int Item1        = 182215388;
    private const int Item2        = 182215389;
    private const int Item3        = 182215390;
    private const int InstanceWorld = 310040000;
    private const string Zone1     = "LF1_ITEMUSEAREA_Q14031";
    private const string Zone2     = "LF1A_ITEMUSEAREA_Q14031";
    private const string Zone3     = "LF2_ITEMUSEAREA_Q14031";

    private readonly IItemDao _itemDao;

    public _14031AHyper_vention(IDataManager dataManager, IQuestDao questDao, QuestRewardService rewardService, IItemDao itemDao)
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
        engine.RegisterOnEnterWorld(QuestId);
        engine.RegisterQuestItem(Item1, QuestId);
        engine.RegisterQuestItem(Item2, QuestId);
        engine.RegisterQuestItem(Item3, QuestId);
        engine.RegisterQuestNpc(BossNpc).OnKill.Add(QuestId);
        foreach (int npc in new[] { FasimedesNpc, IostesNpc, PernosNpc, KhidiaNpc, TumblusenNpc, LargeTele, ShatteredTele })
            engine.RegisterQuestNpc(npc).OnTalk.Add(QuestId);
    }

    public override ValueTask<bool> OnZoneMissionEndAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
        => DefaultOnZoneMissionEndEventAsync(env, conn, ct);

    public override ValueTask<bool> OnLevelUpAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
        => DefaultOnLvlUpEventAsync(env, conn, 14030, isZoneMission: true, ct);

    public override async ValueTask<bool> OnDialogAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var player = env.Player;
        var entry  = player.Quests.Get(QuestId);
        if (entry is null) return false;

        int targetId    = env.TargetId;
        int targetObjId = env.Target?.ObjectId ?? 0;
        var dialog      = DialogActionLookup.FromId(env.DialogId);
        int var         = entry.GetVar(0);

        if (entry.Status == QuestStatus.REWARD)
        {
            if (targetId == FasimedesNpc) return await SendQuestEndDialogAsync(env, conn, ct);
            return false;
        }
        if (entry.Status != QuestStatus.START) return false;

        if (targetId == FasimedesNpc)
        {
            if (dialog == DialogAction.QUEST_SELECT) return await SendQuestDialogAsync(conn, targetObjId, 1011, ct);
            if (dialog == DialogAction.SETPRO1) return await DefaultCloseDialogAsync(env, conn, 0, 1, ct);
            return false;
        }
        if (targetId == IostesNpc)
        {
            if (dialog == DialogAction.QUEST_SELECT && var == 1) return await SendQuestDialogAsync(conn, targetObjId, 1352, ct);
            if (dialog == DialogAction.SETPRO2) return await DefaultCloseDialogAsync(env, conn, 1, 2, ct);
            return false;
        }
        if (targetId == PernosNpc)
        {
            if (dialog == DialogAction.QUEST_SELECT && var == 2) return await SendQuestDialogAsync(conn, targetObjId, 1693, ct);
            if (dialog == DialogAction.SETPRO3)
            {
                await GiveQuestItemAsync(player, conn, _itemDao, Item1, 1, ct);
                return await DefaultCloseDialogAsync(env, conn, 2, 3, ct);
            }
            return false;
        }
        if (targetId == KhidiaNpc)
        {
            if (dialog == DialogAction.QUEST_SELECT && var == 4) return await SendQuestDialogAsync(conn, targetObjId, 2376, ct);
            if (dialog == DialogAction.SETPRO5)
            {
                await GiveQuestItemAsync(player, conn, _itemDao, Item2, 1, ct);
                return await DefaultCloseDialogAsync(env, conn, 4, 5, ct);
            }
            return false;
        }
        if (targetId == TumblusenNpc)
        {
            if (dialog == DialogAction.QUEST_SELECT)
            {
                if (var == 6) return await SendQuestDialogAsync(conn, targetObjId, 3057, ct);
                if (var == 8) return await SendQuestDialogAsync(conn, targetObjId, 3740, ct);
                return false;
            }
            if (dialog == DialogAction.SETPRO7)
            {
                await GiveQuestItemAsync(player, conn, _itemDao, Item3, 1, ct);
                return await DefaultCloseDialogAsync(env, conn, 6, 7, ct);
            }
            if (dialog == DialogAction.SETPRO9)
            {
                await EnterInstanceAsync(player, conn, InstanceWorld, 266f, 211f, 210f, 0, ct);
                SpawnQuestNpc(InstanceWorld, player.Position.InstanceId, BossNpc, 254f, 256f, 227f, 95);
                return await DefaultCloseDialogAsync(env, conn, 8, 9, ct);
            }
            if (dialog == DialogAction.FINISH_DIALOG) return await CloseDialogWindowAsync(conn, targetObjId, ct);
            return false;
        }
        if (targetId == LargeTele)
        {
            if (dialog == DialogAction.USE_OBJECT && var == 10)
            {
                await PlayQuestMovieAsync(conn, player, 898, ct);
                var pos = env.Target?.Position ?? player.Position;
                SpawnQuestNpc(pos.WorldId, pos.InstanceId, ShatteredTele, pos.X, pos.Y, pos.Z, 0);
                // note: npc.getController().onDelete() despawn dropped — no despawn API for quest scripts
                await ChangeQuestStepAsync(conn, entry, 0, 11, toReward: false, ct);
                return await CloseDialogWindowAsync(conn, targetObjId, ct);
            }
            return false;
        }
        if (targetId == ShatteredTele)
        {
            if (dialog == DialogAction.USE_OBJECT && var == 11)
            {
                // note: npc.getController().onDelete() despawn + TeleportService2 relocation to Eltnen (210020000) dropped — non-entry relocation; REWARD flip kept
                entry.Status = QuestStatus.REWARD;
                await UpdateQuestStatusAsync(conn, entry, ct);
                return true;
            }
        }
        return false;
    }

    public override async ValueTask<bool> OnKillAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var player = env.Player;
        var entry  = player.Quests.Get(QuestId);
        if (entry is null || entry.Status != QuestStatus.START) return false;

        if (env.TargetId == BossNpc && entry.GetVar(0) == 9)
        {
            var pos = player.Position;
            SpawnQuestNpc(pos.WorldId, pos.InstanceId, LargeTele, pos.X, pos.Y, pos.Z, (byte)pos.Heading);
            entry.SetVar(0, 10);
            await UpdateQuestStatusAsync(conn, entry, ct);
            return true;
        }
        return false;
    }

    public override async ValueTask<bool> OnItemUseAsync(Player player, int itemId, GsClientConnection conn, CancellationToken ct)
    {
        var entry = player.Quests.Get(QuestId);
        if (entry is null) return false;

        int var = entry.GetVar(0);
        // note: SM_ITEM_USAGE_ANIMATION cast-time animation + 3s schedule dropped — item removal and var++ apply immediately
        if (var == 3 && itemId == Item1)
        {
            if (!player.CurrentZones.Contains(Zone1)) return false;
            await RemoveQuestItemAsync(player, conn, _itemDao, Item1, 1, ct);
            entry.SetVar(0, 4);
            await UpdateQuestStatusAsync(conn, entry, ct);
            return true;
        }
        if (var == 5 && itemId == Item2)
        {
            if (!player.CurrentZones.Contains(Zone2)) return false;
            await RemoveQuestItemAsync(player, conn, _itemDao, Item2, 1, ct);
            entry.SetVar(0, 6);
            await UpdateQuestStatusAsync(conn, entry, ct);
            return true;
        }
        if (var == 7 && itemId == Item3)
        {
            if (!player.CurrentZones.Contains(Zone3)) return false;
            await RemoveQuestItemAsync(player, conn, _itemDao, Item3, 1, ct);
            entry.SetVar(0, 8);
            await UpdateQuestStatusAsync(conn, entry, ct);
            return true;
        }
        return false;
    }

    public override async ValueTask<bool> OnDieAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var entry = env.Player.Quests.Get(QuestId);
        if (entry is null || entry.Status != QuestStatus.START) return false;

        int var = entry.GetVar(0);
        if (var >= 9 && var < 11)
        {
            // note: QUEST_FAILED_$1 system message dropped — cosmetic notice
            entry.SetVar(0, 8);
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

        int var = entry.GetVar(0);
        if (var >= 9 && var < 11)
        {
            entry.SetVar(0, 8);
            await QuestDao.UpsertAsync(player.ObjectId, entry, ct); // logout hook: mutate + persist only, no packets
            return true;
        }
        return false;
    }

    public override ValueTask<bool> OnEnterWorldAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var player = env.Player;
        var entry  = player.Quests.Get(QuestId);
        if (entry is null || entry.Status != QuestStatus.START) return ValueTask.FromResult(false);

        int var = entry.GetVar(0);
        if ((var == 9 || var == 10) && player.Position.WorldId != InstanceWorld)
        {
            entry.SetVar(0, 8); // Java sets var in memory only here (no updateQuestStatus)
            return ValueTask.FromResult(true);
        }
        return ValueTask.FromResult(false);
    }
}
