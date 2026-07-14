using AionLightning.Commons.Network;

namespace AionLightning.Game.Network.Aion.ServerPackets;

/// <summary>
/// Serial-killer rank notification (Java network.aion.serverpackets.SM_SERIAL_KILLER). Opcode 0x54
/// (Java comment: "// 4.5"). TODO: verify opcode/byte layout against a live 4.6 client capture — this
/// port has not been byte-diff-verified the way the login packets were.
///
/// Only the type 0/1 "self rank changed" variant is ported (sent to the affected player when their own
/// SerialKillerRank changes — Java's <c>new SM_SERIAL_KILLER(showMsg, rank)</c>). Java's type 4 "nearby
/// serial killers" variant (a <c>Collection&lt;Player&gt;</c> map-icon broadcast showing enemy-race
/// observers every active SK's rank/position/name/level, sent from onEnterMap/onLeaveMap/updateIcons/
/// updateRank) is not ported: it needs a per-world-instance visitor/broadcast registry this service
/// does not build (see SerialKillerService's class doc). Nearby observers still learn about a serial
/// killer only via the reward buff granted when one is killed.
/// </summary>
public sealed class SM_SERIAL_KILLER : AionServerPacket
{
    private readonly int _type;
    private readonly int _rank;

    public SM_SERIAL_KILLER(bool showMessage, int rank) : base(0x54)
    {
        _type = showMessage ? 1 : 0;
        _rank = rank;
    }

    public override void Write(ref PacketWriter w)
    {
        w.WriteD(_type);
        w.WriteD(0x01);
        w.WriteD(0x01);
        w.WriteH(0x01);
        w.WriteD(_rank);
    }
}
