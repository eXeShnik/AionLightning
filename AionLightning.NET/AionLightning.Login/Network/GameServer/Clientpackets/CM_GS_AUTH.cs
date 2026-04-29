using AionLightning.Commons.Network;
using AionLightning.Login.Network.GameServer.ServerPackets;

namespace AionLightning.Login.Network.GameServer.Clientpackets;

public sealed class CM_GS_AUTH : GsClientPacket
{
    private readonly GsConnection _conn;
    private byte _gameServerId;
    private byte[] _defaultAddress = Array.Empty<byte>();
    private List<IPRange> _ipRanges = new();
    private int _port;
    private int _maxPlayers;
    private string _password = string.Empty;

    public CM_GS_AUTH(GsConnection conn)
    {
        _conn = conn;
    }

    public override void Read(ref PacketReader r)
    {
        _gameServerId = r.ReadC();

        byte addrLen = r.ReadC();
        _defaultAddress = r.ReadB(addrLen);

        int rangeCount = r.ReadD();
        _ipRanges = new List<IPRange>(rangeCount);
        for (int i = 0; i < rangeCount; i++)
        {
            var min = r.ReadB(r.ReadC());
            var max = r.ReadB(r.ReadC());
            var addr = r.ReadB(r.ReadC());
            _ipRanges.Add(new IPRange(min, max, addr));
        }

        _port = r.ReadH();
        _maxPlayers = r.ReadD();
        _password = r.ReadS();
    }

    public override async ValueTask RunAsync(CancellationToken ct)
    {
        var resp = GameServerTable.RegisterGameServer(
            _conn, _gameServerId, _defaultAddress, _ipRanges, _port, _maxPlayers, _password);

        var serverCount = (byte)GameServerTable.GetGameServers().Count;

        if (resp == GsAuthResponse.AUTHED)
        {
            _conn.State = GsConnection.GsState.AUTHED;
            await _conn.SendAsync(new SM_GS_AUTH_RESPONSE(resp, serverCount), ct);
            await _conn.SendAsync(new SM_MACBAN_LIST(), ct);
        }
        else
        {
            await _conn.SendAsync(new SM_GS_AUTH_RESPONSE(resp), ct);
            await _conn.DisposeAsync();
        }
    }
}
