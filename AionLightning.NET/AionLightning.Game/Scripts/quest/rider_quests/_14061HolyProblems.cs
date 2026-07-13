// Port of Java data/scripts/system/handlers/quest/rider_quests/_14061HolyProblems.java (pralinka).
// Theobomos chain (level-up gated on 14060): Telemachus (798927, var0 0->1), Padma (798954, 1->2),
// Manir (799022, var0==2) — solo-only: gives items 182215348+182215349 and enters instance world
// 300190000 (var0 2->3); inside, USE 182215348 (var0 3->4), then USE 182215349 repeatedly counting
// var0 4->24, at var0==23 the last use jumps to var0 24; back at Manir CHECK_USER_HAS_QUEST_ITEM
// consumes both items and collect-checks (var0 24->25), Padma SET_SUCCEED (var0 25 flips REWARD).
// Turn in at Telemachus. Dying at var0 3/4 (or 25 without the reward item) resets to var0 2.
// Instance entry uses EnterInstanceAsync; player-death revert uses OnDieAsync.
// Java bug fixed: onEnterWorldEvent's reset guard was `var >= 3 || var <= 24 || (...)`, an always-
// true tautology (every int satisfies one of the two ranges). The same quest's onDieEvent uses the
// correct discrete form (var == 3 || var == 4 || (var == 25 && ...)), confirming the intent is
// "reset only while inside the instance progression", so the enter-world guard is fixed to
// `(var >= 3 && var <= 24) || (var == 25 && rewardItem < 1)`.
// Skip vs Java: group-only branch keeps Java's sendQuestDialog(2546) "solo only" path unchanged.
using System.Threading;
using System.Threading.Tasks;
using AionLightning.Game.Dao;
using AionLightning.Game.DataHolders;
using AionLightning.Game.Model;
using AionLightning.Game.Model.Quest;
using AionLightning.Game.Network.Aion;
using AionLightning.Game.Network.Aion.ServerPackets;
using AionLightning.Game.QuestEngine;
using AionLightning.Game.QuestEngine.Handlers;
using AionLightning.Game.QuestEngine.Model;
using AionLightning.Game.Services;

namespace Quest.RiderQuests;

public sealed class _14061HolyProblems : QuestHandlerBase
{
    private const int QuestIdConst   = 14061;
    private const int TelemachusNpc  = 798927;
    private const int PadmaNpc       = 798954;
    private const int ManirNpc       = 799022;
    private const int Item1          = 182215348;
    private const int Item2          = 182215349;
    private const int RewardItem     = 182215346;
    private const int InstanceWorld  = 300190000;

    private readonly IItemDao _itemDao;

    public _14061HolyProblems(IDataManager dataManager, IQuestDao questDao, QuestRewardService rewardService, IItemDao itemDao)
        : base(QuestIdConst, dataManager, questDao, rewardService)
    {
        _itemDao = itemDao;
    }

    public override void Register(QuestEngine engine)
    {
        engine.RegisterOnZoneMissionEnd(QuestId);
        engine.RegisterOnLevelUp(QuestId);
        engine.RegisterOnEnterWorld(QuestId);
        RegisterOnDie(engine);
        foreach (int npc in new[] { TelemachusNpc, PadmaNpc, ManirNpc })
            engine.RegisterQuestNpc(npc).OnTalk.Add(QuestId);
        engine.RegisterQuestItem(Item1, QuestId);
        engine.RegisterQuestItem(Item2, QuestId);
    }

    public override ValueTask<bool> OnZoneMissionEndAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
        => DefaultOnZoneMissionEndEventAsync(env, conn, ct);

    public override ValueTask<bool> OnLevelUpAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
        => DefaultOnLvlUpEventAsync(env, conn, 14060, isZoneMission: true, ct);

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
            if (targetId == TelemachusNpc)
            {
                if (dialog == DialogAction.QUEST_SELECT && var == 0) return await SendQuestDialogAsync(conn, targetObjId, 1011, ct);
                if (dialog == DialogAction.SETPRO1) return await DefaultCloseDialogAsync(env, conn, 0, 1, ct);
                return false;
            }
            if (targetId == PadmaNpc)
            {
                if (dialog == DialogAction.QUEST_SELECT)
                {
                    if (var == 1) return await SendQuestDialogAsync(conn, targetObjId, 1352, ct);
                    if (var == 25) return await SendQuestDialogAsync(conn, targetObjId, 3057, ct);
                    return false;
                }
                if (dialog == DialogAction.SETPRO2) return await DefaultCloseDialogAsync(env, conn, 1, 2, ct);
                if (dialog == DialogAction.SET_SUCCEED) return await DefaultCloseDialogAsync(env, conn, 25, 25, reward: true, sameNpc: false, ct);
                return false;
            }
            if (targetId == ManirNpc)
            {
                if (dialog == DialogAction.QUEST_SELECT)
                {
                    if (var == 2) return await SendQuestDialogAsync(conn, targetObjId, 1693, ct);
                    if (var == 24) return await SendQuestDialogAsync(conn, targetObjId, 2716, ct);
                    return false;
                }
                if (dialog == DialogAction.SETPRO3 && var == 2)
                {
                    if (player.Group is not null)
                        return await SendQuestDialogAsync(conn, targetObjId, 2546, ct);
                    if (await GiveQuestItemAsync(player, conn, _itemDao, Item1, 1, ct)
                        && await GiveQuestItemAsync(player, conn, _itemDao, Item2, 1, ct))
                    {
                        await EnterInstanceAsync(player, conn, InstanceWorld, 202.26694f, 226.0532f, 1098.236f, 30, ct);
                        await ChangeQuestStepAsync(conn, entry, 0, 3, toReward: false, ct);
                        return await CloseDialogWindowAsync(conn, targetObjId, ct);
                    }
                    await conn.SendAsync(SM_SYSTEM_MESSAGE.InventoryFull(), ct);
                    return await SendQuestSelectionDialogAsync(conn, targetObjId, ct);
                }
                if (dialog == DialogAction.CHECK_USER_HAS_QUEST_ITEM)
                {
                    await RemoveQuestItemAsync(player, conn, _itemDao, Item1, 1, ct);
                    await RemoveQuestItemAsync(player, conn, _itemDao, Item2, 1, ct);
                    return await CheckQuestItemsAsync(env, conn, _itemDao, 24, 25, reward: false, 10000, 10001, ct);
                }
                if (dialog == DialogAction.FINISH_DIALOG) return await SendQuestSelectionDialogAsync(conn, targetObjId, ct);
                return false;
            }
        }
        else if (entry.Status == QuestStatus.REWARD)
        {
            if (targetId == TelemachusNpc)
            {
                if (dialog == DialogAction.USE_OBJECT) return await SendQuestDialogAsync(conn, targetObjId, 10002, ct);
                return await SendQuestEndDialogAsync(env, conn, ct);
            }
        }
        return false;
    }

    public override async ValueTask<bool> OnItemUseAsync(Player player, int itemId, GsClientConnection conn, CancellationToken ct)
    {
        var entry = player.Quests.Get(QuestId);
        if (entry is null || entry.Status != QuestStatus.START) return false;
        if (player.Position.WorldId != InstanceWorld) return false;

        int var = entry.GetVar(0);
        if (itemId == Item1)
        {
            if (var == 3)
            {
                await ChangeQuestStepAsync(conn, entry, 0, 4, toReward: false, ct);
                return true;
            }
            return false;
        }
        if (itemId == Item2)
        {
            if (var >= 4 && var < 23)
            {
                entry.SetVar(0, var + 1);
                await UpdateQuestStatusAsync(conn, entry, ct);
                return true;
            }
            if (var == 23)
            {
                entry.SetVar(0, 24);
                await UpdateQuestStatusAsync(conn, entry, ct);
                return true;
            }
        }
        return false;
    }

    public override async ValueTask<bool> OnEnterWorldAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var player = env.Player;
        var entry  = player.Quests.Get(QuestId);
        if (entry is null || entry.Status != QuestStatus.START) return false;
        if (player.Position.WorldId == InstanceWorld) return false;

        int var = entry.GetVar(0);
        long rewardCount = player.Inventory.FindByItemId(RewardItem)?.Count ?? 0;
        // Java bug fixed: `var >= 3 || var <= 24 || ...` tautology -> discrete instance-range guard
        if ((var >= 3 && var <= 24) || (var == 25 && rewardCount < 1))
        {
            await RemoveQuestItemAsync(player, conn, _itemDao, Item1, 1, ct);
            await RemoveQuestItemAsync(player, conn, _itemDao, Item2, 1, ct);
            entry.SetVar(0, 2);
            await UpdateQuestStatusAsync(conn, entry, ct);
            return true;
        }
        return false;
    }

    public override async ValueTask<bool> OnDieAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var player = env.Player;
        var entry  = player.Quests.Get(QuestId);
        if (entry is null || entry.Status != QuestStatus.START) return false;

        int var = entry.GetVar(0);
        long rewardCount = player.Inventory.FindByItemId(RewardItem)?.Count ?? 0;
        if (var == 3 || var == 4 || (var == 25 && rewardCount < 1))
        {
            await RemoveQuestItemAsync(player, conn, _itemDao, Item1, 1, ct);
            await RemoveQuestItemAsync(player, conn, _itemDao, Item2, 1, ct);
            entry.SetVar(0, 2);
            await UpdateQuestStatusAsync(conn, entry, ct);
            return true;
        }
        return false;
    }
}
