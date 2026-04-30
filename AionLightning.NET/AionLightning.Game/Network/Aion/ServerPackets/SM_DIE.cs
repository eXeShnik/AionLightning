using AionLightning.Commons.Network;

namespace AionLightning.Game.Network.Aion.ServerPackets;

/// <summary>
/// Notifies the dying player's client of their death and available revive options. Opcode 0xC1.
/// </summary>
public sealed class SM_DIE : AionServerPacket
{
    private readonly bool _hasRebirth;
    private readonly bool _hasItem;
    private readonly int _remainingKiskTime;
    private readonly int _type;

    public SM_DIE(bool hasRebirth = false, bool hasItem = false,
        int remainingKiskTime = 0, int type = 0)
        : base(0xC1)
    {
        _hasRebirth        = hasRebirth;
        _hasItem           = hasItem;
        _remainingKiskTime = remainingKiskTime;
        _type              = type;
    }

    public override void Write(ref PacketWriter w)
    {
        w.WriteC(_hasRebirth ? (byte)1 : (byte)0);
        w.WriteC(_hasItem    ? (byte)1 : (byte)0);
        w.WriteD(_remainingKiskTime);
        w.WriteC((byte)_type);
        w.WriteC(0x00);
    }
}
