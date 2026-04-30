using AionLightning.Commons.Network;

namespace AionLightning.Game.Network.Aion.ServerPackets;

/// <summary>Gather status notification. Opcode 0xA4.</summary>
public sealed class SM_GATHER_STATUS : AionServerPacket
{
    public enum Status { Start = 0, Success = 1, Fail = 2, Complete = 3 }

    private readonly int _playerObjectId;
    private readonly int _gatherableObjectId;
    private readonly Status _status;

    public SM_GATHER_STATUS(int playerObjectId, int gatherableObjectId, Status status) : base(0xA4)
    {
        _playerObjectId    = playerObjectId;
        _gatherableObjectId = gatherableObjectId;
        _status            = status;
    }

    public override void Write(ref PacketWriter w)
    {
        w.WriteD(_playerObjectId);
        w.WriteD(_gatherableObjectId);
        w.WriteH(0);
        w.WriteC((byte)_status);
    }
}
