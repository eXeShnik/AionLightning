// Port of Java data/scripts/system/handlers/quest/gelkmaros/_20022SpreadingAsmodaesReach.java (Gigi).
// Valetta (799226, var0->1) -> field officer (799282: var1->2 relay, CHECK_USER_HAS_QUEST_ITEM
// consumes 10x each of 182207605/606/607 and jumps var->3, var4/260->5 gives 182207608x2,
// var7->8 gives 182207609x1, SET_SUCCEED var9->reward) -> two loot props (700704/700703, var==2
// click no-ops) -> two craft props (700701 var5->6, 700702 var6->7, each consuming one
// 182207608) -> kill 2 of {216102,216103} while var0==3 to bump var1 up to 4 then flip var0->4 ->
// use item 182207609 while var0==8 and standing within 10 units of the ritual site to finish
// (movie 553); too far away is a soft no-op (Java's SM_SYSTEM_MESSAGE(1300426) toast is dropped —
// no generic arbitrary-code system-message sender exists in this port, only named factories).
// Java dead branch kept as-is: "var == 4 || var == 260" — var is clamped 0-63, so 260 is
// unreachable; harmless leftover from the original script.
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

namespace Quest.Gelkmaros;

public sealed class _20022SpreadingAsmodaesReach : QuestHandlerBase
{
    private const int QuestIdConst  = 20022;
    private const int ValettaNpc    = 799226;
    private const int FieldNpc      = 799282;
    private const int LootObject1   = 700704;
    private const int LootObject2   = 700703;
    private const int CraftObject1  = 700701;
    private const int CraftObject2  = 700702;
    private const int MobA          = 216102;
    private const int MobB          = 216103;
    private const int CollectItem1  = 182207605;
    private const int CollectItem2  = 182207606;
    private const int CollectItem3  = 182207607;
    private const int MaterialItem  = 182207608;
    private const int RitualItem    = 182207609;
    private const int GelkmarosWorldId = 220070000;
    private static readonly int[] _talkNpcs = [ValettaNpc, FieldNpc, LootObject1, LootObject2, CraftObject1, CraftObject2];
    private static readonly int[] _mobs     = [MobA, MobB];

    private readonly IItemDao _itemDao;

    public _20022SpreadingAsmodaesReach(IDataManager dataManager, IQuestDao questDao, QuestRewardService rewardService, IItemDao itemDao)
        : base(QuestIdConst, dataManager, questDao, rewardService)
    {
        _itemDao = itemDao;
    }

    public override void Register(QuestEngine engine)
    {
        engine.RegisterOnZoneMissionEnd(QuestId);
        engine.RegisterOnLevelUp(QuestId);
        foreach (int mob in _mobs) engine.RegisterQuestNpc(mob).OnKill.Add(QuestId);
        foreach (int npc in _talkNpcs) engine.RegisterQuestNpc(npc).OnTalk.Add(QuestId);
        engine.RegisterQuestItem(RitualItem, QuestId);
    }

    public override ValueTask<bool> OnZoneMissionEndAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
        => DefaultOnZoneMissionEndEventAsync(env, conn, ct);

    public override ValueTask<bool> OnLevelUpAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
        => DefaultOnLvlUpEventAsync(env, conn, precedingQuestId: 20000, isZoneMission: true, ct);

    public override async ValueTask<bool> OnDialogAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var player = env.Player;
        var entry  = player.Quests.Get(QuestId);
        if (entry is null) return false;

        var dialog = DialogActionLookup.FromId(env.DialogId);
        int targetId    = env.TargetId;
        int targetObjId = env.Target?.ObjectId ?? 0;

        if (entry.Status == QuestStatus.START)
        {
            int var = entry.GetVar(0);

            if (targetId == ValettaNpc)
            {
                if (dialog == DialogAction.QUEST_SELECT && var == 0)
                    return await SendQuestDialogAsync(conn, targetObjId, 1011, ct);
                if (dialog == DialogAction.SETPRO1)
                    return await DefaultCloseDialogAsync(env, conn, 0, 1, ct);
                return false;
            }

            if (targetId == FieldNpc)
            {
                if (dialog == DialogAction.QUEST_SELECT)
                {
                    if (var == 1) return await SendQuestDialogAsync(conn, targetObjId, 1352, ct);
                    if (var == 2)
                    {
                        long c1 = player.Inventory.FindByItemId(CollectItem1)?.Count ?? 0;
                        long c2 = player.Inventory.FindByItemId(CollectItem2)?.Count ?? 0;
                        long c3 = player.Inventory.FindByItemId(CollectItem3)?.Count ?? 0;
                        return c1 > 4 && c2 > 4 && c3 > 4
                            ? await SendQuestDialogAsync(conn, targetObjId, 1693, ct)
                            : await SendQuestDialogAsync(conn, targetObjId, 10001, ct);
                    }
                    if (var == 3) return await SendQuestDialogAsync(conn, targetObjId, 10000, ct);
                    if (var == 4 || var == 260) return await SendQuestDialogAsync(conn, targetObjId, 2375, ct);
                    if (var == 7) return await SendQuestDialogAsync(conn, targetObjId, 3398, ct);
                    if (var == 9) return await SendQuestDialogAsync(conn, targetObjId, 4080, ct);
                    return false;
                }
                if (dialog == DialogAction.SETPRO2)
                    return await DefaultCloseDialogAsync(env, conn, 1, 2, ct);
                if (dialog == DialogAction.CHECK_USER_HAS_QUEST_ITEM)
                {
                    await ChangeQuestStepAsync(conn, entry, 0, 3, toReward: false, ct);
                    await RemoveQuestItemAsync(player, conn, _itemDao, CollectItem1, 10, ct);
                    await RemoveQuestItemAsync(player, conn, _itemDao, CollectItem2, 10, ct);
                    await RemoveQuestItemAsync(player, conn, _itemDao, CollectItem3, 10, ct);
                    return await SendQuestSelectionDialogAsync(conn, targetObjId, ct);
                }
                if (dialog == DialogAction.SETPRO5)
                    return await DefaultCloseDialogAsync(env, conn, _itemDao, 4, 5, reward: false, sameNpc: false,
                        MaterialItem, 2, 0, 0, ct);
                if (dialog == DialogAction.SETPRO8)
                    return await DefaultCloseDialogAsync(env, conn, _itemDao, 7, 8, reward: false, sameNpc: false,
                        RitualItem, 1, 0, 0, ct);
                if (dialog == DialogAction.SET_SUCCEED)
                    return await DefaultCloseDialogAsync(env, conn, 9, 9, reward: true, sameNpc: false, ct);
                return false;
            }

            if (targetId == LootObject1 || targetId == LootObject2)
                return var == 2 && dialog == DialogAction.USE_OBJECT; // Java: click handled, no state change

            if (targetId == CraftObject1)
            {
                if (dialog == DialogAction.USE_OBJECT)
                    return await UseQuestObjectAsync(env, conn, 5, 6, reward: false, varNum: 0,
                        addItemId: 0, addItemCount: 0, removeItemId: MaterialItem, removeItemCount: 1,
                        movieId: 0, dieObject: false, _itemDao, ct);
                return false;
            }

            if (targetId == CraftObject2)
            {
                if (dialog == DialogAction.USE_OBJECT)
                    return await UseQuestObjectAsync(env, conn, 6, 7, reward: false, varNum: 0,
                        addItemId: 0, addItemCount: 0, removeItemId: MaterialItem, removeItemCount: 1,
                        movieId: 0, dieObject: false, _itemDao, ct);
                return false;
            }
        }
        else if (entry.Status == QuestStatus.REWARD)
        {
            if (targetId == ValettaNpc)
            {
                if (dialog == DialogAction.QUEST_SELECT)
                    return await SendQuestDialogAsync(conn, targetObjId, 10002, ct);
                return await SendQuestEndDialogAsync(env, conn, ct);
            }
        }
        return false;
    }

    public override async ValueTask<bool> OnKillAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var entry = env.Player.Quests.Get(QuestId);
        if (entry is null || entry.Status != QuestStatus.START) return false;
        if (env.TargetId != MobA && env.TargetId != MobB) return false;

        int var  = entry.GetVar(0);
        int var1 = entry.GetVar(1);
        if (var == 3 && var1 < 4)
        {
            entry.SetVar(1, var1 + 1);
            await UpdateQuestStatusAsync(conn, entry, ct);
        }
        else if (var == 3 && var1 == 4)
        {
            entry.SetVar(0, 4);
            await UpdateQuestStatusAsync(conn, entry, ct);
        }
        return false; // Java onKillEvent always returns false regardless of the state change above
    }

    public override async ValueTask<bool> OnItemUseAsync(Player player, int itemId, GsClientConnection conn, CancellationToken ct)
    {
        if (player.Position.WorldId != GelkmarosWorldId) return false;

        var entry = player.Quests.Get(QuestId);
        if (entry is null) return false;
        if (itemId != RitualItem || entry.GetVar(0) != 8) return false;

        const float centerX = 285.17746f, centerY = 1534.7837f, centerZ = 356.52f, radius = 10f;
        float dx = player.Position.X - centerX, dy = player.Position.Y - centerY, dz = player.Position.Z - centerZ;
        if (dx * dx + dy * dy + dz * dz < radius * radius)
        {
            var env = new QuestEnv(null, player, QuestId, 0);
            return await UseQuestObjectAsync(env, conn, 8, 9, reward: false, varNum: 0,
                addItemId: 0, addItemCount: 0, removeItemId: itemId, removeItemCount: 1,
                movieId: 553, dieObject: false, _itemDao, ct);
        }

        // Too far from the ritual site: Java shows SM_SYSTEM_MESSAGE(1300426) here and treats the
        // interaction as handled without consuming the item — the toast is dropped (see file header).
        return true;
    }
}
