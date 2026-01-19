using System.Collections.Generic;
using AionLightning.Commons.Database.DAO;
using AionLightning.Commons.Utils;
using AionLightning.LoginServer.Configs;
using AionLightning.LoginServer.Dao;
using AionLightning.LoginServer.Model;
using AionLightning.LoginServer.Network.Aion;
using AionLightning.LoginServer.Network.Aion.Serverpackets;
using AionLightning.LoginServer.Network.Gameserver;
using AionLightning.LoginServer.Network.Gameserver.Serverpackets;
using Microsoft.Extensions.Logging;

namespace AionLightning.LoginServer.Controller
{
    public class AccountController
    {
        private static readonly ILogger<AccountController> _log = new LoggerFactory().CreateLogger<AccountController>();
        private static readonly Dictionary<int, ReconnectingAccount> _reconnectingAccounts = new Dictionary<int, ReconnectingAccount>();

        public static void CheckAuth(SessionKey sessionKey, GsConnection gsConnection)
        {
            var account = GetAccount(sessionKey.AccountId, gsConnection.GameServerInfo);
            bool ok = false;

            if (account != null && account.SessionKey.CheckSessionKey(sessionKey))
            {
                ok = true;
                account.GameServerInfo = gsConnection.GameServerInfo;
                gsConnection.GameServerInfo.AddAccount(account);
            }

            gsConnection.SendPacket(new SM_ACCOUNT_AUTH_RESPONSE(sessionKey.AccountId, ok));
        }

        public static Account GetAccount(int accountId, GameServerInfo gameServerInfo)
        {
            var account = gameServerInfo.GetAccount(accountId);
            if (account != null)
                return account;

            foreach (var gsi in GameServerTable.GetGameServers())
            {
                account = gsi.GetAccount(accountId);
                if (account != null)
                    return account;
            }

            return null;
        }

        public static void Authenticate(string user, string password, LoginConnection client)
        {
            var account = DAOManager.GetDAO<AccountDAO>().GetAccount(user);

            if (account == null)
            {
                if (Config.ACCOUNT_AUTO_CREATION)
                {
                    account = new Account
                    {
                        Name = user,
                        Password = password,
                        AccessLevel = 0,
                        Membership = 0,
                        Activated = 1,
                        LastIp = client.IP,
                        LastMac = "xx-xx-xx-xx-xx-xx"
                    };
                    DAOManager.GetDAO<AccountDAO>().InsertAccount(account);
                    Authenticate(user, password, client);
                }
                else
                {
                    client.SendPacket(new SM_LOGIN_FAIL(AionAuthResponse.INVALID_PASSWORD));
                }
                return;
            }

            if (!account.Password.Equals(password))
            {
                client.SendPacket(new SM_LOGIN_FAIL(AionAuthResponse.INVALID_PASSWORD));
                return;
            }

            if (account.AccessLevel < 0)
            {
                client.SendPacket(new SM_LOGIN_FAIL(AionAuthResponse.BANNED));
                return;
            }

            if (GameServerTable.IsAccountOnAnyGameServer(account))
            {
                GameServerTable.KickAccountFromGameServer(account);
                client.SendPacket(new SM_LOGIN_FAIL(AionAuthResponse.ALREADY_LOGGED_IN));
                return;
            }

            client.Account = account;
            account.SessionKey = new SessionKey(account);
            client.State = LoginConnection.State.AUTHED_LOGIN;
            client.SendPacket(new SM_LOGIN_OK(account.SessionKey));
        }

        public static void Logout(int accountId, GsConnection gsConnection)
        {
            var account = gsConnection.GameServerInfo.GetAccount(accountId);
            if (account != null)
            {
                gsConnection.GameServerInfo.RemoveAccount(account);
                account.GameServerInfo = null;
            }
        }

        public static void Login(SessionKey sk, LoginConnection client)
        {
            var account = client.Account;
            if (account.SessionKey.Equals(sk))
            {
                if (GameServerTable.IsAccountOnAnyGameServer(account))
                {
                    GameServerTable.KickAccountFromGameServer(account);
                    client.Close(new SM_LOGIN_FAIL(AionAuthResponse.ALREADY_LOGGED_IN), true);
                }
                else
                {
                    client.State = LoginConnection.State.AUTHED_LOGIN;
                    client.SendPacket(new SM_LOGIN_OK(sk));
                }
            }
            else
            {
                client.Close(new SM_LOGIN_FAIL(AionAuthResponse.SYSTEM_ERROR), true);
            }
        }

        public static void Play(SessionKey key, byte serverId, LoginConnection client)
        {
            var account = client.Account;
            if (account.SessionKey.Equals(key))
            {
                var gsi = GameServerTable.GetGameServerInfo(serverId);
                if (gsi == null || !gsi.IsOnline())
                {
                    client.Close(new SM_PLAY_FAIL(AionAuthResponse.SYSTEM_ERROR), true);
                    return;
                }

                if (gsi.GetCurrentPlayers() >= gsi.MaxPlayers)
                {
                    client.Close(new SM_PLAY_FAIL(AionAuthResponse.SERVER_FULL), true);
                    return;
                }

                if (account.AccessLevel < gsi.RequiredAccessLevel)
                {
                    client.Close(new SM_PLAY_FAIL(AionAuthResponse.GM_ONLY), true);
                    return;
                }

                client.State = LoginConnection.State.AUTHED_GS;
                client.GameServerInfo = gsi;
                client.SendPacket(new SM_PLAY_OK(key, serverId));
            }
            else
            {
                client.Close(new SM_PLAY_FAIL(AionAuthResponse.SYSTEM_ERROR), true);
            }
        }
    }
}
