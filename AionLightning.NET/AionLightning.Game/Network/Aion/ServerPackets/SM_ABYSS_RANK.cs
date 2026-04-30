using AionLightning.Commons.Network;

namespace AionLightning.Game.Network.Aion.ServerPackets;

/// <summary>Sends the player's abyss rank and AP/GP statistics. Opcode 0xED.</summary>
public sealed class SM_ABYSS_RANK : AionServerPacket
{
    private readonly long _ap;
    private readonly int  _rank;

    public SM_ABYSS_RANK(long ap = 0, int rank = 1) : base(0xED)
    {
        _ap   = ap;
        _rank = rank;
    }

    public override void Write(ref PacketWriter w)
    {
        w.WriteQ(_ap);     // accumulated AP
        w.WriteD(0);       // GP (glory points — not implemented)
        w.WriteD(_rank);   // current rank ID
        w.WriteD(0);       // top ranking position
        w.WriteD(0);       // progress percent toward next rank
        w.WriteD(0);       // all-time kill count
        w.WriteD(_rank);   // max rank achieved (same as current)
        w.WriteD(0);       // daily kill count
        w.WriteQ(0);       // daily AP gained
        w.WriteD(0);       // daily GP gained
        w.WriteD(0);       // weekly kill count
        w.WriteQ(0);       // weekly AP gained
        w.WriteD(0);       // weekly GP gained
        w.WriteD(0);       // last-week kill count
        w.WriteQ(0);       // last-week AP gained
        w.WriteD(0);       // last-week GP gained
        w.WriteC(0);       // unk
    }
}
