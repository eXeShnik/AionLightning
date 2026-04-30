using AionLightning.Commons.Network;

namespace AionLightning.Game.Network.Aion.ServerPackets;

/// <summary>Use-object animation packet. Opcode 0xA3.</summary>
public sealed class SM_USE_OBJECT : AionServerPacket
{
    private readonly int _playerObjectId;
    private readonly int _targetObjectId;
    private readonly int _time;
    private readonly int _actionType;

    public SM_USE_OBJECT(int playerObjectId, int targetObjectId, int time, int actionType) : base(0xA3)
    {
        _playerObjectId = playerObjectId;
        _targetObjectId = targetObjectId;
        _time           = time;
        _actionType     = actionType;
    }

    public override void Write(ref PacketWriter w)
    {
        w.WriteD(_playerObjectId);
        w.WriteD(_targetObjectId);
        w.WriteD(_time);
        w.WriteC((byte)_actionType);
    }
}
