using System.Collections.Generic;
using System.Net;
using AionLightning.Commons.Network;
using AionLightning.LoginServer.Model;
using AionLightning.LoginServer.Network.Gameserver;

namespace AionLightning.LoginServer
{
    public class GameServerInfo
    {
        public byte Id { get; }
        public string Ip { get; }
        public string Password { get; }
        public byte[] DefaultAddress { get; set; }
        public List<IPRange> IpRanges { get; set; }
        public int Port { get; set; }
        public GsConnection GscHandler { get; set; }
        public int MaxPlayers { get; set; }
        private readonly Dictionary<int, Account> _accountsOnGameServer = new Dictionary<int, Account>();

        public GameServerInfo(byte id, string ip, string password)
        {
            Id = id;
            Ip = ip;
            Password = password;
        }

        public bool IsOnline()
        {
            return GscHandler != null && GscHandler.State == GsConnection.State.AUTHED;
        }

        public int GetCurrentPlayers()
        {
            return _accountsOnGameServer.Count;
        }

        public void AddAccount(Account account)
        {
            _accountsOnGameServer.Add(account.Id, account);
        }

        public void RemoveAccount(Account account)
        {
            _accountsOnGameServer.Remove(account.Id);
        }

        public bool IsAccountOnGameServer(int accountId)
        {
            return _accountsOnGameServer.ContainsKey(accountId);
        }

        public Account GetAccount(int accountId)
        {
            _accountsOnGameServer.TryGetValue(accountId, out var account);
            return account;
        }

        public ICollection<Account> GetAccounts()
        {
            return _accountsOnGameServer.Values;
        }
    }
}
