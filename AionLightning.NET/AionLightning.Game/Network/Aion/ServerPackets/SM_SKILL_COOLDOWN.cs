using AionLightning.Commons.Network;

namespace AionLightning.Game.Network.Aion.ServerPackets;

/// <summary>Sends skill cooldown state. Opcode 0x33.</summary>
public sealed class SM_SKILL_COOLDOWN : AionServerPacket
{
    public SM_SKILL_COOLDOWN() : base(0x33) { }

    public override void Write(ref PacketWriter w)
    {
        w.WriteH(0); // count of cooldown entries
        w.WriteC(1); // unk flag (always 1)
    }
}
