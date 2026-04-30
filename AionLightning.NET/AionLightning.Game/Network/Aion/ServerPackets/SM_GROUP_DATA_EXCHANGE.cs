using AionLightning.Commons.Network;

namespace AionLightning.Game.Network.Aion.ServerPackets;

/// <summary>Forwards group UI data (minimap markers, target indicators). Opcode 0x53.</summary>
public sealed class SM_GROUP_DATA_EXCHANGE : AionServerPacket
{
    private readonly byte[] _data;
    private readonly int    _action;
    private readonly int    _unk2;

    /// <param name="action">1 = self-only echo; other values = group relay with unk2</param>
    public SM_GROUP_DATA_EXCHANGE(byte[] data, int action = 1, int unk2 = 0) : base(0x53)
    {
        _data   = data;
        _action = action;
        _unk2   = unk2;
    }

    public override void Write(ref PacketWriter w)
    {
        w.WriteC((byte)_action);
        if (_action != 1)
            w.WriteC((byte)_unk2);
        w.WriteD(_data.Length);
        w.WriteB(_data);
    }
}
