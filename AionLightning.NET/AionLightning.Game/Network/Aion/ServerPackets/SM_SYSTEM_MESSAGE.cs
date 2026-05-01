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

    private SM_SYSTEM_MESSAGE(int code, params string[] parms) : base(0x19)
    {
        _code   = code;
        _params = parms;
    }

    public override void Write(ref PacketWriter w)
    {
        w.WriteC(0x19); // text color id
        w.WriteC(0x00); // dialect unk
        w.WriteD(0);    // npc object id (0 = not from NPC)
        w.WriteD(_code);
        w.WriteC((byte)_params.Length);
        foreach (var p in _params)
            w.WriteS(p);
        w.WriteC(0x00); // not a npc shout
    }

    // STR_NO_SUCH_USER — "Cannot find player %0." (msg code 1300627)
    public static SM_SYSTEM_MESSAGE NoSuchUser(string name) => new(1300627, name);

    // STR_NO_ENOUGH_KINAH — "Not enough Kinah." (msg code 1300137)
    public static SM_SYSTEM_MESSAGE NoEnoughKinah() => new(1300137);

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
}
