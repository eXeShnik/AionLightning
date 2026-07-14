// Port of Java data/scripts/system/handlers/quest/sanctum/_1929ASliverofDarkness.java
// (Mr. Poke, reworked vlog, modified Rolandas/apozema). Stigma-attunement questline:
// Jucleas (203752, var0 0->1) -> Ludina (203852, var0 1->2) -> Morai (203164, SETPRO3 var2->93,
// enters instance 310070000) -> Icaronix (205110, SETPRO4 var93->94, flight to the box) ->
// Icaronix's Box (700240, USE_OBJECT var94, movie 155) -> movie-end spawns Ecus (205111) + var94->98
// -> Ecus SELECT_ACTION_2546 hands the class stigma stone + 300 Stigma Shards -> equipping the stigma
// (onEquipItem) advances var98->96 -> Ecus SELECT_ACTION_2720 (var96) spawns the Sliver (212992) +
// var96->97 -> killing 212992 (var97->8, out to 210030000) -> Morai SETPRO7 (var8->9, to 110010000)
// -> Lavirintos (203701, SETPRO8 var9 -> REWARD) -> turn in at Miriya (203711).
//
// New hook used: OnEquipItemAsync (Java registerOnEquipItem) gates the stigma-equip step (var98->96);
// Java's isStigmaEquipped/getEquippedItemsAllStigma is approximated via the equipped itemId, advancing
// only when the player's own class stigma stone is equipped.
//
// Deviations vs Java (state transitions preserved):
//  - Instance creation via EnterInstanceAsync (Java getNextAvailableInstance/registerPlayerWithInstance).
//  - Non-instance TeleportService2 relocations (Ludina SETPRO2 -> 210030000; Morai SETPRO7 -> 110010000;
//    onKill 212992 -> 210030000) are dropped (no relocation API); the var transitions are kept so the
//    chain stays completable (player travels manually).
//  - Icaronix's flight-teleport animation (SM_EMOTION START_FLYTELEPORT) is cosmetic and dropped.
//  - At Ecus USE_OBJECT (var96) Java re-checks isStigmaEquipped; var96 is only reachable by equipping the
//    stigma (onEquipItem), so it is treated as still-equipped and the 2716 branch is always shown.
//  - removeStigma (Java unequips the stone + removes the item) is approximated by removing the item;
//    the equipment-slot unequip is not modelled.
//  - QUEST_FAILED system message on death/leave and SM_DIALOG_WINDOW refinements are cosmetic drops.
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

namespace Quest.Sanctum;

public sealed class _1929ASliverofDarkness : QuestHandlerBase
{
    private const int QuestIdConst  = 1929;
    private const int Jucleas       = 203752;
    private const int Ludina        = 203852;
    private const int Morai         = 203164;
    private const int Icaronix      = 205110;
    private const int IcaronixBox   = 700240;
    private const int Ecus          = 205111;
    private const int Lavirintos    = 203701;
    private const int Miriya        = 203711;
    private const int Sliver        = 212992;
    private const int InstanceWorld = 310070000;
    private const int StigmaShard   = 141000001;
    private const int ShardTarget   = 300;

    private static readonly int[] Stigmas =
    {
        140000008, 140000027, 140000047, 140000076, 140000131, 140000147,
        140000098, 140000112, 140000859, 140000943, 140001002,
    };

    private readonly IItemDao _itemDao;

    public _1929ASliverofDarkness(IDataManager dataManager, IQuestDao questDao, QuestRewardService rewardService, IItemDao itemDao)
        : base(QuestIdConst, dataManager, questDao, rewardService)
    {
        _itemDao = itemDao;
    }

    public override void Register(QuestEngine engine)
    {
        engine.RegisterOnLevelUp(QuestId);
        engine.RegisterOnQuestMovieEnd(155, QuestId);
        engine.RegisterQuestNpc(Sliver).OnKill.Add(QuestId);
        engine.RegisterOnEnterWorld(QuestId);
        RegisterOnDie(engine);
        foreach (int npc in new[] { Jucleas, Ludina, Morai, Icaronix, IcaronixBox, Ecus, Lavirintos, Miriya })
            engine.RegisterQuestNpc(npc).OnTalk.Add(QuestId);
        foreach (int stigma in Stigmas)
            RegisterOnEquipItem(engine, stigma);
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
            if (targetId == Jucleas)
            {
                if (dialog == DialogAction.QUEST_SELECT && var == 0)
                    return await SendQuestDialogAsync(conn, targetObjId, 1011, ct);
                if (dialog == DialogAction.SETPRO1)
                    return await DefaultCloseDialogAsync(env, conn, 0, 1, ct);
                return false;
            }

            if (targetId == Ludina)
            {
                if (dialog == DialogAction.QUEST_SELECT && var == 1)
                    return await SendQuestDialogAsync(conn, targetObjId, 1352, ct);
                if (dialog == DialogAction.SETPRO2 && var == 1)
                {
                    // note: Java teleports to 210030000 here (relocation) - dropped.
                    await ChangeQuestStepAsync(conn, entry, 0, 2, toReward: false, ct);
                    return await CloseDialogWindowAsync(conn, targetObjId, ct);
                }
                return false;
            }

            if (targetId == Morai)
            {
                if (dialog == DialogAction.QUEST_SELECT)
                {
                    if (var == 2) return await SendQuestDialogAsync(conn, targetObjId, 1693, ct);
                    if (var == 8) return await SendQuestDialogAsync(conn, targetObjId, 3057, ct);
                }
                if (dialog == DialogAction.SETPRO3 && var == 2)
                {
                    await ChangeQuestStepAsync(conn, entry, 0, 93, toReward: false, ct);
                    await EnterInstanceAsync(player, conn, InstanceWorld, 338f, 101f, 1191f, 0, ct);
                    return await CloseDialogWindowAsync(conn, targetObjId, ct);
                }
                if (dialog == DialogAction.SETPRO7 && var == 8)
                {
                    await ChangeQuestStepAsync(conn, entry, 0, 9, toReward: false, ct);
                    // note: Java teleports to 110010000 here (relocation) - dropped.
                    return await CloseDialogWindowAsync(conn, targetObjId, ct);
                }
                return false;
            }

            if (targetId == Icaronix)
            {
                if (dialog == DialogAction.QUEST_SELECT && var == 93)
                    return await SendQuestDialogAsync(conn, targetObjId, 2034, ct);
                if (dialog == DialogAction.SETPRO4 && var == 93)
                {
                    await ChangeQuestStepAsync(conn, entry, 0, 94, toReward: false, ct);
                    // note: Java plays a flight-teleport animation (SM_EMOTION START_FLYTELEPORT 31001) - cosmetic, dropped.
                    return true;
                }
                return false;
            }

            if (targetId == IcaronixBox)
            {
                if (dialog == DialogAction.USE_OBJECT && var == 94)
                {
                    await PlayQuestMovieAsync(conn, player, 155, ct);
                    return true;
                }
                return false;
            }

            if (targetId == Ecus)
            {
                if (dialog == DialogAction.USE_OBJECT && var == 96)
                    // note: Java re-checks isStigmaEquipped; var96 is only reached by equipping the stigma, so treated as equipped.
                    return await SendQuestDialogAsync(conn, targetObjId, 2716, ct);
                if (dialog == DialogAction.QUEST_SELECT && var == 98)
                    return await SendQuestDialogAsync(conn, targetObjId, 2375, ct);
                if (dialog == DialogAction.SELECT_ACTION_2546 && var == 98)
                {
                    int stone = GetStoneId(player.PlayerClass);
                    if (await GiveQuestItemAsync(player, conn, _itemDao, stone, 1, ct))
                    {
                        long shards = player.Inventory.FindByItemId(StigmaShard)?.Count ?? 0;
                        if (shards < ShardTarget)
                            await GiveQuestItemAsync(player, conn, _itemDao, StigmaShard, ShardTarget, ct);
                        return await SendQuestDialogAsync(conn, targetObjId, 1, ct); // Java SM_DIALOG_WINDOW(objId, 1)
                    }
                    return false;
                }
                if (dialog == DialogAction.SELECT_ACTION_2720 && var == 96)
                {
                    // note: Java despawns Ecus here (onDelete) - no despawn API, dropped.
                    SpawnQuestNpc(InstanceWorld, player.Position.InstanceId, Sliver, 191.9f, 267.68f, 1374f, 0);
                    await ChangeQuestStepAsync(conn, entry, 0, 97, toReward: false, ct);
                    return await CloseDialogWindowAsync(conn, targetObjId, ct);
                }
                return false;
            }

            if (targetId == Lavirintos)
            {
                if (dialog == DialogAction.QUEST_SELECT && var == 9)
                    return await SendQuestDialogAsync(conn, targetObjId, 3398, ct);
                if (dialog == DialogAction.SETPRO8)
                    return await DefaultCloseDialogAsync(env, conn, 9, 9, reward: true, sameNpc: false, ct);
                return false;
            }

            return false;
        }

        if (entry.Status == QuestStatus.REWARD)
        {
            if (targetId == Miriya)
            {
                if (dialog == DialogAction.USE_OBJECT)
                    return await SendQuestDialogAsync(conn, targetObjId, 10002, ct);
                return await SendQuestEndDialogAsync(env, conn, ct);
            }
        }
        return false;
    }

    public override async ValueTask<bool> OnMovieEndAsync(QuestEnv env, int movieId, GsClientConnection conn, CancellationToken ct)
    {
        var player = env.Player;
        var entry  = player.Quests.Get(QuestId);
        if (entry is null) return false;
        if (movieId == 155)
        {
            SpawnQuestNpc(InstanceWorld, player.Position.InstanceId, Ecus, 197.6f, 265.9f, 1374f, 0);
            await ChangeQuestStepAsync(conn, entry, 0, 98, toReward: false, ct);
            return true;
        }
        return false;
    }

    public override async ValueTask<bool> OnEquipItemAsync(QuestEnv env, int itemId, GsClientConnection conn, CancellationToken ct)
    {
        var player = env.Player;
        var entry  = player.Quests.Get(QuestId);
        if (entry is null || entry.Status != QuestStatus.START) return false;
        // Approximate Java isStigmaEquipped: advance only when the player's own class stigma stone is equipped.
        if (entry.GetVar(0) != 98 || itemId != GetStoneId(player.PlayerClass)) return false;

        await ChangeQuestStepAsync(conn, entry, 0, 96, toReward: false, ct);
        return await CloseDialogWindowAsync(conn, env.Target?.ObjectId ?? 0, ct);
    }

    public override async ValueTask<bool> OnKillAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var entry = env.Player.Quests.Get(QuestId);
        if (entry is null || entry.Status != QuestStatus.START) return false;
        if (entry.GetVar(0) == 97)
        {
            await ChangeQuestStepAsync(conn, entry, 0, 8, toReward: false, ct);
            // note: Java teleports to 210030000 here (relocation) - dropped.
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
        if (var >= 93 && var <= 98)
        {
            await RemoveStigmaAsync(player, conn, ct);
            entry.SetVar(0, 2);
            await UpdateQuestStatusAsync(conn, entry, ct);
            // note: Java sends QUEST_FAILED system message - dropped (cosmetic notification).
            return true;
        }
        return false;
    }

    public override async ValueTask<bool> OnEnterWorldAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var player = env.Player;
        var entry  = player.Quests.Get(QuestId);
        if (entry is null || entry.Status != QuestStatus.START) return false;
        int var = entry.GetVar(0);
        if (player.Position.WorldId != InstanceWorld)
        {
            if (var >= 93 && var <= 98)
            {
                await RemoveStigmaAsync(player, conn, ct);
                entry.SetVar(0, 2);
                await UpdateQuestStatusAsync(conn, entry, ct);
                // note: Java sends QUEST_FAILED system message - dropped (cosmetic notification).
                return true;
            }
            if (var == 8)
            {
                await RemoveStigmaAsync(player, conn, ct);
                return true;
            }
        }
        return false;
    }

    private async ValueTask RemoveStigmaAsync(Player player, GsClientConnection conn, CancellationToken ct)
    {
        // note: Java also unequips the stone from the stigma slot; equipment-slot unequip is not modelled.
        int stone = GetStoneId(player.PlayerClass);
        if (stone == 0) return;
        await RemoveQuestItemAsync(player, conn, _itemDao, stone, 1, ct);
    }

    private static int GetStoneId(PlayerClass playerClass) => playerClass switch
    {
        PlayerClass.GLADIATOR    => 140000008, // Improved Stamina I
        PlayerClass.TEMPLAR      => 140000027, // Divine Fury I
        PlayerClass.RANGER       => 140000047, // Arrow Deluge I
        PlayerClass.ASSASSIN     => 140000076, // Sigil Strike I
        PlayerClass.SORCERER     => 140000131, // Lumiel's Wisdom I
        PlayerClass.SPIRIT_MASTER => 140000147, // Absorb Vitality I
        PlayerClass.CLERIC       => 140000098, // Grace of Empyrean Lord I
        PlayerClass.CHANTER      => 140000112, // Rage Spell I
        PlayerClass.GUNNER       => 140000943, // Nature's Favor I
        PlayerClass.BARD         => 140000859, // Freestyle I
        PlayerClass.RIDER        => 140001002, // Nullification Trigger I
        _                        => 0,
    };
}
