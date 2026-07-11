using AionLightning.Commons.Network;

namespace AionLightning.Game.Network.Aion.ServerPackets;

/// <summary>
/// Delivers a localised system notification to the client (opcode 0x19).
/// Wire format: colorId(C) + dialect(C) + npcObjId(D) + msgCode(D) + paramCount(C)
///              + [writeS per string param] + npcShout(C).
/// </summary>
public sealed class SM_SYSTEM_MESSAGE : AionServerPacket
{
    private readonly int      _code;
    private readonly string[] _params;
    private readonly int      _npcObjId; // 0 for system messages, NPC objectId for shouts

    // System message (no NPC sender)
    private SM_SYSTEM_MESSAGE(int code, params string[] parms) : base(0x19)
    {
        _code     = code;
        _npcObjId = 0;
        _params   = parms;
    }

    // NPC shout (carries the NPC's object ID)
    private SM_SYSTEM_MESSAGE(int code, int npcObjId, string[] parms) : base(0x19)
    {
        _code     = code;
        _npcObjId = npcObjId;
        _params   = parms;
    }

    public override void Write(ref PacketWriter w)
    {
        w.WriteC(0x19); // text color id
        w.WriteC(0x00); // dialect unk
        w.WriteD(_npcObjId);
        w.WriteD(_code);
        w.WriteC((byte)_params.Length);
        foreach (var p in _params)
            w.WriteS(p);
        w.WriteC(0x00);
    }

    /// <summary>NPC shout — sent to nearby players with the NPC's object ID (mirrors Java NpcShoutsService).</summary>
    public static SM_SYSTEM_MESSAGE NpcShout(int npcObjId, int stringId)
        => new(stringId, npcObjId, Array.Empty<string>());

    // STR_NO_SUCH_USER — "Cannot find player %0." (msg code 1300627)
    public static SM_SYSTEM_MESSAGE NoSuchUser(string name) => new(1300627, name);

    // STR_NO_ENOUGH_KINAH — "Not enough Kinah." (msg code 1300137)
    public static SM_SYSTEM_MESSAGE NoEnoughKinah() => new(1300137);

    // STR_SKILL_NOT_ENOUGH_DP — "Not enough DP." (msg code 1300016)
    public static SM_SYSTEM_MESSAGE NotEnoughDp() => new(1300016);

    // STR_SKILL_NOT_ENOUGH_HP — "Not enough HP to use this skill." (msg code 1300014)
    public static SM_SYSTEM_MESSAGE NotEnoughHp() => new(1300014);

    // STR_SKILL_NOT_ENOUGH_MP — "Not enough MP to use this skill." (msg code 1300015)
    public static SM_SYSTEM_MESSAGE NotEnoughMp() => new(1300015);

    // STR_REBIRTH_MASSAGE_ME — "You have been revived at the bind point." (msg code 1300738)
    public static SM_SYSTEM_MESSAGE Revived() => new(1300738);

    // STR_DICE_ROLL_ME — "You rolled a %0 (1~%1)." (msg code 1400126)
    public static SM_SYSTEM_MESSAGE RollSelf(int roll, int max) => new(1400126, roll.ToString(), max.ToString());

    // STR_DICE_ROLL_OTHER — "%0 rolled a %1 (1~%2)." (msg code 1400127)
    public static SM_SYSTEM_MESSAGE RollOther(string name, int roll, int max) => new(1400127, name, roll.ToString(), max.ToString());

    // STR_CMD_LOCATION_DESC — "/loc output: mapId=%0, x=%1, y=%2, z=%3" (msg code 230038)
    public static SM_SYSTEM_MESSAGE LocationDesc(int worldId, float x, float y, float z)
        => new(230038, worldId.ToString(), x.ToString("F2"), y.ToString("F2"), z.ToString("F2"));

    // STR_MSG_ASK_PCINFO_LOGOFF — "The player is not logged in." (msg code 1300046)
    public static SM_SYSTEM_MESSAGE PlayerOffline() => new(1300046);

    // STR_UI_INVENTORY_FULL — "Inventory is full." (msg code 1300042)
    public static SM_SYSTEM_MESSAGE InventoryFull() => new(1300042);

    // STR_DEATH_REGISTER_RESURRECT_POINT — "You have set your resurrection point." (msg code 1300670)
    public static SM_SYSTEM_MESSAGE BindPointSet() => new(1300670, string.Empty);

    // STR_EXTRACT_GATHERING_SUCCESS_GETEXP — "You've gained exp from successful gathering." (msg code 1330082)
    public static SM_SYSTEM_MESSAGE GatheringSuccessExp() => new(1330082);

    // STR_MSG_MANASTONE_SUCCEED — "You have succeeded in the manastone socketing of %0." (msg code 1300252)
    public static SM_SYSTEM_MESSAGE ManastoneSuccess(string itemName) => new(1300252, itemName);

    // STR_EXTRACT_NO_SKILL — "You must learn the %0 skill to start gathering." (msg code 1330054)
    public static SM_SYSTEM_MESSAGE GatherNoSkill(string skillName) => new(1330054, skillName);

    // STR_EXTRACT_INSUFFICIENT_SKILL — "Your %0 skill level is not high enough." (msg code 1330001)
    public static SM_SYSTEM_MESSAGE GatherSkillLevelLow(string skillName) => new(1330001, skillName);

    // STR_MSG_ENCHANT_ITEM_SUCCEED_NEW — "You have enchanted %0 to +%1." (msg code 1401681)
    public static SM_SYSTEM_MESSAGE EnchantSuccess(string itemName, int level) => new(1401681, itemName, level.ToString());

    // STR_ENCHANT_ITEM_FAILED — "You have failed to enchant %0." (msg code 1300456)
    public static SM_SYSTEM_MESSAGE EnchantFailed(string itemName) => new(1300456, itemName);

    // STR_EXTEND_INVENTORY_CANT_EXTEND_MORE — "You cannot expand the cube any further." (msg code 1300430)
    public static SM_SYSTEM_MESSAGE CannotExpandCubeMore() => new(1300430);

    // STR_EXTEND_INVENTORY — "%0 slots have been added to the cube." (msg code 1300431)
    public static SM_SYSTEM_MESSAGE CubeExpanded(int slots) => new(1300431, slots.ToString());

    // STR_CRAFT_SKILL_UPGRADE_LIMIT — "You cannot learn any more skills from this NPC." (msg code 1390233)
    public static SM_SYSTEM_MESSAGE CraftSkillMaxLevel() => new(1390233);

    // STR_CRAFT_SKILL_NEED_QUEST — "You must complete the required quest to advance further." (msg code 1300834)
    public static SM_SYSTEM_MESSAGE CraftSkillNeedQuest() => new(1300834);

    // STR_SUCCESS_RECOVER_EXPERIENCE — "You have been cured of Soul Sickness." (msg code 1300674)
    public static SM_SYSTEM_MESSAGE SoulSicknessCleared() => new(1300674);

    // STR_DEATH_MESSAGE_ME — "You have died." (msg code 1300737)
    public static SM_SYSTEM_MESSAGE YouDied() => new(1300737);

    // STR_MSG_COMBAT_MY_DEATH_TO_B — "You were killed by %0's attack." (msg code 1340002)
    public static SM_SYSTEM_MESSAGE YouWereKilledBy(string killerName) => new(1340002, killerName);

    // STR_MSG_COMBAT_FRIENDLY_DEATH — "%0 has died." (msg code 1350000)
    public static SM_SYSTEM_MESSAGE GroupMemberDied(string playerName) => new(1350000, playerName);

    // STR_MSG_COMBAT_FRIENDLY_DEATH_TO_B — "%0 was killed by %1's attack." (msg code 1350001)
    public static SM_SYSTEM_MESSAGE PlayerKilledByPlayer(string victimName, string killerName) => new(1350001, victimName, killerName);

    // M381: STR_SKILL_SUMMON_ALREADY_HAVE_A_FOLLOWER (msg code 1300072) — master already has an active summon.
    // Java embeds the summon name via a client-side DescriptionId nameId reference for the below five messages;
    // this port uses a plain string param instead (simplification — see migration_plan.md M381 note).
    public static SM_SYSTEM_MESSAGE SummonAlreadyHaveFollower() => new(1300072);

    // STR_SKILL_SUMMON_UNSUMMONED — "%0 has been dismissed." (msg code 1200006)
    public static SM_SYSTEM_MESSAGE SummonUnsummoned(string summonName) => new(1200006, summonName);

    // STR_SKILL_SUMMON_ATTACK_MODE — "%0 starts to attack the enemy." (msg code 1200008)
    public static SM_SYSTEM_MESSAGE SummonAttackMode(string summonName) => new(1200008, summonName);

    // STR_SKILL_SUMMON_GUARD_MODE — "%0 is in Guard mode." (msg code 1200009)
    public static SM_SYSTEM_MESSAGE SummonGuardMode(string summonName) => new(1200009, summonName);

    // STR_SKILL_SUMMON_REST_MODE — "%0 is in Resting mode." (msg code 1200010)
    public static SM_SYSTEM_MESSAGE SummonRestMode(string summonName) => new(1200010, summonName);

    // STR_SKILL_SUMMON_UNSUMMON_FOLLOWER — "You unsummon %0." (msg code 1200011)
    public static SM_SYSTEM_MESSAGE SummonUnsummonFollower(string summonName) => new(1200011, summonName);

    // STR_GIVE_ITEM_PROC_ENCHANTED_TARGET_ITEM — "Item tuning complete." (msg code 1401626)
    public static SM_SYSTEM_MESSAGE TuningComplete() => new(1401626);

    // STR_GIVE_ITEM_PROC_ENCHANTED_TARGET_ITEM — "Godstone has been applied to %0." (msg code 1300502)
    public static SM_SYSTEM_MESSAGE GodstoneApplied() => new(1300502);

    // STR_GIVE_ITEM_PROC_NO_PROC_GIVE_ITEM — "This item cannot be used as a Godstone." (msg code 1300503)
    public static SM_SYSTEM_MESSAGE GodstoneInvalid() => new(1300503);

    // STR_GIVE_ITEM_PROC_CANNOT_GIVE_PROC_TO_EQUIPPED_ITEM — "Cannot apply Godstone to an equipped item." (msg code 1300504)
    public static SM_SYSTEM_MESSAGE GodstoneAlreadySlotted() => new(1300504);

    // STR_GIVE_ITEM_PROC_NOT_ENOUGH_MONEY — "Not enough Kinah to apply Godstone." (msg code 1300505)
    public static SM_SYSTEM_MESSAGE GodstoneNoKinah() => new(1300505);

    // Item dye messages (STR_ITEM_COLOR_*)
    // 1300510: STR_ITEM_COLOR_REMOVE_SUCCEED — "You have removed the dye from %0."
    public static SM_SYSTEM_MESSAGE DyeRemoved() => new(1300510);
    // 1300511: STR_ITEM_COLOR_CHANGE_SUCCEED — "You have dyed %0 %1."
    public static SM_SYSTEM_MESSAGE DyeApplied() => new(1300511);
    // 1300512: STR_ITEM_COLOR_CHANGE_ERROR_CANNOTDYE — "%0 cannot be dyed."
    public static SM_SYSTEM_MESSAGE DyeCannotDye() => new(1300512);
    // 1300513: STR_ITEM_COLOR_REMOVE_ERROR_CANNOTREMOVE — "The item has not been dyed."
    public static SM_SYSTEM_MESSAGE DyeCannotRemove() => new(1300513);

    // Weapon fusion messages (STR_COMPOUND_* / STR_DECOMPOUND_*)
    // 1400288: STR_COMPOUND_ERROR_MAIN_REQUIRE_HIGHER_LEVEL
    public static SM_SYSTEM_MESSAGE CompoundMainRequireHigherLevel() => new(1400288);
    // 1400289: STR_COMPOUND_ERROR_NOT_AVAILABLE — "%0 cannot be combined."
    public static SM_SYSTEM_MESSAGE CompoundNotAvailable() => new(1400289);
    // 1400335: STR_COMPOUNDED_ITEM_DECOMPOUND_SUCCESS — "The ability combined with %0 has been removed."
    public static SM_SYSTEM_MESSAGE DecompoundSuccess() => new(1400335);
    // 1400336: STR_COMPOUND_SUCCESS — "%1 has been combined with %0."
    public static SM_SYSTEM_MESSAGE CompoundSuccess() => new(1400336);
    // 1400337: STR_COMPOUND_ERROR_NOT_ENOUGH_MONEY — "Not enough Kinah to combine."
    public static SM_SYSTEM_MESSAGE CompoundNotEnoughMoney() => new(1400337);
    // 1400364: STR_COMPOUND_ERROR_DIFFERENT_TYPE — "Weapon types do not match."
    public static SM_SYSTEM_MESSAGE CompoundDifferentType() => new(1400364);
    // 1400373: STR_DECOMPOUND_ERROR_NOT_AVAILABLE — "%0 is not a combined item."
    public static SM_SYSTEM_MESSAGE DecompoundNotAvailable() => new(1400373);

    // Item remodel messages (STR_CHANGE_ITEM_SKIN_*)
    // 1300476: STR_CHANGE_ITEM_SKIN_PC_LEVEL_LIMIT — "You must be level 10 or higher."
    public static SM_SYSTEM_MESSAGE RemodelLevelLimit() => new(1300476);
    // 1300480: STR_CHANGE_ITEM_SKIN_NOT_COMPATIBLE — "Items are not compatible."
    public static SM_SYSTEM_MESSAGE RemodelNotCompatible() => new(1300480);
    // 1300481: STR_CHANGE_ITEM_SKIN_NOT_ENOUGH_GOLD — "Not enough Kinah for remodeling."
    public static SM_SYSTEM_MESSAGE RemodelNoKinah() => new(1300481);
    // 1300483: STR_CHANGE_ITEM_SKIN_SUCCEED — "Item remodeling successful."
    public static SM_SYSTEM_MESSAGE RemodelSuccess() => new(1300483);

    // STR_ITEM_CANT_USE_UNTIL_DELAY_TIME — "You cannot use this item yet." (msg code 1300400)
    public static SM_SYSTEM_MESSAGE ItemCantUseUntilDelayTime() => new(1300400);

    // STR_UI_STIGMA_NOT_ENOUGH_MATERIAL — "Not enough stigma shards." (msg code 1300450)
    public static SM_SYSTEM_MESSAGE StigmaNotEnoughShards() => new(1300450);

    // STR_EDIT_CHAR_ALL_CANT_NO_ITEM — "Need a Plastic Surgery Ticket." (msg code 901752)
    public static SM_SYSTEM_MESSAGE CharEditNoPlasticSurgeryTicket() => new(901752);

    // STR_EDIT_CHAR_GENDER_CANT_NO_ITEM — "Need a Gender Change Ticket." (msg code 901754)
    public static SM_SYSTEM_MESSAGE CharEditNoGenderTicket() => new(901754);

    // Legion rename messages (STR_LEGION_RENAME_*)
    // 1400152: invalid or forbidden name
    public static SM_SYSTEM_MESSAGE LegionNameInvalid() => new(1400152);
    // 1400154: name is the same as current
    public static SM_SYSTEM_MESSAGE LegionNameUnchanged() => new(1400154);
    // 1400156: name already in use
    public static SM_SYSTEM_MESSAGE LegionNameTaken() => new(1400156);
    // 1400158: rename successful — "Legion name changed to %0."
    public static SM_SYSTEM_MESSAGE LegionRenamed(string newName) => new(1400158, newName);
}
