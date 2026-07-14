// Port of Java data/scripts/system/handlers/quest/ascension/_1006Ascension.java (MrPoke, reworked vlog).
// The Elyos class-ascension questline. Pernos (790001) hands the bottle (182200007, var0 0->1); fill it
// at Cliona Lake (item-use in zone LF1_ITEMUSEAREA_Q1006 -> 182200008, var0 1->2); Daminu (730008,
// SETPRO2) plays movie 14, whose end swaps 182200008 for Daminu's Essence 182200009 (var0->3); Pernos
// (SETPRO3, var0 3) opens the Karamatis instance (310020000, var0->99) and consumes 182200009; Belpartan
// (205000, var0 99) starts the encounter (var0->50, a 43s timer spawns four Raiders 211042 and sets
// var0->51); killing the Raiders (var0 51..54) then spawns Orissan 211043 (var0->4); killing Orissan
// (movie 151) spawns Pernos in the instance (var0->5); Pernos then performs the 2nd-class change (SETPRO4
// shows the class sub-dialog, SETPRO5..15 pick the class) and flips REWARD.
//
// New helper used: SetPlayerClass (Java ClassChangeService.setClass) for the 2nd-class change.
//
// Deviations vs Java (state transitions preserved):
//  - Instance creation via EnterInstanceAsync (Java getNextAvailableInstance/registerPlayerWithInstance).
//  - TeleportService2 relocations (SETPRO1 -> Cliona island; movie-14 end -> Cliona; REWARD exit) are
//    dropped (no relocation API); the var transitions are kept so the chain stays completable.
//  - Belpartan's flight-teleport morph (skill 1910 + SM_EMOTION START_FLYTELEPORT) is cosmetic - dropped;
//    the 43s pre-fight delay is preserved via the quest timer.
//  - Spawned-mob aggro (addDamage) and NpcActions.delete corpse cleanup have no API - dropped.
//  - SM_ASCENSION_MORPH (class-morph visual on entering the instance) and the QUEST_FAILED system message
//    are cosmetic packets - dropped.
//  - SetPlayerClass updates the class only; Java's upgradePlayer() stat/skill recompute is not ported.
//  - CustomConfig.ENABLE_SIMPLE_2NDCLASS (Java early-return that disables this handler) is not ported - the
//    handler always registers.
//  - Java's QUEST_SELECT -> SETPROn switch fallthroughs are evaluated independently (per the _1007/_2009
//    ascension-sibling precedent that treats that fallthrough as a Java bug).
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

namespace Quest.Ascension;

public sealed class _1006Ascension : QuestHandlerBase
{
    private const int QuestIdConst  = 1006;
    private const int Pernos        = 790001;
    private const int Daminu        = 730008;
    private const int Belpartan     = 205000;
    private const int Raider        = 211042;
    private const int Orissan       = 211043;
    private const int InstanceWorld = 310020000;
    private const int EmptyBottle   = 182200007;
    private const int FilledBottle  = 182200008;
    private const int DaminuEssence = 182200009;
    private const string ItemUseZone = "LF1_ITEMUSEAREA_Q1006";

    private readonly IItemDao _itemDao;

    public _1006Ascension(IDataManager dataManager, IQuestDao questDao, QuestRewardService rewardService, IItemDao itemDao)
        : base(QuestIdConst, dataManager, questDao, rewardService)
    {
        _itemDao = itemDao;
    }

    public override void Register(QuestEngine engine)
    {
        engine.RegisterOnLevelUp(QuestId);
        engine.RegisterQuestNpc(Raider).OnKill.Add(QuestId);
        engine.RegisterQuestNpc(Orissan).OnKill.Add(QuestId);
        engine.RegisterQuestNpc(Pernos).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(Daminu).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(Belpartan).OnTalk.Add(QuestId);
        engine.RegisterQuestItem(EmptyBottle, QuestId);
        engine.RegisterOnEnterWorld(QuestId);
        RegisterOnDie(engine);
        engine.RegisterOnQuestMovieEnd(14, QuestId);
        engine.RegisterOnQuestMovieEnd(151, QuestId); // registered-but-unhandled in Java too (played on the Orissan kill)
        engine.RegisterOnQuestTimerEnd(QuestId);
    }

    public override ValueTask<bool> OnLevelUpAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
        => DefaultOnLvlUpEventAsync(env, conn, ct);

    public override async ValueTask<bool> OnDialogAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var player      = env.Player;
        var entry       = player.Quests.Get(QuestId);
        if (entry is null) return false;

        int var         = entry.GetVar(0);
        int targetId    = env.TargetId;
        int targetObjId = env.Target?.ObjectId ?? 0;
        var dialog      = DialogActionLookup.FromId(env.DialogId);

        if (entry.Status == QuestStatus.START)
        {
            if (targetId == Pernos)
            {
                if (dialog == DialogAction.QUEST_SELECT)
                {
                    if (var == 0) return await SendQuestDialogAsync(conn, targetObjId, 1011, ct);
                    if (var == 3) return await SendQuestDialogAsync(conn, targetObjId, 1693, ct);
                    if (var == 5) return await SendQuestDialogAsync(conn, targetObjId, 2034, ct);
                    return false;
                }
                if (dialog == DialogAction.SETPRO1)
                {
                    if ((player.Inventory.FindByItemId(EmptyBottle)?.Count ?? 0) == 0)
                        if (!await GiveQuestItemAsync(player, conn, _itemDao, EmptyBottle, 1, ct)) return true;
                    entry.SetVar(0, 1);
                    await UpdateQuestStatusAsync(conn, entry, ct);
                    // note: Java teleports to Cliona Lake island (210010000) - relocation dropped.
                    return true;
                }
                if (dialog == DialogAction.SETPRO3 && var == 3)
                {
                    await EnterInstanceAsync(player, conn, InstanceWorld, 52f, 174f, 229f, 10, ct);
                    entry.SetVar(0, 99);
                    await UpdateQuestStatusAsync(conn, entry, ct);
                    await RemoveQuestItemAsync(player, conn, _itemDao, DaminuEssence, 1, ct);
                    return await CloseDialogWindowAsync(conn, targetObjId, ct);
                }
                // 2nd-class change (var0 == 5). SETPRO4 shows the class sub-dialog; SETPRO5..15 pick the class.
                if (dialog == DialogAction.SETPRO4 && var == 5 && player.PlayerClass.IsStartingClass())
                {
                    int page = player.PlayerClass switch
                    {
                        PlayerClass.WARRIOR  => 2375,
                        PlayerClass.SCOUT    => 2716,
                        PlayerClass.MAGE     => 3057,
                        PlayerClass.PRIEST   => 3398,
                        PlayerClass.ENGINEER => 3739,
                        PlayerClass.ARTIST   => 4080,
                        _                    => 0,
                    };
                    if (page != 0) return await SendQuestDialogAsync(conn, targetObjId, page, ct);
                }
                if (dialog == DialogAction.SETPRO5)  return await SetPlayerClassAsync(env, conn, entry, PlayerClass.GLADIATOR, ct);
                if (dialog == DialogAction.SETPRO6)  return await SetPlayerClassAsync(env, conn, entry, PlayerClass.TEMPLAR, ct);
                if (dialog == DialogAction.SETPRO7)  return await SetPlayerClassAsync(env, conn, entry, PlayerClass.ASSASSIN, ct);
                if (dialog == DialogAction.SETPRO8)  return await SetPlayerClassAsync(env, conn, entry, PlayerClass.RANGER, ct);
                if (dialog == DialogAction.SETPRO9)  return await SetPlayerClassAsync(env, conn, entry, PlayerClass.SORCERER, ct);
                if (dialog == DialogAction.SETPRO10) return await SetPlayerClassAsync(env, conn, entry, PlayerClass.SPIRIT_MASTER, ct);
                if (dialog == DialogAction.SETPRO11) return await SetPlayerClassAsync(env, conn, entry, PlayerClass.CLERIC, ct);
                if (dialog == DialogAction.SETPRO12) return await SetPlayerClassAsync(env, conn, entry, PlayerClass.CHANTER, ct);
                if (dialog == DialogAction.SETPRO13) return await SetPlayerClassAsync(env, conn, entry, PlayerClass.GUNNER, ct);
                if (dialog == DialogAction.SETPRO14) return await SetPlayerClassAsync(env, conn, entry, PlayerClass.BARD, ct);
                if (dialog == DialogAction.SETPRO15) return await SetPlayerClassAsync(env, conn, entry, PlayerClass.RIDER, ct);
                return false;
            }

            if (targetId == Daminu)
            {
                if (dialog == DialogAction.QUEST_SELECT && var == 2 && (player.Inventory.FindByItemId(FilledBottle)?.Count ?? 0) >= 1)
                    return await SendQuestDialogAsync(conn, targetObjId, 1352, ct);
                if (dialog == DialogAction.SETPRO2 && var == 2)
                {
                    await PlayQuestMovieAsync(conn, player, 14, ct);
                    return true;
                }
                return false;
            }

            if (targetId == Belpartan)
            {
                if (dialog == DialogAction.QUEST_SELECT && var == 99)
                {
                    // note: Java applies the flight morph (skill 1910 + SM_EMOTION START_FLYTELEPORT) - cosmetic, dropped.
                    entry.SetVar(0, 50);
                    await UpdateQuestStatusAsync(conn, entry, ct);
                    StartQuestTimer(env, conn, 43);
                    return true;
                }
                return false;
            }

            return false;
        }

        if (entry.Status == QuestStatus.REWARD)
        {
            if (targetId == Pernos)
            {
                if (dialog == DialogAction.SELECTED_QUEST_NOREWARD && player.Position.WorldId == InstanceWorld)
                {
                    // note: Java teleports out of the instance to 210010000 - relocation dropped.
                }
                return await SendQuestEndDialogAsync(env, conn, ct);
            }
        }
        return false;
    }

    private async ValueTask<bool> SetPlayerClassAsync(QuestEnv env, GsClientConnection conn, QuestEntry entry, PlayerClass target, CancellationToken ct)
    {
        var player = env.Player;
        if (!player.PlayerClass.IsStartingClass()) return false;
        SetPlayerClass(player, target);
        // note: base SetPlayerClass updates class only; Java upgradePlayer() (stat/skill recompute) not ported.
        entry.SetVar(0, 5); // Java changeQuestStep(env, 5, 5, true) -> var0 stays 5, flips REWARD
        entry.Status = QuestStatus.REWARD;
        await UpdateQuestStatusAsync(conn, entry, ct);
        return await SendQuestDialogAsync(conn, env.Target?.ObjectId ?? 0, 5, ct);
    }

    public override async ValueTask<bool> OnItemUseAsync(Player player, int itemId, GsClientConnection conn, CancellationToken ct)
    {
        if (itemId != EmptyBottle) return false;
        var entry = player.Quests.Get(QuestId);
        if (entry is null || entry.Status != QuestStatus.START) return false;
        if (!player.CurrentZones.Contains(ItemUseZone)) return false;
        if (entry.GetVar(0) != 1) return false;

        // Java useQuestItem(env, item, 1, 2, false, 182200008, 1, 0): give the filled bottle, var0 1->2 (no cast delay ported).
        if (!await GiveQuestItemAsync(player, conn, _itemDao, FilledBottle, 1, ct)) return false;
        await ChangeQuestStepAsync(conn, entry, 0, 2, toReward: false, ct);
        return true;
    }

    public override async ValueTask<bool> OnKillAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var player = env.Player;
        var entry  = player.Quests.Get(QuestId);
        if (entry is null || entry.Status != QuestStatus.START) return false;

        int var      = entry.GetVar(0);
        int targetId = env.TargetId;

        if (targetId == Raider)
        {
            // note: Java NpcActions.delete(npc) corpse cleanup - no despawn API, dropped.
            if (var >= 51 && var < 54)
                return await DefaultOnKillEventAsync(env, conn, Raider, 51, 54, ct); // var 51..53 -> 52..54
            if (var == 54)
            {
                entry.SetVar(0, 4);
                await UpdateQuestStatusAsync(conn, entry, ct);
                SpawnQuestNpc(InstanceWorld, player.Position.InstanceId, Orissan, 226.7f, 251.5f, 205.5f, 0);
                // note: Java aggros Orissan onto the player (addDamage) - no aggro API, dropped.
                return true;
            }
            return false;
        }

        if (targetId == Orissan && var == 4)
        {
            await PlayQuestMovieAsync(conn, player, 151, ct);
            // note: Java despawns all instance NPCs here - no despawn API, dropped.
            SpawnQuestNpc(InstanceWorld, player.Position.InstanceId, Pernos, 220.6f, 247.8f, 206.0f, 0);
            entry.SetVar(0, 5);
            await UpdateQuestStatusAsync(conn, entry, ct);
            // Java falls through to return false.
        }
        return false;
    }

    public override async ValueTask<bool> OnQuestTimerEndAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var player = env.Player;
        var entry  = player.Quests.Get(QuestId);
        if (entry is null || entry.Status != QuestStatus.START) return false;
        if (entry.GetVar(0) != 50) return false;

        entry.SetVar(0, 51);
        await UpdateQuestStatusAsync(conn, entry, ct);
        int inst = player.Position.InstanceId;
        SpawnQuestNpc(InstanceWorld, inst, Raider, 224.073f, 239.1f, 206.7f, 0);
        SpawnQuestNpc(InstanceWorld, inst, Raider, 233.5f, 241.04f, 206.365f, 0);
        SpawnQuestNpc(InstanceWorld, inst, Raider, 229.6f, 265.7f, 205.7f, 0);
        SpawnQuestNpc(InstanceWorld, inst, Raider, 222.8f, 262.5f, 205.7f, 0);
        // note: Java aggros the spawned Raiders onto the player (addDamage) - no aggro API, dropped.
        return true;
    }

    public override async ValueTask<bool> OnMovieEndAsync(QuestEnv env, int movieId, GsClientConnection conn, CancellationToken ct)
    {
        var player = env.Player;
        var entry  = player.Quests.Get(QuestId);
        if (entry is null || entry.Status != QuestStatus.START) return false;
        if (movieId == 14)
        {
            await RemoveQuestItemAsync(player, conn, _itemDao, FilledBottle, 1, ct);
            await GiveQuestItemAsync(player, conn, _itemDao, DaminuEssence, 1, ct);
            entry.SetVar(0, 3);
            await UpdateQuestStatusAsync(conn, entry, ct);
            // note: Java teleports to Cliona Lake (210010000) - relocation dropped.
            return true;
        }
        return false;
    }

    public override async ValueTask<bool> OnDieAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var player = env.Player;
        var entry  = player.Quests.Get(QuestId);
        if (entry is null) return false;
        // Java bug: the original guards `getStatus() != START`, which never matches during the fight (the
        // quest is START then) so death never fails it; the sibling _2008Ascension correctly guards `== START`.
        if (entry.Status != QuestStatus.START) return false;

        int var = entry.GetVar(0);
        if (var == 4 || (var == 5 && player.PlayerClass.IsStartingClass()) || (var >= 50 && var <= 55))
        {
            entry.SetVar(0, 3);
            await UpdateQuestStatusAsync(conn, entry, ct);
            // note: Java sends the QUEST_FAILED system message - dropped (cosmetic notification).
        }
        return false;
    }

    public override async ValueTask<bool> OnEnterWorldAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var player = env.Player;
        var entry  = player.Quests.Get(QuestId);
        if (entry is null || entry.Status != QuestStatus.START) return false;

        int var = entry.GetVar(0);
        bool inFight = var == 4 || (var == 5 && player.PlayerClass.IsStartingClass()) || (var >= 50 && var <= 55) || var == 99;
        if (inFight)
        {
            if (player.Position.WorldId != InstanceWorld)
            {
                entry.SetVar(0, 3);
                await UpdateQuestStatusAsync(conn, entry, ct);
                // note: Java sends the QUEST_FAILED system message - dropped (cosmetic notification).
                return false;
            }
            // note: Java sends SM_ASCENSION_MORPH(1) (class-morph visual) here - packet not ported, dropped.
            return true;
        }
        return false;
    }
}
