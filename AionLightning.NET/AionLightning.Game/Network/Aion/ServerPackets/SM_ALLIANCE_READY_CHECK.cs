using AionLightning.Commons.Network;

namespace AionLightning.Game.Network.Aion.ServerPackets;

/// <summary>
/// Alliance ready-check status packet. Opcode 0xFA (Java 4.5 table value — TODO: verify opcode vs live
/// 4.6 client). Wire format matches Java <c>SM_ALLIANCE_READY_CHECK.writeImpl()</c>.
/// </summary>
public sealed class SM_ALLIANCE_READY_CHECK : AionServerPacket
{
    private readonly int _playerObjectId;
    private readonly int _statusCode;

    public SM_ALLIANCE_READY_CHECK(int playerObjectId, int statusCode) : base(0xFA)
    {
        _playerObjectId = playerObjectId;
        _statusCode     = statusCode;
    }

    public override void Write(ref PacketWriter w)
    {
        w.WriteD(_playerObjectId);
        w.WriteC((byte)_statusCode);
    }
}
