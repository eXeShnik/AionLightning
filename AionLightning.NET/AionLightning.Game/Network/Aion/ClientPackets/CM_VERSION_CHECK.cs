using AionLightning.Commons.Network;
using AionLightning.Game.Configs.Options;
using AionLightning.Game.Network.Aion.ServerPackets;

namespace AionLightning.Game.Network.Aion.ClientPackets;

public sealed class CM_VERSION_CHECK : AionClientPacket
{
    private readonly GsClientConnection _conn;
    private readonly GameServerInfoOptions _info;
    private readonly NetworkOptions _network;
    private readonly CsConnectionOptions _cs;
    private readonly GsOptions _gs;

    private int _version;

    public CM_VERSION_CHECK(GsClientConnection conn, GameServerInfoOptions info, NetworkOptions network,
        CsConnectionOptions cs, GsOptions gs)
    {
        _conn    = conn;
        _info    = info;
        _network = network;
        _cs      = cs;
        _gs      = gs;
    }

    public override void Read(ref PacketReader r)
    {
        _version      = r.ReadH();
        r.ReadH();  // subversion
        r.ReadD();  // windowsEncoding
        r.ReadD();  // windowsVersion
        r.ReadD();  // windowsSubVersion
        r.ReadC();  // always 2
    }

    public override async ValueTask RunAsync(CancellationToken ct)
        => await _conn.SendAsync(new SM_VERSION_CHECK(_version, _info, _network, _cs, _gs), ct);
}
