using AionLightning.Commons.Network;

namespace AionLightning.Game.Network.Aion.ServerPackets;

/// <summary>
/// Removes a skill from the client's skill window (Java SM_SKILL_REMOVE). Opcode 0x2D.
/// </summary>
public sealed class SM_SKILL_REMOVE : AionServerPacket
{
    private readonly int  _skillId;
    private readonly int  _skillLevel;
    private readonly bool _isStigma;

    public SM_SKILL_REMOVE(int skillId, int skillLevel, bool isStigma) : base(0x2D)
    {
        _skillId    = skillId;
        _skillLevel = skillLevel;
        _isStigma   = isStigma;
    }

    public override void Write(ref PacketWriter w)
    {
        w.WriteH((short)_skillId);
        if (_skillId is >= 30001 and <= 30003 or >= 40001 and <= 40010)
        {
            // crafting/gathering professions
            w.WriteC(0);
            w.WriteC(0);
        }
        else if (_isStigma)
        {
            w.WriteC(1);
            w.WriteC(1);
        }
        else
        {
            w.WriteC((byte)_skillLevel);
        }
    }
}
