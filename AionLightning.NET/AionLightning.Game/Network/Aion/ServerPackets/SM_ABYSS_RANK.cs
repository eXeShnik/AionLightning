using AionLightning.Commons.Network;

namespace AionLightning.Game.Network.Aion.ServerPackets;

/// <summary>Sends the player's abyss rank and AP/GP statistics. Opcode 0xED.</summary>
public sealed class SM_ABYSS_RANK : AionServerPacket
{
    public SM_ABYSS_RANK() : base(0xED) { }

    public override void Write(ref PacketWriter w)
    {
        w.WriteQ(0);   // AP (accumulated points)
        w.WriteD(0);   // GP (glory points)
        w.WriteD(1);   // current rank ID (1 = lowest: Soldier Rank 9 / Recruit)
        w.WriteD(0);   // top ranking position
        // rank 1 is ≤ 18, so write progress toward next rank
        w.WriteD(0);   // progress percent toward next rank
        w.WriteD(0);   // all-time kill count
        w.WriteD(1);   // max rank achieved
        w.WriteD(0);   // daily kill count
        w.WriteQ(0);   // daily AP gained
        w.WriteD(0);   // daily GP gained
        w.WriteD(0);   // weekly kill count
        w.WriteQ(0);   // weekly AP gained
        w.WriteD(0);   // weekly GP gained
        w.WriteD(0);   // last-week kill count
        w.WriteQ(0);   // last-week AP gained
        w.WriteD(0);   // last-week GP gained
        w.WriteC(0);   // unk
    }
}
