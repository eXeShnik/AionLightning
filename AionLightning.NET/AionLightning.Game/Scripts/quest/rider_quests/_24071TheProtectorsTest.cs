// Port of Java data/scripts/system/handlers/quest/rider_quests/_24071TheProtectorsTest.java (pralinka).
// Zone-mission sub-quest of 24070: Rafael (205585, var0 0->1), Garnon (205987, 1->2 and 7->8),
// Oriata (802058, 2->3 with movie 889, and 8->9 handing out item 182215402, plus the REWARD turn-in),
// Hatiel (205754, 3->4), a no-op npc 702088 that just acks any dialog, split kill counters at var0==6
// (var1 0->5 across 217912/217913, var2 0->3 across 217914) and var0==7 (var3 0->6 across
// 218098/218100/218578), Manyos (205743, collect-item check 4->5, then 5->6), Sutton (205756, 6->7
// gated on var1==5 && var2==3, SET... wait SETPRO7 -> reward). Using either quest item inside the
// IDLDF4A_ItemUseArea_Q14071 zone finishes var0 9->REWARD with movie 890.
// Skip vs Java: Garnon's SETPRO2/SETPRO8 branches called TeleportService2.teleportTo(player,
// 600020000, ...) - no TeleportService exists in this port; the var transitions are kept.
// Java bugs fixed: (1) Garnon's dialog switch had no break after QUEST_SELECT, so talking with the
// wrong var fell through into SETPRO2's body and fired the (now-skipped) teleport unconditionally
// before defaultCloseDialog's own guard - ported with explicit dialog checks so it would only have
// fired on an actual SETPRO2 dialog. (2) Manyos' dialog switch had the same missing break, so a
// QUEST_SELECT with var0 outside {4,5} fell through into the CHECK_USER_HAS_QUEST_ITEM body and ran
// the collect-item check/consume+dialog flow on the wrong dialog action - fixed by gating it behind
// an actual CHECK_USER_HAS_QUEST_ITEM dialog.
using System.Linq;
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

public sealed class _24071TheProtectorsTest : QuestHandlerBase
{
    private const int QuestIdConst = 24071;
    private const int RafaelNpc     = 205585;
    private const int GarnonNpc     = 205987;
    private const int OriataNpc     = 802058;
    private const int HatielNpc     = 205754;
    private const int NoOpNpc       = 702088;
    private const int ManyosNpc     = 205743;
    private const int SuttonNpc     = 205756;
    private const int RewardItem    = 182215402;
    private const string ItemUseZone = "IDLDF4A_ItemUseArea_Q14071";
    private static readonly int[] _var1Mobs = [217912, 217913];
    private const int Var2Mob = 217914;
    private static readonly int[] _var3Mobs = [218098, 218100, 218578];

    private readonly IItemDao _itemDao;

    public _24071TheProtectorsTest(IDataManager dataManager, IQuestDao questDao, QuestRewardService rewardService, IItemDao itemDao)
        : base(QuestIdConst, dataManager, questDao, rewardService)
    {
        _itemDao = itemDao;
    }

    public override void Register(QuestEngine engine)
    {
        engine.RegisterOnZoneMissionEnd(QuestId);
        engine.RegisterOnLevelUp(QuestId);
        engine.RegisterOnEnterWorld(QuestId);
        engine.RegisterQuestItem(182215401, QuestId);
        engine.RegisterQuestItem(RewardItem, QuestId);
        foreach (int npc in new[] { RafaelNpc, GarnonNpc, OriataNpc, HatielNpc, NoOpNpc, ManyosNpc, SuttonNpc })
            engine.RegisterQuestNpc(npc).OnTalk.Add(QuestId);
        foreach (int mob in new[] { 217912, 217913, 217914, 218098, 218100, 218578 })
            engine.RegisterQuestNpc(mob).OnKill.Add(QuestId);
    }

    public override ValueTask<bool> OnZoneMissionEndAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
        => DefaultOnZoneMissionEndEventAsync(env, conn, ct);

    public override ValueTask<bool> OnLevelUpAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
        => DefaultOnLvlUpEventAsync(env, conn, 24070, isZoneMission: true, ct);

    public override async ValueTask<bool> OnDialogAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var player = env.Player;
        var entry  = player.Quests.Get(QuestId);
        if (entry is null) return false;

        int targetId    = env.TargetId;
        int targetObjId = env.Target?.ObjectId ?? 0;
        int var0        = entry.GetVar(0);
        var dialog      = DialogActionLookup.FromId(env.DialogId);

        if (entry.Status == QuestStatus.START)
        {
            if (targetId == RafaelNpc)
            {
                if (dialog == DialogAction.QUEST_SELECT && var0 == 0) return await SendQuestDialogAsync(conn, targetObjId, 1011, ct);
                if (dialog == DialogAction.SETPRO1) return await DefaultCloseDialogAsync(env, conn, 0, 1, ct);
                return false;
            }
            if (targetId == GarnonNpc)
            {
                int var3 = entry.GetVar(3);
                if (dialog == DialogAction.QUEST_SELECT && var0 == 1) return await SendQuestDialogAsync(conn, targetObjId, 1352, ct);
                if (dialog == DialogAction.QUEST_SELECT && var0 == 7 && var3 == 6) return await SendQuestDialogAsync(conn, targetObjId, 3398, ct);
                if (dialog == DialogAction.SETPRO2) return await DefaultCloseDialogAsync(env, conn, 1, 2, ct); // relocation skipped, see header
                if (dialog == DialogAction.SETPRO8) return await DefaultCloseDialogAsync(env, conn, 7, 8, ct); // relocation skipped, see header
                return false;
            }
            if (targetId == OriataNpc)
            {
                if (dialog == DialogAction.QUEST_SELECT && var0 == 2)
                {
                    await PlayQuestMovieAsync(conn, player, 889, ct);
                    return await SendQuestDialogAsync(conn, targetObjId, 1693, ct);
                }
                if (dialog == DialogAction.QUEST_SELECT && var0 == 8) return await SendQuestDialogAsync(conn, targetObjId, 3740, ct);
                if (dialog == DialogAction.SETPRO3) return await DefaultCloseDialogAsync(env, conn, 2, 3, ct);
                if (dialog == DialogAction.SETPRO9)
                {
                    await GiveQuestItemAsync(player, conn, _itemDao, RewardItem, 1, ct);
                    return await DefaultCloseDialogAsync(env, conn, 8, 9, ct);
                }
                return false;
            }
            if (targetId == HatielNpc)
            {
                if (dialog == DialogAction.QUEST_SELECT && var0 == 3) return await SendQuestDialogAsync(conn, targetObjId, 2034, ct);
                if (dialog == DialogAction.SETPRO4) return await DefaultCloseDialogAsync(env, conn, 3, 4, ct);
                return false;
            }
            if (targetId == NoOpNpc) return true;
            if (targetId == ManyosNpc)
            {
                if (dialog == DialogAction.QUEST_SELECT && var0 == 4) return await SendQuestDialogAsync(conn, targetObjId, 2375, ct);
                if (dialog == DialogAction.QUEST_SELECT && var0 == 5) return await SendQuestDialogAsync(conn, targetObjId, 2716, ct);
                if (dialog == DialogAction.CHECK_USER_HAS_QUEST_ITEM)
                    return await CheckQuestItemsAsync(env, conn, _itemDao, 4, 5, reward: false, checkOkId: 10000, checkFailId: 10001, ct);
                if (dialog == DialogAction.SETPRO6) return await DefaultCloseDialogAsync(env, conn, 5, 6, ct);
                return false;
            }
            if (targetId == SuttonNpc)
            {
                int var1 = entry.GetVar(1);
                int var2 = entry.GetVar(2);
                if (dialog == DialogAction.QUEST_SELECT && var0 == 6 && var1 == 5 && var2 == 3) return await SendQuestDialogAsync(conn, targetObjId, 3057, ct);
                if (dialog == DialogAction.SETPRO7) return await DefaultCloseDialogAsync(env, conn, 6, 7, ct);
                return false;
            }
        }
        else if (entry.Status == QuestStatus.REWARD)
        {
            if (targetId == OriataNpc) return await SendQuestEndDialogAsync(env, conn, ct);
        }
        return false;
    }

    public override async ValueTask<bool> OnKillAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var entry = env.Player.Quests.Get(QuestId);
        if (entry is null || entry.Status != QuestStatus.START) return false;

        int targetId = env.TargetId;
        int var0     = entry.GetVar(0);

        if (_var1Mobs.Contains(targetId))
        {
            if (var0 != 6) return false;
            int var1 = entry.GetVar(1);
            if (var1 < 0 || var1 > 4) return false;
            await ChangeQuestStepAsync(conn, entry, 1, var1 + 1, toReward: false, ct);
            return true;
        }
        if (targetId == Var2Mob)
        {
            if (var0 != 6) return false;
            int var2 = entry.GetVar(2);
            if (var2 < 0 || var2 > 2) return false;
            await ChangeQuestStepAsync(conn, entry, 2, var2 + 1, toReward: false, ct);
            return true;
        }
        if (_var3Mobs.Contains(targetId))
        {
            if (var0 != 7) return false;
            int var3 = entry.GetVar(3);
            if (var3 < 0 || var3 > 5) return false;
            await ChangeQuestStepAsync(conn, entry, 3, var3 + 1, toReward: false, ct);
            return true;
        }
        return false;
    }

    public override async ValueTask<bool> OnItemUseAsync(Player player, int itemId, GsClientConnection conn, CancellationToken ct)
    {
        if (!player.CurrentZones.Contains(ItemUseZone)) return false;

        var env = new QuestEnv(null, player, QuestId, 0);
        return await UseQuestObjectAsync(env, conn, 9, 9, reward: true, varNum: 0,
            addItemId: 0, addItemCount: 0, removeItemId: itemId, removeItemCount: 1,
            movieId: 890, dieObject: false, _itemDao, ct);
    }

    public override ValueTask<bool> OnEnterWorldAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var player = env.Player;
        var entry  = player.Quests.Get(QuestId);
        if (entry is null || entry.Status != QuestStatus.START) return ValueTask.FromResult(false);
        if (player.Position.WorldId != 300330000) return ValueTask.FromResult(false);

        int var0 = entry.GetVar(0);
        if (var0 == 2)
        {
            SpawnQuestNpc(300330000, player.Position.InstanceId, 702089, 250.331f, 245.210f, 126.270f, 60);
            SpawnQuestNpc(300330000, player.Position.InstanceId, OriataNpc, 249.11f, 248.15f, 125.06f, 70);
        }
        else if (var0 == 8 || var0 == 9)
        {
            SpawnQuestNpc(300330000, player.Position.InstanceId, OriataNpc, 249.11f, 248.15f, 125.06f, 72);
            SpawnQuestNpc(300330000, player.Position.InstanceId, 702089, 250.331f, 245.210f, 126.270f, 60);
        }
        return ValueTask.FromResult(false);
    }
}
