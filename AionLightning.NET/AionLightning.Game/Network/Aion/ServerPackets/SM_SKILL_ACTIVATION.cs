using AionLightning.Commons.Network;

namespace AionLightning.Game.Network.Aion.ServerPackets;

/// <summary>
/// Signals that a skill cast completed and its effect activates. Opcode 0x2E.
/// Sent after SM_CASTSPELL once cast time elapses.
/// </summary>
public sealed class SM_SKILL_ACTIVATION : AionServerPacket
{
    private readonly int  _skillId;
    private readonly bool _isActive;

    public SM_SKILL_ACTIVATION(int skillId, bool isActive = true) : base(0x2E)
    {
        _skillId  = skillId;
        _isActive = isActive;
    }

    public override void Write(ref PacketWriter w)
    {
        w.WriteH(_skillId);
        w.WriteD(0);                      // unk
        w.WriteC((byte)(_isActive ? 1 : 0));
    }
}
