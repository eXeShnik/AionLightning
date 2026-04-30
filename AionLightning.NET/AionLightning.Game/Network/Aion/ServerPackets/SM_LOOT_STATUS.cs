using AionLightning.Commons.Network;

namespace AionLightning.Game.Network.Aion.ServerPackets;

/// <summary>Opens or closes the loot window for a target. Opcode 0xCD.</summary>
public sealed class SM_LOOT_STATUS : AionServerPacket
{
    public enum State : byte { Open = 0, Close = 1, Locked = 2, Empty = 3 }

    private readonly int _targetObjectId;
    private readonly State _state;

    public SM_LOOT_STATUS(int targetObjectId, State state) : base(0xCD)
    {
        _targetObjectId = targetObjectId;
        _state          = state;
    }

    public override void Write(ref PacketWriter w)
    {
        w.WriteD(_targetObjectId);
        w.WriteC((byte)_state);
    }
}
