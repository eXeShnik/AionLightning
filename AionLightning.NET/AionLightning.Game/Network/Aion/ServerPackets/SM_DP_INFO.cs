using AionLightning.Commons.Network;

namespace AionLightning.Game.Network.Aion.ServerPackets;

/// <summary>Notifies the client of the player's current Divine Power (DP). Opcode 0x07.</summary>
public sealed class SM_DP_INFO : AionServerPacket
{
    private readonly int _playerObjectId;
    private readonly int _dp;

    public SM_DP_INFO(int playerObjectId, int dp) : base(0x07)
    {
        _playerObjectId = playerObjectId;
        _dp             = dp;
    }

    public override void Write(ref PacketWriter w)
    {
        w.WriteD(_playerObjectId);
        w.WriteH(_dp);
    }
}
