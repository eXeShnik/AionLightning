using AionLightning.Commons.Network;

namespace AionLightning.Game.Network.Aion.ServerPackets;

/// <summary>
/// Shows the mantra/aura visual around a player (Java SM_MANTRA_EFFECT). Opcode 0xD0.
/// Broadcast when an aura skill starts.
/// </summary>
public sealed class SM_MANTRA_EFFECT : AionServerPacket
{
    private readonly int _playerObjectId;
    private readonly int _skillId;

    public SM_MANTRA_EFFECT(int playerObjectId, int skillId) : base(0xD0)
    {
        _playerObjectId = playerObjectId;
        _skillId        = skillId;
    }

    public override void Write(ref PacketWriter w)
    {
        w.WriteD(0);
        w.WriteD(_playerObjectId);
        w.WriteH((short)_skillId);
    }
}
