using System.Collections.Generic;
using System.Net;
using AionLightning.Commons.Network;
using AionLightning.Login.Model;
using AionLightning.Login.Network.GameServer;

namespace AionLightning.Login;

public sealed class GameServerInfo
{
    public byte Id { get; }
    public string Ip { get; }
    public string Password { get; }
    public byte[] DefaultAddress { get; set; } = Array.Empty<byte>();
    public List<IPRange> IpRanges { get; set; } = new();
    public int Port { get; set; }
    public GsConnection? GscHandler { get; set; }
    public int MaxPlayers { get; set; }

    private readonly Dictionary<int, Account> _accountsOnGameServer = new();

    public GameServerInfo(byte id, string ip, string password)
    {
        Id = id;
        Ip = ip;
        Password = password;
    }

    public bool IsOnline => GscHandler != null && GscHandler.State == GsConnection.GsState.AUTHED;

    public int GetCurrentPlayers() => _accountsOnGameServer.Count;

    public bool IsFull() => MaxPlayers > 0 && _accountsOnGameServer.Count >= MaxPlayers;

    public void AddAccount(Account account) => _accountsOnGameServer[account.Id] = account;

    public void RemoveAccount(Account account) => _accountsOnGameServer.Remove(account.Id);

    public bool IsAccountOnGameServer(int accountId) => _accountsOnGameServer.ContainsKey(accountId);

    public Account? GetAccount(int accountId)
    {
        _accountsOnGameServer.TryGetValue(accountId, out var account);
        return account;
    }

    public ICollection<Account> GetAccounts() => _accountsOnGameServer.Values;

    public byte[] GetIpAddressForPlayer(string playerIp)
    {
        foreach (var range in IpRanges)
        {
            if (range.IsInRange(playerIp))
                return range.GetAddress().GetAddressBytes();
        }
        return DefaultAddress;
    }
}
