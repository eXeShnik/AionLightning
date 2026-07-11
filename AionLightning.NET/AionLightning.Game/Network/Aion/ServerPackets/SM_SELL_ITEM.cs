using AionLightning.Commons.Network;

namespace AionLightning.Game.Network.Aion.ServerPackets;

/// <summary>
/// Opens the vendor sell window (Java SM_SELL_ITEM). Opcode 0x3E.
/// This is the no-purchase-template variant every regular vendor uses;
/// the tabbed trade-in variant can be added with trade-in-list support.
/// </summary>
public sealed class SM_SELL_ITEM : AionServerPacket
{
    private readonly int _targetObjectId;

    public SM_SELL_ITEM(int targetObjectId) : base(0x3E)
    {
        _targetObjectId = targetObjectId;
    }

    public override void Write(ref PacketWriter w)
    {
        w.WriteD(_targetObjectId);
        w.WriteD(5121);   // sell percentage marker (Java fallback branch)
        w.WriteD(65792);
        w.WriteC(0);
    }
}
