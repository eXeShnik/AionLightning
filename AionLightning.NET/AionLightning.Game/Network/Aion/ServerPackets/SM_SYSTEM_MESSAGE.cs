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
}
