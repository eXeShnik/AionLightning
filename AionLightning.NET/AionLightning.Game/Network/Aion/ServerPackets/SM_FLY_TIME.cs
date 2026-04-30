using AionLightning.Commons.Network;

namespace AionLightning.Game.Network.Aion.ServerPackets;

/// <summary>Sends current and max flight points to the player. Opcode 0xF4.</summary>
public sealed class SM_FLY_TIME : AionServerPacket
{
    private readonly int _currentFp;
    private readonly int _maxFp;

    public SM_FLY_TIME(int currentFp, int maxFp) : base(0xF4)
    {
        _currentFp = currentFp;
        _maxFp     = maxFp;
    }

    public override void Write(ref PacketWriter w)
    {
        w.WriteD(_currentFp);
        w.WriteD(_maxFp);
    }
}
