using AionLightning.Commons.Network;

namespace AionLightning.Game.Network.Aion.ServerPackets;

/// <summary>Relays a group brand marker to clients. Opcode 0xF9.</summary>
public sealed class SM_SHOW_BRAND : AionServerPacket
{
    private readonly int _brandId;
    private readonly int _targetObjectId;

    public SM_SHOW_BRAND(int brandId, int targetObjectId) : base(0xF9)
    {
        _brandId        = brandId;
        _targetObjectId = targetObjectId;
    }

    public override void Write(ref PacketWriter w)
    {
        w.WriteH(0x01);
        w.WriteD(0x01);
        w.WriteD(_brandId);
        w.WriteD(_targetObjectId);
    }
}
