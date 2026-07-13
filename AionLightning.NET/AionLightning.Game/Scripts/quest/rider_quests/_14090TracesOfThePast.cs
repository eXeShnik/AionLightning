// Port of Java data/scripts/system/handlers/quest/rider_quests/_14090TracesOfThePast.java (pralinka).
// Tiamaranta chain (level-up gated on 14081): Protector Oriata (802059, var0 0->1 giving item
// 182215408), then USE 182215408 inside LDF4B_ITEMUSEAREA_Q14090 to enter instance world 300490000
// (var0 1->2); entering 300490000 at var0==2 spawns Tiamat's Remains (730889); USE it (var0==2)
// spawns Oriata of the Past (802060) and gives item 182215409 (var0 2->3); Oriata (802060, var0==3)
// enters instance world 300500000 spawning Israphel (802062) + Concentrated Ide Crystal (730890)
// (var0 3->4); Israphel (802062, var0==4, var0 4->5); USE the Ide Crystal (730890, var0==5) spawns
// Oriata of the Past (802061), gives item 182215410, flips to REWARD (var0 5->5 reward). Turn in at
// 802061 (plays movie 892, removes 182215409+182215410). Logging out at var0 <= 5 resets to var0 0.
// Instance entries use EnterInstanceAsync; in-instance spawns use SpawnQuestNpc; logout revert uses
// OnLogOutAsync (persists without packets).
// Skip vs Java: npc.getController().onDelete() despawns of the used objects/NPCs dropped — no
// despawn API for quest scripts (var/status transitions kept).
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

public sealed class _14090TracesOfThePast : QuestHandlerBase
{
    private const int QuestIdConst    = 14090;
    private const int ProtectorOriata = 802059;
    private const int TiamatRemains   = 730889;
    private const int OriataPast60    = 802060;
    private const int OriataPast61    = 802061;
    private const int IsraphelNpc     = 802062;
    private const int IdeCrystal      = 730890;
    private const int StartItem       = 182215408;
    private const int Item409         = 182215409;
    private const int Item410         = 182215410;
    private const int Instance490     = 300490000;
    private const int Instance500     = 300500000;
    private const string ItemUseZone  = "LDF4B_ITEMUSEAREA_Q14090";

    private readonly IItemDao _itemDao;

    public _14090TracesOfThePast(IDataManager dataManager, IQuestDao questDao, QuestRewardService rewardService, IItemDao itemDao)
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
        foreach (int npc in new[] { ProtectorOriata, TiamatRemains, OriataPast60, OriataPast61, IsraphelNpc, IdeCrystal })
            engine.RegisterQuestNpc(npc).OnTalk.Add(QuestId);
        engine.RegisterQuestItem(StartItem, QuestId);
    }

    public override ValueTask<bool> OnZoneMissionEndAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
        => DefaultOnZoneMissionEndEventAsync(env, conn, ct);

    public override ValueTask<bool> OnLevelUpAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
        => DefaultOnLvlUpEventAsync(env, conn, 14081, isZoneMission: true, ct);

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
            if (targetId == ProtectorOriata)
            {
                if (dialog == DialogAction.QUEST_SELECT && var == 0) return await SendQuestDialogAsync(conn, targetObjId, 1011, ct);
                if (dialog == DialogAction.SETPRO1)
                {
                    await GiveQuestItemAsync(player, conn, _itemDao, StartItem, 1, ct);
                    return await DefaultCloseDialogAsync(env, conn, 0, 1, ct);
                }
                return false;
            }
            if (targetId == TiamatRemains)
            {
                if (dialog == DialogAction.USE_OBJECT && var == 2)
                {
                    if ((player.Inventory.FindByItemId(Item409)?.Count ?? 0) == 0
                        && !await GiveQuestItemAsync(player, conn, _itemDao, Item409, 1, ct))
                        return true;
                    SpawnQuestNpc(Instance490, player.Position.InstanceId, OriataPast60, 458f, 514f, 417f, 119);
                    // note: npc.getController().onDelete() despawn dropped — no despawn API
                    return await DefaultCloseDialogAsync(env, conn, 2, 3, ct);
                }
                return false;
            }
            if (targetId == OriataPast60)
            {
                if (dialog == DialogAction.QUEST_SELECT && var == 3) return await SendQuestDialogAsync(conn, targetObjId, 1693, ct);
                if (dialog == DialogAction.SETPRO3)
                {
                    await EnterInstanceAsync(player, conn, Instance500, 224f, 251f, 125f, 10, ct);
                    SpawnQuestNpc(Instance500, player.Position.InstanceId, IsraphelNpc, 257f, 246f, 125f, 60);
                    SpawnQuestNpc(Instance500, player.Position.InstanceId, IdeCrystal, 253f, 245f, 125f, 119);
                    // note: npc.getController().onDelete() despawn dropped — no despawn API
                    return await DefaultCloseDialogAsync(env, conn, 3, 4, ct);
                }
                return false;
            }
            if (targetId == IsraphelNpc)
            {
                if (dialog == DialogAction.QUEST_SELECT && var == 4) return await SendQuestDialogAsync(conn, targetObjId, 2034, ct);
                if (dialog == DialogAction.SETPRO4)
                {
                    // note: npc.getController().onDelete() despawn dropped — no despawn API
                    return await DefaultCloseDialogAsync(env, conn, 4, 5, ct);
                }
                return false;
            }
            if (targetId == IdeCrystal)
            {
                if (dialog == DialogAction.USE_OBJECT && var == 5)
                {
                    if ((player.Inventory.FindByItemId(Item410)?.Count ?? 0) == 0
                        && !await GiveQuestItemAsync(player, conn, _itemDao, Item410, 1, ct))
                        return true;
                    SpawnQuestNpc(Instance500, player.Position.InstanceId, OriataPast61, 255f, 245f, 125f, 55);
                    // note: npc.getController().onDelete() despawn dropped — no despawn API
                    return await DefaultCloseDialogAsync(env, conn, 5, 5, reward: true, sameNpc: false, ct);
                }
                return false;
            }
        }
        else if (entry.Status == QuestStatus.REWARD)
        {
            if (targetId == OriataPast61)
            {
                await PlayQuestMovieAsync(conn, player, 892, ct);
                await RemoveQuestItemAsync(player, conn, _itemDao, Item409, 1, ct);
                await RemoveQuestItemAsync(player, conn, _itemDao, Item410, 1, ct);
                return await SendQuestEndDialogAsync(env, conn, ct);
            }
        }
        return false;
    }

    public override async ValueTask<bool> OnItemUseAsync(Player player, int itemId, GsClientConnection conn, CancellationToken ct)
    {
        var entry = player.Quests.Get(QuestId);
        if (entry is null || entry.Status != QuestStatus.START) return false;
        if (!player.CurrentZones.Contains(ItemUseZone) || itemId != StartItem) return false;

        await EnterInstanceAsync(player, conn, Instance490, 549f, 525f, 417f, 10, ct);
        var env = new QuestEnv(null, player, QuestId, 0);
        return await UseQuestObjectAsync(env, conn, 1, 2, reward: false, dieObject: false, ct);
    }

    public override ValueTask<bool> OnEnterWorldAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var player = env.Player;
        var entry  = player.Quests.Get(QuestId);
        if (entry is null || entry.Status != QuestStatus.START) return ValueTask.FromResult(false);
        if (player.Position.WorldId == Instance490 && entry.GetVar(0) == 2)
            SpawnQuestNpc(Instance490, player.Position.InstanceId, TiamatRemains, 446f, 513f, 418f, 119);
        return ValueTask.FromResult(false);
    }

    public override async ValueTask<bool> OnLogOutAsync(QuestEnv env, GsClientConnection? conn, CancellationToken ct)
    {
        var player = env.Player;
        var entry  = player.Quests.Get(QuestId);
        if (entry is null || entry.Status != QuestStatus.START) return false;
        if (entry.GetVar(0) <= 5)
        {
            entry.SetVar(0, 0);
            await QuestDao.UpsertAsync(player.ObjectId, entry, ct); // logout hook: mutate + persist only, no packets
            return true;
        }
        return false;
    }
}
