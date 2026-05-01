using AionLightning.Commons.Network;

namespace AionLightning.Game.Network.Aion.ServerPackets;

/// <summary>
/// Confirms or rejects a windstream state change to the client. Opcode 0xA3.
/// state matches the CM_WINDSTREAM state; unk2 = 1 means accepted.
/// </summary>
public sealed class SM_WINDSTREAM : AionServerPacket
{
    private readonly int _state;
    private readonly int _unk2;

    public SM_WINDSTREAM(int state, int unk2 = 1) : base(0xA3)
    {
        _state = state;
        _unk2  = unk2;
    }

    public override void Write(ref PacketWriter w)
    {
        w.WriteD(_state);
        w.WriteC((byte)_unk2);
    }
}
