using AionLightning.Commons.Network;

namespace AionLightning.Game.Network.Aion.ServerPackets;

/// <summary>
/// Delivers a localised system notification to the client (opcode 0x19).
/// Wire format: colorId(C) + dialect(C) + npcObjId(D) + msgCode(D) + paramCount(C)
///              + [writeS per string param] + npcShout(C).
/// </summary>
public sealed class SM_SYSTEM_MESSAGE : AionServerPacket
{
    private readonly int    _code;
    private readonly string _param;

    private SM_SYSTEM_MESSAGE(int code, string param = "") : base(0x19)
    {
        _code  = code;
        _param = param;
    }

    public override void Write(ref PacketWriter w)
    {
        w.WriteC(0x19); // text color id
        w.WriteC(0x00); // dialect unk
        w.WriteD(0);    // npc object id (0 = not from NPC)
        w.WriteD(_code);
        w.WriteC(string.IsNullOrEmpty(_param) ? (byte)0 : (byte)1);
        if (!string.IsNullOrEmpty(_param))
            w.WriteS(_param);
        w.WriteC(0x00); // not a npc shout
    }

    // STR_NO_SUCH_USER — "Cannot find player %0." (msg code 1300627)
    public static SM_SYSTEM_MESSAGE NoSuchUser(string name) => new(1300627, name);

    // STR_NO_ENOUGH_KINAH — "Not enough Kinah." (msg code 1300137)
    public static SM_SYSTEM_MESSAGE NoEnoughKinah() => new(1300137);
}
