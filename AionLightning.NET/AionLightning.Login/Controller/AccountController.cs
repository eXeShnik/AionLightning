using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Text;
using AionLightning.Login.Configs.Options;
using AionLightning.Login.Dao;
using AionLightning.Login.Model;
using AionLightning.Login.Network.Aion;
using AionLightning.Login.Network.GameServer;
using AionLightning.Login.Network.GameServer.ServerPackets;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AionLightning.Login.Controller;

public sealed class AccountController : IAccountController
{
    private readonly ILogger<AccountController> _log;
    private readonly IAccountDao _accountDao;
    private readonly BannedIpController _bannedIpCtrl;
    private readonly IOptions<AccountsOptions> _accounts;
    private readonly IOptions<MaintenanceOptions> _maintenance;
    private readonly ConcurrentDictionary<int, Account> _accountsOnLs = new();

    public AccountController(
        ILogger<AccountController> log,
        IAccountDao accountDao,
        BannedIpController bannedIpCtrl,
        IOptions<AccountsOptions> accounts,
        IOptions<MaintenanceOptions> maintenance)
    {
        _log = log;
        _accountDao = accountDao;
        _bannedIpCtrl = bannedIpCtrl;
        _accounts = accounts;
        _maintenance = maintenance;
    }

    public async Task<(AionAuthResponse Response, Account? Account)> LoginAsync(
        string login, string password, string ip, CancellationToken ct = default)
    {
        if (_bannedIpCtrl.IsBanned(ip))
            return (AionAuthResponse.IP_BANNED, null);

        var account = await _accountDao.FindByNameAsync(login, ct);

        if (account == null)
        {
            if (!_accounts.Value.AutoCreate)
                return (AionAuthResponse.INVALID_PASSWORD, null);

            account = new Account
            {
                Name = login,
                Password = HashPassword(password),
                LastIp = ip
            };
            account.Id = await _accountDao.InsertAsync(account, ct);
            _log.LogInformation("Auto-created account {Name}", login);
        }
        else
        {
            if (account.Password != HashPassword(password))
                return (AionAuthResponse.INVALID_PASSWORD, null);
        }

        if (_maintenance.Value.Enabled && account.AccessLevel < _maintenance.Value.GmLevel)
            return (AionAuthResponse.GM_ONLY, null);

        if (account.Activated < 0)
            return (AionAuthResponse.IP_BANNED, null);

        if (_accountsOnLs.ContainsKey(account.Id) || GameServerTable.IsAccountOnAnyGameServer(account))
            return (AionAuthResponse.ALREADY_LOGGED_IN, null);

        await _accountDao.UpdateLastIpAsync(account.Id, ip, ct);
        return (AionAuthResponse.AUTHED, account);
    }

    public void RegisterAccount(Account account)
    {
        _accountsOnLs[account.Id] = account;
    }

    public void UnregisterAccount(int accountId)
    {
        _accountsOnLs.TryRemove(accountId, out _);
    }

    public async Task CheckAuthAsync(SessionKey key, GsConnection gsConnection, CancellationToken ct = default)
    {
        if (!_accountsOnLs.TryGetValue(key.AccountId, out var account)
            || account.SessionKey == null
            || !account.SessionKey.CheckSessionKey(key))
        {
            await gsConnection.SendAsync(new SM_ACCOUNT_AUTH_RESPONSE(key.AccountId, false), ct);
            return;
        }

        _accountsOnLs.TryRemove(key.AccountId, out _);

        var gsi = gsConnection.GameServerInfo!;
        gsi.AddAccount(account);

        account.LastServer = (sbyte)gsi.Id;
        await _accountDao.UpdateLastServerAsync(account.Id, (sbyte)gsi.Id, ct);

        long toll = account.Toll ?? 0;
        await gsConnection.SendAsync(new SM_ACCOUNT_AUTH_RESPONSE(
            key.AccountId, true, account.Name, account.AccessLevel, account.Membership, toll), ct);

        _log.LogInformation("Account {Name} authenticated on GS #{Id}", account.Name, gsi.Id);
    }

    private static string HashPassword(string password)
    {
        var hash = SHA1.HashData(Encoding.UTF8.GetBytes(password));
        return Convert.ToBase64String(hash);
    }
}
