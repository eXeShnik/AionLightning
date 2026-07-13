// Port of Java data/scripts/system/handlers/quest/rider_quests/_14071HurdlingForTruth.java (pralinka).
// Zone-mission sub-quest of 14070: Kutos (205581, var0 0->1) -> Garnon (205987, var0 1->2, then
// 7->8) -> Oriata (802058, var0 2->3, movie 889; then 8->9 giving item 182215399) -> Yaci (205753,
// var0 3->4) -> a plain trigger npc (702088, always handled, no dialog) -> Manyos (205743, var0 4->5
// collect-check, then 5->6) -> Sutton (205756, var0 6->7, gated on var1==5 && var2==3), kills of
// three mob groups bump vars 1/2/3 independently while var0==6 or 7, an item use inside a zone
// finishes at var0 9->9 (reward), turn in at Oriata.
// Skip vs Java: Garnon's SETPRO2/SETPRO8 dialogs teleport the player to world 600020000 via
// TeleportService2 (not ported) - the var transitions stay, only the relocation is dropped (same
// precedent as quest/rider_quests/_14024AKrallingSuspicion.cs).
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

public sealed class _14071HurdlingForTruth : QuestHandlerBase
{
    private const int QuestIdConst = 14071;
    private const int KutosNpc     = 205581;
    private const int GarnonNpc    = 205987;
    private const int OriataNpc    = 802058;
    private const int YaciNpc      = 205753;
    private const int TriggerNpc   = 702088;
    private const int SpawnedNpc   = 702089;
    private const int ManyosNpc    = 205743;
    private const int SuttonNpc    = 205756;
    private const int Item182215398 = 182215398;
    private const int Item182215399 = 182215399;
    private const string ItemUseAreaZone = "IDLDF4A_ItemUseArea_Q14071";

    private static readonly int[] _kaidanPairMobs = [217912, 217913];
    private const int KaidanScoutMob = 217914;
    private static readonly int[] _kaidanEliteMobs = [218098, 218100, 218578];

    private readonly IItemDao _itemDao;

    public _14071HurdlingForTruth(IDataManager dataManager, IQuestDao questDao, QuestRewardService rewardService, IItemDao itemDao)
        : base(QuestIdConst, dataManager, questDao, rewardService)
    {
        _itemDao = itemDao;
    }

    public override void Register(QuestEngine engine)
    {
        engine.RegisterOnZoneMissionEnd(QuestId);
        engine.RegisterOnLevelUp(QuestId);
        engine.RegisterOnEnterWorld(QuestId);
        engine.RegisterQuestItem(Item182215398, QuestId);
        engine.RegisterQuestItem(Item182215399, QuestId);
        foreach (int npc in new[] { KutosNpc, GarnonNpc, OriataNpc, YaciNpc, TriggerNpc, ManyosNpc, SuttonNpc })
            engine.RegisterQuestNpc(npc).OnTalk.Add(QuestId);
        foreach (int mob in _kaidanPairMobs) engine.RegisterQuestNpc(mob).OnKill.Add(QuestId);
        engine.RegisterQuestNpc(KaidanScoutMob).OnKill.Add(QuestId);
        foreach (int mob in _kaidanEliteMobs) engine.RegisterQuestNpc(mob).OnKill.Add(QuestId);
    }

    public override ValueTask<bool> OnZoneMissionEndAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
        => DefaultOnZoneMissionEndEventAsync(env, conn, ct);

    public override ValueTask<bool> OnLevelUpAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
        => DefaultOnLvlUpEventAsync(env, conn, precedingQuestId: 14070, isZoneMission: true, ct);

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
            if (targetId == KutosNpc)
            {
                if (dialog == DialogAction.QUEST_SELECT)
                    return var == 0 && await SendQuestDialogAsync(conn, targetObjId, 1011, ct);
                if (dialog == DialogAction.SETPRO1)
                    return await DefaultCloseDialogAsync(env, conn, 0, 1, ct);
                return false;
            }
            if (targetId == GarnonNpc)
            {
                int var3 = entry.GetVar(3);
                if (dialog == DialogAction.QUEST_SELECT)
                {
                    if (var == 1) return await SendQuestDialogAsync(conn, targetObjId, 1352, ct);
                    if (var == 7 && var3 == 6) return await SendQuestDialogAsync(conn, targetObjId, 3398, ct);
                    return false;
                }
                if (dialog == DialogAction.SETPRO2)
                {
                    // Skip: Java teleports the player to world 600020000 here via TeleportService2 (not ported).
                    return await DefaultCloseDialogAsync(env, conn, 1, 2, ct);
                }
                if (dialog == DialogAction.SETPRO8)
                {
                    // Skip: Java teleports the player to world 600020000 here via TeleportService2 (not ported).
                    return await DefaultCloseDialogAsync(env, conn, 7, 8, ct);
                }
                return false;
            }
            if (targetId == OriataNpc)
            {
                if (dialog == DialogAction.QUEST_SELECT)
                {
                    if (var == 2)
                    {
                        await PlayQuestMovieAsync(conn, player, 889, ct);
                        return await SendQuestDialogAsync(conn, targetObjId, 1693, ct);
                    }
                    if (var == 8) return await SendQuestDialogAsync(conn, targetObjId, 3740, ct);
                    return false;
                }
                if (dialog == DialogAction.SETPRO3)
                    return await DefaultCloseDialogAsync(env, conn, 2, 3, ct);
                if (dialog == DialogAction.SETPRO9)
                {
                    await GiveQuestItemAsync(player, conn, _itemDao, Item182215399, 1, ct);
                    return await DefaultCloseDialogAsync(env, conn, 8, 9, ct);
                }
                return false;
            }
            if (targetId == YaciNpc)
            {
                if (dialog == DialogAction.QUEST_SELECT)
                    return var == 3 && await SendQuestDialogAsync(conn, targetObjId, 2034, ct);
                if (dialog == DialogAction.SETPRO4)
                    return await DefaultCloseDialogAsync(env, conn, 3, 4, ct);
                return false;
            }
            if (targetId == TriggerNpc)
                return true;
            if (targetId == ManyosNpc)
            {
                if (dialog == DialogAction.QUEST_SELECT)
                {
                    if (var == 4) return await SendQuestDialogAsync(conn, targetObjId, 2375, ct);
                    if (var == 5) return await SendQuestDialogAsync(conn, targetObjId, 2716, ct);
                    return false;
                }
                if (dialog == DialogAction.CHECK_USER_HAS_QUEST_ITEM)
                    return await CheckQuestItemsAsync(env, conn, _itemDao, 4, 5, reward: false, checkOkId: 10000, checkFailId: 10001, ct);
                if (dialog == DialogAction.SETPRO6)
                    return await DefaultCloseDialogAsync(env, conn, 5, 6, ct);
                return false;
            }
            if (targetId == SuttonNpc)
            {
                int var1 = entry.GetVar(1);
                int var2 = entry.GetVar(2);
                if (dialog == DialogAction.QUEST_SELECT)
                    return var == 6 && var1 == 5 && var2 == 3 && await SendQuestDialogAsync(conn, targetObjId, 3057, ct);
                if (dialog == DialogAction.SETPRO7)
                    return await DefaultCloseDialogAsync(env, conn, 6, 7, ct);
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
        int var      = entry.GetVar(0);

        if (targetId == 217912 || targetId == 217913)
        {
            if (var == 6 && entry.GetVar(1) < 5)
            {
                await ChangeQuestStepAsync(conn, entry, 1, entry.GetVar(1) + 1, toReward: false, ct);
                return true;
            }
        }
        else if (targetId == KaidanScoutMob)
        {
            if (var == 6 && entry.GetVar(2) < 3)
            {
                await ChangeQuestStepAsync(conn, entry, 2, entry.GetVar(2) + 1, toReward: false, ct);
                return true;
            }
        }
        else if (targetId == 218098 || targetId == 218100 || targetId == 218578)
        {
            if (var == 7 && entry.GetVar(3) < 6)
            {
                await ChangeQuestStepAsync(conn, entry, 3, entry.GetVar(3) + 1, toReward: false, ct);
                return true;
            }
        }
        return false;
    }

    public override async ValueTask<bool> OnItemUseAsync(Player player, int itemId, GsClientConnection conn, CancellationToken ct)
    {
        if (!player.CurrentZones.Contains(ItemUseAreaZone)) return false;

        var env = new QuestEnv(null, player, QuestId, 0);
        return await UseQuestObjectAsync(env, conn, 9, 9, reward: true, varNum: 0,
            addItemId: 0, addItemCount: 0, removeItemId: itemId, removeItemCount: 1,
            movieId: 890, dieObject: false, _itemDao, ct);
    }

    public override async ValueTask<bool> OnEnterWorldAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var player = env.Player;
        var entry  = player.Quests.Get(QuestId);
        if (entry is null || entry.Status != QuestStatus.START) return false;
        if (player.Position.WorldId != 300330000) return false;

        int var = entry.GetVar(0);
        int instanceId = player.Position.InstanceId;
        if (var == 2)
        {
            SpawnQuestNpc(300330000, instanceId, SpawnedNpc, 250.331f, 245.210f, 126.270f, 60);
            SpawnQuestNpc(300330000, instanceId, OriataNpc, 249.11f, 248.15f, 125.06f, 70);
        }
        else if (var == 8 || var == 9)
        {
            SpawnQuestNpc(300330000, instanceId, OriataNpc, 249.11f, 248.15f, 125.06f, 72);
            SpawnQuestNpc(300330000, instanceId, SpawnedNpc, 250.331f, 245.210f, 126.270f, 60);
        }
        return false;
    }
}
