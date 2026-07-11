using AionLightning.Commons.Network;

namespace AionLightning.Game.Network.Aion.ServerPackets;

/// <summary>
/// Self DP bar update (Java SM_STATUPDATE_DP). Opcode 0x06.
/// </summary>
public sealed class SM_STATUPDATE_DP : AionServerPacket
{
    private readonly int _currentDp;

    public SM_STATUPDATE_DP(int currentDp) : base(0x06)
    {
        _currentDp = currentDp;
    }

    public override void Write(ref PacketWriter w)
    {
        w.WriteH((short)_currentDp);
    }
}
