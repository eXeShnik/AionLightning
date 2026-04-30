using AionLightning.Commons.Network;

namespace AionLightning.Game.Network.Aion.ServerPackets;

/// <summary>Sends UI layout / shortcut data blob to the client. Opcode 0x1E.</summary>
public sealed class SM_UI_SETTINGS : AionServerPacket
{
    private readonly int    _type;
    private readonly byte[] _data;

    public SM_UI_SETTINGS(int type, byte[] data) : base(0x1E)
    {
        _type = type;
        _data = data;
    }

    public override void Write(ref PacketWriter w)
    {
        w.WriteH((short)_type);
        w.WriteC(28);       // constant matching Java
        w.WriteB(_data);
        // pad to 7168 bytes total
        int remaining = 7168 - _data.Length;
        if (remaining > 0)
            w.WriteB(new byte[remaining]);
    }
}
