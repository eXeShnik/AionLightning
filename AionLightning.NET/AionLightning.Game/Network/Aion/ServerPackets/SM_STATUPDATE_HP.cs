using AionLightning.Commons.Network;

namespace AionLightning.Game.Network.Aion.ServerPackets;

/// <summary>
/// Self HP bar update (Java SM_STATUPDATE_HP). Opcode 0x03.
/// Sent to the owner whenever their HP changes.
/// </summary>
public sealed class SM_STATUPDATE_HP : AionServerPacket
{
    private readonly int _currentHp;
    private readonly int _maxHp;

    public SM_STATUPDATE_HP(int currentHp, int maxHp) : base(0x03)
    {
        _currentHp = currentHp;
        _maxHp = maxHp;
    }

    public override void Write(ref PacketWriter w)
    {
        w.WriteD(_currentHp);
        w.WriteD(_maxHp);
    }
}
