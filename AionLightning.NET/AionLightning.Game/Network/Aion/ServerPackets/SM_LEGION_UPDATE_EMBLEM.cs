using AionLightning.Commons.Network;

namespace AionLightning.Game.Network.Aion.ServerPackets;

/// <summary>Broadcasts legion emblem change to the zone. Opcode 0xD7.</summary>
public sealed class SM_LEGION_UPDATE_EMBLEM : AionServerPacket
{
    private readonly int  _legionId;
    private readonly byte _emblemId;
    private readonly byte _emblemType;
    private readonly byte _r;
    private readonly byte _g;
    private readonly byte _b;

    public SM_LEGION_UPDATE_EMBLEM(int legionId, byte emblemId, byte emblemType,
        byte r, byte g, byte b) : base(0xD7)
    {
        _legionId   = legionId;
        _emblemId   = emblemId;
        _emblemType = emblemType;
        _r = r;
        _g = g;
        _b = b;
    }

    public override void Write(ref PacketWriter w)
    {
        w.WriteD(_legionId);
        w.WriteC(_emblemId);
        w.WriteC(_emblemType);
        w.WriteC(0xFF); // fixed
        w.WriteC(_r);
        w.WriteC(_g);
        w.WriteC(_b);
    }
}
