using AionLightning.Commons.Network;

namespace AionLightning.Game.Network.Aion.ServerPackets;

/// <summary>
/// Java network.aion.serverpackets.SM_RECEIVE_BIDS — tells the client its house-bid list is stale and
/// should be re-requested (via CM_GET_HOUSE_BIDS). Sent after a successful CM_PLACE_BID and on login when
/// pending auction-result mail is found. Opcode unknown in the 4.5-era table this port otherwise follows —
/// Java's own <c>unk</c> constructor argument (always 0 at every call site) suggests a nearly-empty
/// single-int packet body; the opcode below is a placeholder.
/// TODO: verify opcode vs live 4.6 client capture.
/// </summary>
public sealed class SM_RECEIVE_BIDS : AionServerPacket
{
    private readonly int _unk;

    public SM_RECEIVE_BIDS(int unk) : base(0x1FE)
    {
        _unk = unk;
    }

    public override void Write(ref PacketWriter w)
    {
        w.WriteD(_unk);
    }
}
