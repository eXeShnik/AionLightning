using AionLightning.Commons.Network;

namespace AionLightning.Game.Network.Aion.ServerPackets;

/// <summary>
/// Sends legion emblem info to the requesting player. Opcode 0xD5.
/// Used in response to CM_LEGION_SEND_EMBLEM for DEFAULT-type emblems.
/// emblemDataSize is 0 for DEFAULT emblems (no raw image data follows).
/// </summary>
public sealed class SM_LEGION_SEND_EMBLEM : AionServerPacket
{
    private readonly int    _legionId;
    private readonly byte   _emblemId;
    private readonly byte   _emblemType;
    private readonly byte   _r;
    private readonly byte   _g;
    private readonly byte   _b;
    private readonly string _legionName;

    public SM_LEGION_SEND_EMBLEM(int legionId, byte emblemId, byte emblemType,
        byte r, byte g, byte b, string legionName) : base(0xD5)
    {
        _legionId  = legionId;
        _emblemId  = emblemId;
        _emblemType = emblemType;
        _r         = r;
        _g         = g;
        _b         = b;
        _legionName = legionName;
    }

    public override void Write(ref PacketWriter w)
    {
        w.WriteD(_legionId);
        w.WriteC(_emblemId);
        w.WriteC(_emblemType);
        w.WriteD(0);                                             // emblemDataSize = 0 (DEFAULT)
        w.WriteC(_emblemType == 0 ? (byte)0x00 : (byte)0xFF);  // 0x00 for DEFAULT
        w.WriteC(_r);
        w.WriteC(_g);
        w.WriteC(_b);
        w.WriteS(_legionName);
        w.WriteC(1);
    }
}
