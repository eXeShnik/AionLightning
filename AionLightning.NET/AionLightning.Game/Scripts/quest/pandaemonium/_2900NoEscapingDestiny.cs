// Port of Java data/scripts/system/handlers/quest/pandaemonium/_2900NoEscapingDestiny.java
// (Mr. Poke, rew. vlog, mod. Rolandas/apozema).
// Norn-sisters chain: Heimdall (204182) SETPRO1 var 0->1; Munin (203550) SETPRO2 var 1->2;
// Urd (790003) SETPRO3 var 2->3; Verdandi (790002) SETPRO4 var 3->4; Skuld (203546) SETPRO5 at
// var4 -> var 95 and enters the solo instance 320070000. Inside, Skuld (204264): SETPRO6 plays
// movie 156 (-> var 96 on movie-end), SELECT_ACTION_3058 gives the class stigma stone + 60 stigma
// shards (var 96->99), SETPRO7 opens the equip window, equipping the stone -> var 97, SETPRO8 var
// 97->98 and spawns the boss 204263; killing it -> var 9. Back outside, Skuld SETPRO9 var 9->10,
// Munin SETPRO10 -> REWARD; turn in at Aud (204061). Dying / leaving the instance while var 95..99
// fails the quest back to var 4 (removing the granted stigma).
// note: the TeleportService2 relocations (Heimdall SETPRO1, Munin SETPRO10, Skuld SETPRO9, boss-kill
//   exit) are cosmetic hops that do not gate progression — dropped; the var/status transitions stay.
// note: the QUEST_FAILED_$1 system message on the die/enter-world reset is a cosmetic notice — dropped.
// note: removeStigma only clears the granted stone's item state (unequip + remove) here; Java's full
//   stigma-skill removal on unequip is not replicated (matches the SetPlayerClass "not fully wired"
//   precedent) — used only on the fail-reset path.
using System.Threading;
using System.Threading.Tasks;
using System.Linq;
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

namespace Quest.Pandaemonium;

public sealed class _2900NoEscapingDestiny : QuestHandlerBase
{
    private const int QuestIdConst = 2900;
    private const int Heimdall  = 204182;
    private const int Munin     = 203550;
    private const int Urd       = 790003;
    private const int Verdandi  = 790002;
    private const int Skuld     = 203546;
    private const int SkuldInst = 204264;
    private const int Aud       = 204061;
    private const int Boss      = 204263;
    private const int InstanceWorld = 320070000;
    private const int ShardItem     = 141000001;

    private static readonly int[] Stigmas =
    {
        140000008, 140000027, 140000047, 140000076, 140000131, 140000147,
        140000098, 140000112, 140000859, 140000943, 140001002
    };

    private readonly IItemDao _itemDao;

    public _2900NoEscapingDestiny(IDataManager dataManager, IQuestDao questDao, QuestRewardService rewardService, IItemDao itemDao)
        : base(QuestIdConst, dataManager, questDao, rewardService)
    {
        _itemDao = itemDao;
    }

    public override void Register(QuestEngine engine)
    {
        engine.RegisterOnLevelUp(QuestId);
        engine.RegisterOnQuestMovieEnd(156, QuestId);
        engine.RegisterQuestNpc(Boss).OnKill.Add(QuestId);
        engine.RegisterOnEnterWorld(QuestId);
        RegisterOnDie(engine);
        foreach (int npc in new[] { Heimdall, Munin, Urd, Verdandi, Skuld, SkuldInst, Aud })
            engine.RegisterQuestNpc(npc).OnTalk.Add(QuestId);
        foreach (int stigma in Stigmas)
            RegisterOnEquipItem(engine, stigma);
    }

    public override async ValueTask<bool> OnDialogAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var player = env.Player;
        var entry  = player.Quests.Get(QuestId);
        if (entry is null) return false;

        int var         = entry.GetVar(0);
        int targetId    = env.TargetId;
        int targetObjId = env.Target?.ObjectId ?? 0;
        var dialog      = DialogActionLookup.FromId(env.DialogId);

        if (entry.Status == QuestStatus.START)
        {
            switch (targetId)
            {
                case Heimdall:
                    // Java switch fallthrough: QUEST_SELECT (no break) -> SETPRO1.
                    if (dialog == DialogAction.QUEST_SELECT && var == 0)
                        return await SendQuestDialogAsync(conn, targetObjId, 1011, ct);
                    if (dialog == DialogAction.QUEST_SELECT || dialog == DialogAction.SETPRO1)
                        // note: Java teleport to 220010000 dropped (cosmetic).
                        return await DefaultCloseDialogAsync(env, conn, 0, 1, ct);
                    return false;

                case Munin:
                    // Java switch fallthrough: QUEST_SELECT -> SETPRO2 (SETPRO2 returns, so no reach to SETPRO10).
                    if (dialog == DialogAction.QUEST_SELECT && var == 1)
                        return await SendQuestDialogAsync(conn, targetObjId, 1352, ct);
                    if (dialog == DialogAction.QUEST_SELECT && var == 10)
                        return await SendQuestDialogAsync(conn, targetObjId, 4080, ct);
                    if (dialog == DialogAction.QUEST_SELECT || dialog == DialogAction.SETPRO2)
                        return await DefaultCloseDialogAsync(env, conn, 1, 2, ct);
                    if (dialog == DialogAction.SETPRO10)
                        // note: Java teleport to 120010000 dropped (cosmetic).
                        return await DefaultCloseDialogAsync(env, conn, 10, 10, reward: true, sameNpc: false, ct);
                    return false;

                case Urd:
                    if (dialog == DialogAction.QUEST_SELECT && var == 2)
                        return await SendQuestDialogAsync(conn, targetObjId, 1693, ct);
                    if (dialog == DialogAction.QUEST_SELECT || dialog == DialogAction.SETPRO3)
                        return await DefaultCloseDialogAsync(env, conn, 2, 3, ct);
                    return false;

                case Verdandi:
                    if (dialog == DialogAction.QUEST_SELECT && var == 3)
                        return await SendQuestDialogAsync(conn, targetObjId, 2034, ct);
                    if (dialog == DialogAction.QUEST_SELECT || dialog == DialogAction.SETPRO4)
                        return await DefaultCloseDialogAsync(env, conn, 3, 4, ct);
                    return false;

                case Skuld:
                    if (dialog == DialogAction.QUEST_SELECT && var == 4)
                        return await SendQuestDialogAsync(conn, targetObjId, 2375, ct);
                    if (dialog == DialogAction.QUEST_SELECT && var == 9)
                        return await SendQuestDialogAsync(conn, targetObjId, 3739, ct);
                    if (dialog == DialogAction.SETPRO5 && var == 4)
                    {
                        await ChangeQuestStepAsync(conn, entry, 0, 95, toReward: false, ct);
                        await EnterInstanceAsync(player, conn, InstanceWorld, 253.66963f, 259.2077f, 125.8369f, 87, ct);
                        return await CloseDialogWindowAsync(conn, targetObjId, ct);
                    }
                    // Java bug: SETPRO5 (var != 4) and QUEST_SELECT (var not 4/9) fell through a missing
                    // break into SETPRO9 and set var=10 unconditionally; guarded to var == 9 to stop the misfire.
                    if (dialog == DialogAction.SETPRO9 && var == 9)
                    {
                        // note: Java teleport to 220010000 dropped (cosmetic).
                        return await DefaultCloseDialogAsync(env, conn, 9, 10, ct);
                    }
                    return false;

                case SkuldInst:
                    // Java switch fallthrough chain: USE_OBJECT -> QUEST_SELECT -> SETPRO6 ->
                    // SELECT_ACTION_3058 -> SETPRO7 -> SETPRO8 (each case reachable from the ones above it).
                    if (dialog == DialogAction.USE_OBJECT && var == 99 && !IsStigmaEquipped(player))
                        return await SendQuestDialogAsync(conn, targetObjId, 3057, ct);

                    if (dialog == DialogAction.QUEST_SELECT || dialog == DialogAction.USE_OBJECT)
                    {
                        if (var == 95) return await SendQuestDialogAsync(conn, targetObjId, 2716, ct);
                        if (var == 96) return await SendQuestDialogAsync(conn, targetObjId, 3057, ct);
                        if (var == 97) return await SendQuestDialogAsync(conn, targetObjId, 3398, ct);
                    }

                    if ((dialog == DialogAction.QUEST_SELECT || dialog == DialogAction.USE_OBJECT
                         || dialog == DialogAction.SETPRO6) && var == 95)
                    {
                        await PlayQuestMovieAsync(conn, player, 156, ct);
                        return await CloseDialogWindowAsync(conn, targetObjId, ct);
                    }

                    if (dialog == DialogAction.QUEST_SELECT || dialog == DialogAction.USE_OBJECT
                        || dialog == DialogAction.SETPRO6 || dialog == DialogAction.SELECT_ACTION_3058)
                    {
                        if (var == 96)
                        {
                            if (await GiveQuestItemAsync(player, conn, _itemDao, GetStoneId(player), 1, ct) && !IsStigmaEquipped(player))
                            {
                                long existingShards = player.Inventory.FindByItemId(ShardItem)?.Count ?? 0;
                                if (existingShards < 60)
                                {
                                    if (player.Inventory.HasFreeSlot)
                                    {
                                        await GiveQuestItemAsync(player, conn, _itemDao, ShardItem, 60, ct);
                                        await ChangeQuestStepAsync(conn, entry, 0, 99, toReward: false, ct);
                                        return await SendQuestDialogAsync(conn, targetObjId, 3058, ct);
                                    }
                                    return await CloseDialogWindowAsync(conn, targetObjId, ct);
                                }
                                await ChangeQuestStepAsync(conn, entry, 0, 99, toReward: false, ct);
                                return await SendQuestDialogAsync(conn, targetObjId, 3058, ct);
                            }
                            return await CloseDialogWindowAsync(conn, targetObjId, ct);
                        }
                        if (var == 99)
                            return await SendQuestDialogAsync(conn, targetObjId, 3058, ct);
                    }

                    if ((dialog == DialogAction.QUEST_SELECT || dialog == DialogAction.USE_OBJECT
                         || dialog == DialogAction.SETPRO6 || dialog == DialogAction.SELECT_ACTION_3058
                         || dialog == DialogAction.SETPRO7) && var == 99)
                    {
                        await conn.SendAsync(new SM_DIALOG_WINDOW(targetObjId, 1), ct);
                        return true;
                    }

                    if ((dialog == DialogAction.QUEST_SELECT || dialog == DialogAction.USE_OBJECT
                         || dialog == DialogAction.SETPRO6 || dialog == DialogAction.SELECT_ACTION_3058
                         || dialog == DialogAction.SETPRO7 || dialog == DialogAction.SETPRO8) && var == 97)
                    {
                        await ChangeQuestStepAsync(conn, entry, 0, 98, toReward: false, ct);
                        SpawnQuestNpc(InstanceWorld, player.Position.InstanceId, Boss, 257.5f, 245f, 125f, 65);
                        return await CloseDialogWindowAsync(conn, targetObjId, ct);
                    }
                    return false;
            }
        }
        else if (entry.Status == QuestStatus.REWARD)
        {
            if (targetId == Aud)
                return await SendQuestEndDialogAsync(env, conn, ct);
        }
        return false;
    }

    public override async ValueTask<bool> OnMovieEndAsync(QuestEnv env, int movieId, GsClientConnection conn, CancellationToken ct)
    {
        if (movieId != 156) return false;
        var entry = env.Player.Quests.Get(QuestId);
        if (entry is null) return false;
        await ChangeQuestStepAsync(conn, entry, 0, 96, toReward: false, ct);
        return true;
    }

    public override async ValueTask<bool> OnEquipItemAsync(QuestEnv env, int itemId, GsClientConnection conn, CancellationToken ct)
    {
        var entry = env.Player.Quests.Get(QuestId);
        if (entry is null) return false;
        // Java changeQuestStep(99, 97) is unconditional; only reachable in normal play at var 99 (SETPRO7 prompt).
        await ChangeQuestStepAsync(conn, entry, 0, 97, toReward: false, ct);
        return await CloseDialogWindowAsync(conn, env.Target?.ObjectId ?? 0, ct);
    }

    public override async ValueTask<bool> OnKillAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var entry = env.Player.Quests.Get(QuestId);
        if (entry is null || entry.Status != QuestStatus.START) return false;
        if (entry.GetVar(0) != 98) return false;

        // note: Java teleport to 220010000 dropped (cosmetic).
        await ChangeQuestStepAsync(conn, entry, 0, 9, toReward: false, ct);
        return true;
    }

    public override async ValueTask<bool> OnDieAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var player = env.Player;
        var entry  = player.Quests.Get(QuestId);
        if (entry is null || entry.Status != QuestStatus.START) return false;
        if (player.Position.WorldId == InstanceWorld) return false;

        int var = entry.GetVar(0);
        if (var >= 95 && var <= 99)
        {
            await RemoveStigmaAsync(player, conn, ct);
            // note: Java QUEST_FAILED_$1 system message dropped (cosmetic notice).
            await ChangeQuestStepAsync(conn, entry, 0, 4, toReward: false, ct);
            return true;
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
        if (var >= 95 && var <= 99)
        {
            await RemoveStigmaAsync(player, conn, ct);
            // note: Java QUEST_FAILED_$1 system message dropped (cosmetic notice).
            await ChangeQuestStepAsync(conn, entry, 0, 4, toReward: false, ct);
            return true;
        }
        if (var == 9)
        {
            await RemoveStigmaAsync(player, conn, ct);
            return true;
        }
        return false;
    }

    public override ValueTask<bool> OnLevelUpAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
        => DefaultOnLvlUpEventAsync(env, conn, ct);

    private static int GetStoneId(Player player) => player.PlayerClass switch
    {
        PlayerClass.GLADIATOR     => 140000008, // Improved Stamina I
        PlayerClass.TEMPLAR       => 140000027, // Divine Fury I
        PlayerClass.RANGER        => 140000047, // Arrow Deluge I
        PlayerClass.ASSASSIN      => 140000076, // Sigil Strike I
        PlayerClass.SORCERER      => 140000131, // Lumiel's Wisdom I
        PlayerClass.SPIRIT_MASTER => 140000147, // Absorb Vitality I
        PlayerClass.CLERIC        => 140000098, // Grace of Empyrean Lord I
        PlayerClass.CHANTER       => 140000112, // Rage Spell I
        PlayerClass.GUNNER        => 140000943, // Nature's Favor I
        PlayerClass.BARD          => 140000859, // Freestyle I
        PlayerClass.RIDER         => 140001002, // Nullification Trigger I
        _                         => 0,
    };

    private bool IsStigmaEquipped(Player player)
    {
        int stoneId = GetStoneId(player);
        return player.Inventory.All.Any(i => i.IsEquipped && i.ItemId == stoneId);
    }

    private async ValueTask RemoveStigmaAsync(Player player, GsClientConnection conn, CancellationToken ct)
    {
        int stoneId = GetStoneId(player);
        // note: Java unEquipItem also strips the granted stigma skills; only the item state is cleared here.
        foreach (var item in player.Inventory.All.Where(i => i.IsEquipped && i.ItemId == stoneId).ToList())
        {
            item.IsEquipped = false;
            item.Slot       = -1;
        }
        await RemoveQuestItemAsync(player, conn, _itemDao, stoneId, 1, ct);
    }
}
