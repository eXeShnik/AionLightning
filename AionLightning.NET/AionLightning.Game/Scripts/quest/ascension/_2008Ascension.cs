// Port of Java data/scripts/system/handlers/quest/ascension/_2008Ascension.java (MrPoke).
// The Asmodian class-ascension questline. Start NPC 203550 (var0 0->1); relay through 790003 (SETPRO2,
// gives 182203009, var0->2), 790002 (SETPRO3, gives 182203010, var0->3) and 203546 (SETPRO4, gives
// 182203011, var0->4). At var0 4, SELECT_ACTION_2376 plays movie 57 and consumes the three relics;
// SETPRO5 opens the instance 320020000 (var0->99). NPC 205020 (var0 99) starts the encounter (var0->50,
// a 43s timer spawns four mobs 205040 and sets var0->51); killing them (var0 51..54) spawns 205041
// (var0->5); killing 205041 (movie 152) spawns 203550 in the instance (var0->6); 203550 then performs the
// 2nd-class change (SETPRO6 shows the class sub-dialog, SETPRO7..17 pick the class) and flips REWARD.
//
// New helper used: SetPlayerClass (Java ClassChangeService.setClass) for the 2nd-class change.
//
// Deviations vs Java (state transitions preserved): identical in kind to _1006Ascension - instance via
// EnterInstanceAsync; TeleportService2 relocations dropped; flight-teleport morph dropped (43s pre-fight
// delay preserved via the quest timer); mob aggro / corpse despawn have no API and are dropped;
// SM_ASCENSION_MORPH and the QUEST_FAILED system message are cosmetic drops; SetPlayerClass updates the
// class only (Java upgradePlayer() not ported); CustomConfig.ENABLE_SIMPLE_2NDCLASS early-return not
// ported; QUEST_SELECT -> SETPROn switch fallthroughs evaluated independently (per _1007/_2009 precedent).
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

public sealed class _2008Ascension : QuestHandlerBase
{
    private const int QuestIdConst  = 2008;
    private const int StartNpc      = 203550;
    private const int RelayA        = 790003;
    private const int RelayB        = 790002;
    private const int RelayC        = 203546;
    private const int FlightNpc     = 205020;
    private const int MobA          = 205040;
    private const int MobB          = 205041;
    private const int InstanceWorld = 320020000;
    private const int RelicA        = 182203009;
    private const int RelicB        = 182203010;
    private const int RelicC        = 182203011;

    private readonly IItemDao _itemDao;

    public _2008Ascension(IDataManager dataManager, IQuestDao questDao, QuestRewardService rewardService, IItemDao itemDao)
        : base(QuestIdConst, dataManager, questDao, rewardService)
    {
        _itemDao = itemDao;
    }

    public override void Register(QuestEngine engine)
    {
        engine.RegisterOnLevelUp(QuestId);
        engine.RegisterQuestNpc(StartNpc).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(RelayA).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(RelayB).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(RelayC).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(FlightNpc).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(MobA).OnKill.Add(QuestId);
        engine.RegisterQuestNpc(MobB).OnKill.Add(QuestId);
        engine.RegisterOnQuestMovieEnd(152, QuestId); // registered-but-unhandled in Java too (played on the MobB kill)
        engine.RegisterOnEnterWorld(QuestId);
        RegisterOnDie(engine);
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
            if (targetId == StartNpc)
            {
                if (dialog == DialogAction.QUEST_SELECT)
                {
                    if (var == 0) return await SendQuestDialogAsync(conn, targetObjId, 1011, ct);
                    if (var == 4) return await SendQuestDialogAsync(conn, targetObjId, 2375, ct);
                    if (var == 6) return await SendQuestDialogAsync(conn, targetObjId, 2716, ct);
                    return false;
                }
                if (dialog == DialogAction.SELECT_ACTION_2376 && var == 4)
                {
                    await PlayQuestMovieAsync(conn, player, 57, ct);
                    await RemoveQuestItemAsync(player, conn, _itemDao, RelicA, 1, ct);
                    await RemoveQuestItemAsync(player, conn, _itemDao, RelicB, 1, ct);
                    await RemoveQuestItemAsync(player, conn, _itemDao, RelicC, 1, ct);
                    return false;
                }
                if (dialog == DialogAction.SETPRO1 && var == 0)
                {
                    entry.SetVar(0, 1);
                    await UpdateQuestStatusAsync(conn, entry, ct);
                    // note: Java teleports to 220010000 - relocation dropped.
                    return true;
                }
                if (dialog == DialogAction.SETPRO5 && var == 4)
                {
                    entry.SetVar(0, 99);
                    await UpdateQuestStatusAsync(conn, entry, ct);
                    await CloseDialogWindowAsync(conn, targetObjId, ct); // Java SM_DIALOG_WINDOW(objId, 0)
                    await EnterInstanceAsync(player, conn, InstanceWorld, 457.65f, 426.8f, 230.4f, 0, ct);
                    return true;
                }
                // 2nd-class change (var0 == 6). SETPRO6 shows the class sub-dialog; SETPRO7..17 pick the class.
                if (dialog == DialogAction.SETPRO6 && var == 6 && player.PlayerClass.IsStartingClass())
                {
                    int page = player.PlayerClass switch
                    {
                        PlayerClass.WARRIOR  => 3057,
                        PlayerClass.SCOUT    => 3398,
                        PlayerClass.MAGE     => 3739,
                        PlayerClass.PRIEST   => 4080,
                        PlayerClass.ENGINEER => 3569,
                        PlayerClass.ARTIST   => 3910,
                        _                    => 0,
                    };
                    if (page != 0) return await SendQuestDialogAsync(conn, targetObjId, page, ct);
                }
                if (dialog == DialogAction.SETPRO7  && var == 6) return await SetPlayerClassAsync(env, conn, entry, PlayerClass.GLADIATOR, ct);
                if (dialog == DialogAction.SETPRO8  && var == 6) return await SetPlayerClassAsync(env, conn, entry, PlayerClass.TEMPLAR, ct);
                if (dialog == DialogAction.SETPRO9  && var == 6) return await SetPlayerClassAsync(env, conn, entry, PlayerClass.ASSASSIN, ct);
                if (dialog == DialogAction.SETPRO10 && var == 6) return await SetPlayerClassAsync(env, conn, entry, PlayerClass.RANGER, ct);
                if (dialog == DialogAction.SETPRO11 && var == 6) return await SetPlayerClassAsync(env, conn, entry, PlayerClass.SORCERER, ct);
                if (dialog == DialogAction.SETPRO12 && var == 6) return await SetPlayerClassAsync(env, conn, entry, PlayerClass.SPIRIT_MASTER, ct);
                if (dialog == DialogAction.SETPRO13 && var == 6) return await SetPlayerClassAsync(env, conn, entry, PlayerClass.CHANTER, ct);
                if (dialog == DialogAction.SETPRO14 && var == 6) return await SetPlayerClassAsync(env, conn, entry, PlayerClass.CLERIC, ct);
                if (dialog == DialogAction.SETPRO15 && var == 6) return await SetPlayerClassAsync(env, conn, entry, PlayerClass.GUNNER, ct);
                if (dialog == DialogAction.SETPRO16 && var == 6) return await SetPlayerClassAsync(env, conn, entry, PlayerClass.BARD, ct);
                if (dialog == DialogAction.SETPRO17 && var == 6) return await SetPlayerClassAsync(env, conn, entry, PlayerClass.RIDER, ct);
                return false;
            }

            if (targetId == RelayA)
            {
                if (dialog == DialogAction.QUEST_SELECT && var == 1)
                    return await SendQuestDialogAsync(conn, targetObjId, 1352, ct);
                if (dialog == DialogAction.SETPRO2 && var == 1)
                {
                    if ((player.Inventory.FindByItemId(RelicA)?.Count ?? 0) == 0)
                        if (!await GiveQuestItemAsync(player, conn, _itemDao, RelicA, 1, ct)) return true;
                    entry.SetVar(0, 2);
                    await UpdateQuestStatusAsync(conn, entry, ct);
                    // note: Java teleports to 220010000 - relocation dropped.
                    return true;
                }
                return false;
            }

            if (targetId == RelayB)
            {
                if (dialog == DialogAction.QUEST_SELECT && var == 2)
                    return await SendQuestDialogAsync(conn, targetObjId, 1693, ct);
                if (dialog == DialogAction.SETPRO3 && var == 2)
                {
                    if ((player.Inventory.FindByItemId(RelicB)?.Count ?? 0) == 0)
                        if (!await GiveQuestItemAsync(player, conn, _itemDao, RelicB, 1, ct)) return true;
                    entry.SetVar(0, 3);
                    await UpdateQuestStatusAsync(conn, entry, ct);
                    // note: Java teleports to 220010000 - relocation dropped.
                    return true;
                }
                return false;
            }

            if (targetId == RelayC)
            {
                if (dialog == DialogAction.QUEST_SELECT && var == 3)
                    return await SendQuestDialogAsync(conn, targetObjId, 2034, ct);
                if (dialog == DialogAction.SETPRO4 && var == 3)
                {
                    if ((player.Inventory.FindByItemId(RelicC)?.Count ?? 0) == 0)
                        if (!await GiveQuestItemAsync(player, conn, _itemDao, RelicC, 1, ct)) return true;
                    entry.SetVar(0, 4);
                    await UpdateQuestStatusAsync(conn, entry, ct);
                    // note: Java teleports to 220010000 - relocation dropped.
                    return true;
                }
                return false;
            }

            if (targetId == FlightNpc)
            {
                if (dialog == DialogAction.QUEST_SELECT && var == 99)
                {
                    // note: Java applies the flight morph (skill 1853 + SM_EMOTION START_FLYTELEPORT) - cosmetic, dropped.
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
            if (targetId == StartNpc)
            {
                if (dialog == DialogAction.SELECTED_QUEST_NOREWARD && player.Position.WorldId == InstanceWorld)
                {
                    // note: Java teleports out of the instance to 220010000 - relocation dropped.
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
        entry.SetVar(0, 6); // Java changeQuestStep(env, 6, 6, true) -> var0 stays 6, flips REWARD
        entry.Status = QuestStatus.REWARD;
        await UpdateQuestStatusAsync(conn, entry, ct);
        return await SendQuestDialogAsync(conn, env.Target?.ObjectId ?? 0, 5, ct);
    }

    public override async ValueTask<bool> OnKillAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var player = env.Player;
        var entry  = player.Quests.Get(QuestId);
        if (entry is null || entry.Status != QuestStatus.START) return false;

        int var      = entry.GetVar(0);
        int targetId = env.TargetId;
        int inst     = player.Position.InstanceId;

        if (targetId == MobA)
        {
            // note: Java NpcActions.delete(npc) corpse cleanup - no despawn API, dropped.
            if (var >= 51 && var <= 53)
            {
                entry.SetVar(0, var + 1);
                await UpdateQuestStatusAsync(conn, entry, ct);
                return true;
            }
            if (var == 54)
            {
                entry.SetVar(0, 5);
                await UpdateQuestStatusAsync(conn, entry, ct);
                SpawnQuestNpc(InstanceWorld, inst, MobB, 301f, 259f, 205.5f, 0);
                // note: Java aggros MobB onto the player (addDamage) - no aggro API, dropped.
                return true;
            }
            return false;
        }

        if (targetId == MobB && var == 5)
        {
            await PlayQuestMovieAsync(conn, player, 152, ct);
            // note: Java despawns all instance NPCs here - no despawn API, dropped.
            SpawnQuestNpc(InstanceWorld, inst, StartNpc, 301.92999f, 274.26001f, 205.7f, 0);
            entry.SetVar(0, 6);
            await UpdateQuestStatusAsync(conn, entry, ct);
            return true;
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
        SpawnQuestNpc(InstanceWorld, inst, MobA, 294f, 277f, 207f, 0);
        SpawnQuestNpc(InstanceWorld, inst, MobA, 305f, 279f, 206.5f, 0);
        SpawnQuestNpc(InstanceWorld, inst, MobA, 298f, 253f, 205.7f, 0);
        SpawnQuestNpc(InstanceWorld, inst, MobA, 306f, 251f, 206f, 0);
        // note: Java aggros the spawned mobs onto the player (addDamage) - no aggro API, dropped.
        return true;
    }

    public override async ValueTask<bool> OnDieAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var player = env.Player;
        var entry  = player.Quests.Get(QuestId);
        if (entry is null || entry.Status != QuestStatus.START) return false;

        int var = entry.GetVar(0);
        if (var == 5 || (var == 6 && player.PlayerClass.IsStartingClass()) || (var >= 51 && var <= 53))
        {
            entry.SetVar(0, 4);
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
        bool inFight = var == 5 || (var == 6 && player.PlayerClass.IsStartingClass()) || (var >= 50 && var <= 55) || var == 99;
        if (inFight)
        {
            if (player.Position.WorldId != InstanceWorld)
            {
                entry.SetVar(0, 4);
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
