using System.Collections.Generic;
using System.Linq;
using AionLightning.Commons.Database.DAO;
using AionLightning.Commons.Network;
using AionLightning.Commons.Utils;
using AionLightning.LoginServer.Dao;
using AionLightning.LoginServer.Model;
using AionLightning.LoginServer.Network.Gameserver;
using AionLightning.LoginServer.Network.Gameserver.Serverpackets;
using Microsoft.Extensions.Logging;

namespace AionLightning.LoginServer
{
    public class GameServerTable
    {
        private static readonly ILogger<GameServerTable> _log = new LoggerFactory().CreateLogger<GameServerTable>();
        private static Dictionary<byte, GameServerInfo> _gameservers;

        public static ICollection<GameServerInfo> GetGameServers()
        {
            return _gameservers.Values;
        }

        public static void Load()
        {
            _gameservers = DAOManager.GetDAO<GameServersDAO>().GetAllGameServers().ToDictionary(g => g.Id);
            _log.LogInformation($"GameServerTable loaded {_gameservers.Count} registered GameServers.");
        }

        public static GsAuthResponse RegisterGameServer(GsConnection gsConnection, byte requestedId, byte[] defaultAddress,
            List<IPRange> ipRanges, int port, int maxPlayers, string password)
        {
            if (!_gameservers.TryGetValue(requestedId, out var gsi))
            {
                _log.LogInformation($"{gsConnection} requestedID={requestedId} not available!");
                return GsAuthResponse.NOT_AUTHED;
            }

            if (gsi.GscHandler != null)
            {
                return GsAuthResponse.ALREADY_REGISTERED;
            }

            if (gsi.Password != password || !NetworkUtils.CheckIPMatching(gsi.Ip, gsConnection.IP))
            {
                _log.LogInformation($"{gsi.Password} {password}");
                _log.LogInformation($"{gsConnection} wrong ip or password!");
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

        public static GameServerInfo GetGameServerInfo(byte gameServerId)
        {
            _gameservers.TryGetValue(gameServerId, out var gsi);
            return gsi;
        }

        public static bool IsAccountOnAnyGameServer(Account acc)
        {
            return GetGameServers().Any(gsi => gsi.IsAccountOnGameServer(acc.Id));
        }

        public static void KickAccountFromGameServer(Account account)
        {
            foreach (var gsi in GetGameServers())
            {
                if (gsi.IsAccountOnGameServer(account.Id))
                {
                    gsi.GscHandler.SendPacket(new SM_REQUEST_KICK_ACCOUNT(account.Id));
                    break;
                }
            }
        }
    }
}
