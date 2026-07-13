// Port of Java data/scripts/system/handlers/quest/rider_quests/_24061UntanglingTalocsHollow.java (pralinka).
// Brusthonin chain (level-up gated on 24060): Vasharti (799226, var0 0->1), Kaie (799247, 1->2),
// Ophisha (799250, 2->3), Zenoa (799325, var0==3) — solo-only: gives items 182215381+182215382 and
// enters instance world 300190000 (var0 3->4); inside, USE 182215382 (var0 4->5), then USE 182215381
// repeatedly counting var3 0->19 (var0 stays 5), at var3==19 the last use sets var0 6; kill 215488
// (var0 6->7); Senchindi (799503, var0==7, var0 7->8); Rhelien (799239, var0==8) SET_SUCCEED flips to
// REWARD. Turn in at Vasharti. Dying at var0 4/5 (or 26 without the reward item) resets to var0 3.
// Instance entry uses EnterInstanceAsync; player-death revert uses OnDieAsync.
// Java bug fixed: onEnterWorldEvent's reset guard was `var >= 4 || var <= 25 || (...)`, an always-
// true tautology. The same quest's onDieEvent uses the correct discrete form (var == 4 || var == 5 ||
// (var == 26 && ...)), so the enter-world guard is fixed to `(var >= 4 && var <= 25) || (var == 26 &&
// rewardItem < 1)`, matching the "reset only inside the instance progression" intent.
// Skip vs Java: Senchindi's QUEST_SELECT TeleportService2 relocation to Brusthonin (220070000)
// dropped — non-entry relocation, the var0 7->8 transition is kept.
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

public sealed class _24061UntanglingTalocsHollow : QuestHandlerBase
{
    private const int QuestIdConst = 24061;
    private const int VashartiNpc  = 799226;
    private const int KaieNpc      = 799247;
    private const int OphishaNpc   = 799250;
    private const int ZenoaNpc     = 799325;
    private const int SenchindiNpc = 799503;
    private const int RhelienNpc   = 799239;
    private const int KillMob      = 215488;
    private const int Item381      = 182215381;
    private const int Item382      = 182215382;
    private const int RewardItem   = 182215346;
    private const int InstanceWorld = 300190000;

    private readonly IItemDao _itemDao;

    public _24061UntanglingTalocsHollow(IDataManager dataManager, IQuestDao questDao, QuestRewardService rewardService, IItemDao itemDao)
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
        foreach (int npc in new[] { VashartiNpc, KaieNpc, OphishaNpc, ZenoaNpc, SenchindiNpc, RhelienNpc })
            engine.RegisterQuestNpc(npc).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(KillMob).OnKill.Add(QuestId);
        engine.RegisterQuestItem(Item381, QuestId);
        engine.RegisterQuestItem(Item382, QuestId);
    }

    public override ValueTask<bool> OnZoneMissionEndAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
        => DefaultOnZoneMissionEndEventAsync(env, conn, ct);

    public override ValueTask<bool> OnLevelUpAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
        => DefaultOnLvlUpEventAsync(env, conn, 24060, isZoneMission: true, ct);

    public override ValueTask<bool> OnKillAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
        => DefaultOnKillEventAsync(env, conn, KillMob, 6, 7, ct);

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
            if (targetId == VashartiNpc)
            {
                if (dialog == DialogAction.QUEST_SELECT && var == 0) return await SendQuestDialogAsync(conn, targetObjId, 1011, ct);
                if (dialog == DialogAction.SETPRO1) return await DefaultCloseDialogAsync(env, conn, 0, 1, ct);
                return false;
            }
            if (targetId == KaieNpc)
            {
                if (dialog == DialogAction.QUEST_SELECT && var == 1) return await SendQuestDialogAsync(conn, targetObjId, 1352, ct);
                if (dialog == DialogAction.SETPRO2) return await DefaultCloseDialogAsync(env, conn, 1, 2, ct);
                return false;
            }
            if (targetId == OphishaNpc)
            {
                if (dialog == DialogAction.QUEST_SELECT && var == 2) return await SendQuestDialogAsync(conn, targetObjId, 1693, ct);
                if (dialog == DialogAction.SETPRO3) return await DefaultCloseDialogAsync(env, conn, 2, 3, ct);
                return false;
            }
            if (targetId == ZenoaNpc)
            {
                if (dialog == DialogAction.QUEST_SELECT && var == 3) return await SendQuestDialogAsync(conn, targetObjId, 2034, ct);
                if (dialog == DialogAction.SETPRO4 && var == 3)
                {
                    if (player.Group is not null)
                        return await SendQuestDialogAsync(conn, targetObjId, 2546, ct);
                    if (await GiveQuestItemAsync(player, conn, _itemDao, Item381, 1, ct)
                        && await GiveQuestItemAsync(player, conn, _itemDao, Item382, 1, ct))
                    {
                        await EnterInstanceAsync(player, conn, InstanceWorld, 202.26694f, 226.0532f, 1098.236f, 30, ct);
                        await ChangeQuestStepAsync(conn, entry, 0, 4, toReward: false, ct);
                        return await CloseDialogWindowAsync(conn, targetObjId, ct);
                    }
                    await conn.SendAsync(SM_SYSTEM_MESSAGE.InventoryFull(), ct);
                    return await SendQuestSelectionDialogAsync(conn, targetObjId, ct);
                }
                if (dialog == DialogAction.FINISH_DIALOG) return await SendQuestSelectionDialogAsync(conn, targetObjId, ct);
                return false;
            }
            if (targetId == SenchindiNpc)
            {
                if (dialog == DialogAction.QUEST_SELECT && var == 7)
                {
                    // note: TeleportService2 relocation to Brusthonin (220070000) dropped — non-entry relocation; var0 7->8 kept
                    await ChangeQuestStepAsync(conn, entry, 0, 8, toReward: false, ct);
                    return await CloseDialogWindowAsync(conn, targetObjId, ct);
                }
                return false;
            }
            if (targetId == RhelienNpc)
            {
                if (dialog == DialogAction.QUEST_SELECT && var == 8) return await SendQuestDialogAsync(conn, targetObjId, 1608, ct);
                if (dialog == DialogAction.SET_SUCCEED) return await DefaultCloseDialogAsync(env, conn, 8, 8, reward: true, sameNpc: false, ct);
                return false;
            }
        }
        else if (entry.Status == QuestStatus.REWARD)
        {
            if (targetId == VashartiNpc)
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

        int var  = entry.GetVar(0);
        int var3 = entry.GetVar(3);
        if (itemId == Item382)
        {
            if (var == 4)
            {
                await ChangeQuestStepAsync(conn, entry, 0, 5, toReward: false, ct);
                return true;
            }
            return false;
        }
        if (itemId == Item381 && var == 5)
        {
            if (var3 >= 0 && var3 < 19)
            {
                await ChangeQuestStepAsync(conn, entry, 3, var3 + 1, toReward: false, ct);
                return true;
            }
            if (var3 == 19)
            {
                entry.SetVar(0, 6);
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
        // Java bug fixed: `var >= 4 || var <= 25 || ...` tautology -> discrete instance-range guard
        if ((var >= 4 && var <= 25) || (var == 26 && rewardCount < 1))
        {
            await RemoveQuestItemAsync(player, conn, _itemDao, Item381, 1, ct);
            await RemoveQuestItemAsync(player, conn, _itemDao, Item382, 1, ct);
            entry.SetVar(0, 3);
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
        if (var == 4 || var == 5 || (var == 26 && rewardCount < 1))
        {
            await RemoveQuestItemAsync(player, conn, _itemDao, Item381, 1, ct);
            await RemoveQuestItemAsync(player, conn, _itemDao, Item382, 1, ct);
            entry.SetVar(0, 3);
            await UpdateQuestStatusAsync(conn, entry, ct);
            return true;
        }
        return false;
    }
}
