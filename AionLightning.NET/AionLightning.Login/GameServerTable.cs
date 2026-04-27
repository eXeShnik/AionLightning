using AionLightning.Commons.Network;
using AionLightning.Login.Model;
using AionLightning.Login.Network.GameServer;
using Microsoft.Extensions.Logging;

namespace AionLightning.Login;

/// <summary>
/// In-memory registry of known game servers.
/// TODO M3: seed from DB via GameServersDAO (Dapper). Until then, Load() yields empty.
/// </summary>
public static class GameServerTable
{
    private static readonly ILogger _log =
        LoggerFactory.Create(b => b.AddConsole()).CreateLogger(nameof(GameServerTable));

    private static Dictionary<byte, GameServerInfo> _servers = new();

    public static IReadOnlyDictionary<byte, GameServerInfo> Instance => _servers;

    public static ICollection<GameServerInfo> GetGameServers() => _servers.Values;

    public static void Load()
    {
        // TODO M3: load from DB
        _servers = new Dictionary<byte, GameServerInfo>();
        _log.LogInformation("GameServerTable loaded {Count} registered servers (stub)", _servers.Count);
    }

    public static GsAuthResponse RegisterGameServer(
        GsConnection gsConnection, byte requestedId,
        byte[] defaultAddress, List<IPRange> ipRanges,
        int port, int maxPlayers, string password)
    {
        if (!_servers.TryGetValue(requestedId, out var gsi))
        {
            _log.LogInformation("{Conn} requested unknown server id={Id}", gsConnection.IP, requestedId);
            return GsAuthResponse.NOT_AUTHED;
        }

        if (gsi.GscHandler != null)
            return GsAuthResponse.ALREADY_REGISTERED;

        if (gsi.Password != password)
        {
            _log.LogInformation("{Conn} wrong password for server id={Id}", gsConnection.IP, requestedId);
            return GsAuthResponse.NOT_AUTHED;
        }

        gsi.DefaultAddress = defaultAddress;
        gsi.IpRanges = ipRanges;
        gsi.Port = port;
        gsi.MaxPlayers = maxPlayers;
        gsi.GscHandler = gsConnection;
        gsConnection.GameServerInfo = gsi;
        return GsAuthResponse.AUTHED;
    }

    public static GameServerInfo? GetGameServerInfo(byte id)
    {
        _servers.TryGetValue(id, out var gsi);
        return gsi;
    }

    public static bool IsAccountOnAnyGameServer(Account acc) =>
        GetGameServers().Any(gsi => gsi.IsAccountOnGameServer(acc.Id));

    public static void KickAccountFromGameServer(Account account)
    {
        foreach (var gsi in GetGameServers())
        {
            if (!gsi.IsAccountOnGameServer(account.Id)) continue;
            // TODO M2: gsi.GscHandler?.SendPacketAsync(new SM_REQUEST_KICK_ACCOUNT(account.Id))
            break;
        }
    }
}
