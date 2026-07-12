using System.Net;
using AionLightning.Commons.Network;
using AionLightning.Game.Configs.Options;

namespace AionLightning.Game.Network.Aion.ServerPackets;

/// <summary>
/// Sent in response to CM_VERSION_CHECK. Accepted version >= 204; below that sends 0x02 (wrong version).
/// </summary>
public sealed class SM_VERSION_CHECK : AionServerPacket
{
    private readonly int _clientVersion;
    private readonly GameServerInfoOptions _info;
    private readonly NetworkOptions _network;
    private readonly CsConnectionOptions _cs;
    private readonly GsOptions _gs;

    public SM_VERSION_CHECK(int clientVersion, GameServerInfoOptions info, NetworkOptions network,
        CsConnectionOptions cs, GsOptions gs)
        : base(0x00)
    {
        _clientVersion = clientVersion;
        _info    = info;
        _network = network;
        _cs      = cs;
        _gs      = gs;
    }

    public override void Write(ref PacketWriter w)
    {
        if (_clientVersion < 204)
        {
            w.WriteC(0x02); // wrong version
            return;
        }

        w.WriteC(0x00);                           // accepted
        w.WriteC((byte)_info.Id);                 // server id
        w.WriteD(141031);                         // start date yymmdd
        w.WriteD(140820);                         // start date yymmdd
        w.WriteD(0x00);
        w.WriteD(140922);                         // year month day
        w.WriteD(1415179894);                     // server time (unix)
        w.WriteC(0x00);
        w.WriteC((byte)_gs.CountryCode);          // country code (Java GSConfig.SERVER_COUNTRY_CODE) — must match the client's region
        w.WriteC(0x00);
        w.WriteC(0x10);                           // serverMode: 1 char slot, no factions restriction

        w.WriteD((int)(DateTimeOffset.UtcNow.ToUnixTimeSeconds()));
        w.WriteH(350);
        w.WriteH(1281);
        w.WriteH(2575);
        w.WriteH(257);
        w.WriteH(322);
        w.WriteH(0x02);
        w.WriteC(10);  // CHARACTER_REENTRY_TIME
        w.WriteC(0x00);
        w.WriteC(0x00);
        w.WriteC(0x00);
        w.WriteC(0x00);
        w.WriteC(0x00);
        w.WriteD(unchecked((int)0xFFFFF1F0));
        w.WriteD(0x62917804);
        w.WriteC(0x02);
        w.WriteD(0x00);
        w.WriteD(0x00);
        w.WriteC(0x00);
        w.WriteH(0x0BB8);
        w.WriteH(0x0001);
        w.WriteC(0x00);
        w.WriteC(0x01);
        w.WriteD(0x00);
        w.WriteH(0x01); // 1 chat entry

        // Chat server entry (spacer + address + port)
        w.WriteC(0x00);
        byte[] chatAddr = IPAddress.TryParse(_cs.Host, out var ip)
            ? ip.GetAddressBytes()
            : new byte[] { 127, 0, 0, 1 };
        w.WriteB(chatAddr);
        w.WriteH((short)_cs.Port);
    }
}
