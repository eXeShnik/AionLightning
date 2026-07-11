using AionLightning.Commons.Network;

namespace AionLightning.Game.Network.Aion.ServerPackets;

/// <summary>Closes the master's summon panel UI. Opcode 0x49.</summary>
public sealed class SM_SUMMON_PANEL_REMOVE : AionServerPacket
{
    public SM_SUMMON_PANEL_REMOVE() : base(0x49) { }

    public override void Write(ref PacketWriter w)
    {
        w.WriteH(0); // unk
        w.WriteC(0); // possible mod
    }
}
