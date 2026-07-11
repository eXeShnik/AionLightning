using AionLightning.Commons.Network;

namespace AionLightning.Game.Network.Aion.ServerPackets;

/// <summary>
/// Self MP bar update (Java SM_STATUPDATE_MP). Opcode 0x04.
/// Sent to the owner whenever their MP changes.
/// </summary>
public sealed class SM_STATUPDATE_MP : AionServerPacket
{
    private readonly int _currentMp;
    private readonly int _maxMp;

    public SM_STATUPDATE_MP(int currentMp, int maxMp) : base(0x04)
    {
        _currentMp = currentMp;
        _maxMp = maxMp;
    }

    public override void Write(ref PacketWriter w)
    {
        w.WriteD(_currentMp);
        w.WriteD(_maxMp);
    }
}
