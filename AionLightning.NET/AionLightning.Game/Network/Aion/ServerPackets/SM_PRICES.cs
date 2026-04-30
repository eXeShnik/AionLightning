using AionLightning.Commons.Network;

namespace AionLightning.Game.Network.Aion.ServerPackets;

/// <summary>Sends global NPC buy/sell price modifiers. Opcode 0xFC.</summary>
public sealed class SM_PRICES : AionServerPacket
{
    public SM_PRICES() : base(0xFC) { }

    public override void Write(ref PacketWriter w)
    {
        w.WriteC(0); // global prices factor
        w.WriteC(0); // global prices modifier
        w.WriteC(0); // taxes
    }
}
