using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Text;
using AionLightning.Login.Configs.Options;
using AionLightning.Login.Dao;
using AionLightning.Login.Model;
using AionLightning.Login.Network.Aion;
using AionLightning.Login.Network.Aion.ServerPackets;
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
            if (account == null)
                _log.LogWarning("GS auth DENIED: account {Id} not registered on LS (accounts on LS: [{Ids}])",
                    key.AccountId, string.Join(",", _accountsOnLs.Keys));
            else if (account.SessionKey == null)
                _log.LogWarning("GS auth DENIED: account {Id} has no session key", key.AccountId);
            else
                _log.LogWarning(
                    "GS auth DENIED: session key mismatch for account {Id} — expected loginOk={ELoginOk} playOk1={EPlayOk1} playOk2={EPlayOk2}, got loginOk={GLoginOk} playOk1={GPlayOk1} playOk2={GPlayOk2}",
                    key.AccountId, account.SessionKey.LoginOk, account.SessionKey.PlayOk1, account.SessionKey.PlayOk2,
                    key.LoginOk, key.PlayOk1, key.PlayOk2);
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

    // --- Reconnect flow (Java: reconnectingAccounts) ---

    private readonly ConcurrentDictionary<int, ReconnectingAccount> _reconnectingAccounts = new();

    public void AddReconnectingAccount(ReconnectingAccount account)
    {
        _reconnectingAccounts[account.Account.Id] = account;
    }

    public async Task AuthReconnectingAccountAsync(int accountId, int loginOk, int reconnectKey, LoginConnection conn, CancellationToken ct = default)
    {
        if (!_reconnectingAccounts.TryRemove(accountId, out var reconnecting)
            || reconnecting.ReconnectionKey != reconnectKey)
        {
            _log.LogWarning("Reconnect auth failed for account {Id} (key mismatch or not pending) — closing", accountId);
            await conn.DisposeAsync();
            return;
        }

        var account = reconnecting.Account;
        conn.Account = account;
        var sk = new SessionKey(account);
        account.SessionKey = sk;
        conn.SessionKey = sk;
        conn.State = LoginConnection.LoginState.AUTHED_LOGIN;
        _accountsOnLs[account.Id] = account;

        _log.LogInformation("Account {Name} fast-reconnected to login server", account.Name);
        await conn.SendAsync(new SM_UPDATE_SESSION(sk.AccountId, sk.LoginOk), ct);
    }

    // --- Character-count roundtrip (Java: accountsGSCharacterCounts) ---

    private readonly object _charCountLock = new();
    private readonly Dictionary<int, Dictionary<int, int>> _gsCharacterCounts = new();
    private readonly Dictionary<int, LoginConnection> _pendingServerList = new();
    private static readonly TimeSpan ServerListTimeout = TimeSpan.FromSeconds(3);

    public async Task RequestServerListAsync(LoginConnection conn, CancellationToken ct = default)
    {
        int accountId = conn.Account!.Id;
        var targets = new List<GsConnection>();

        lock (_charCountLock)
        {
            var counts = new Dictionary<int, int>();
            _gsCharacterCounts[accountId] = counts;
            _pendingServerList[accountId] = conn;

            foreach (var gsi in GameServerTable.GetGameServers())
            {
                if (gsi.GscHandler != null)
                    targets.Add(gsi.GscHandler);
                else
                    counts[gsi.Id] = 0;
            }
        }

        if (targets.Count == 0)
        {
            await SendServerListAsync(accountId, ct);
            return;
        }

        foreach (var gsc in targets)
        {
            try
            {
                await gsc.SendAsync(new SM_GS_CHARACTER_RESPONSE(accountId), ct);
            }
            catch (Exception ex)
            {
                _log.LogWarning(ex, "Failed to request character count from GS for account {Id}", accountId);
            }
        }

        // Safety net Java doesn't have: a GS that never answers would leave the
        // client staring at an empty dialog. Send whatever we have after the timeout.
        _ = Task.Delay(ServerListTimeout).ContinueWith(async _ =>
        {
            bool stillPending;
            lock (_charCountLock)
                stillPending = _pendingServerList.ContainsKey(accountId);
            if (stillPending)
            {
                _log.LogWarning("Character-count replies incomplete for account {Id} after {Timeout}s — sending server list anyway",
                    accountId, ServerListTimeout.TotalSeconds);
                await SendServerListAsync(accountId, CancellationToken.None);
            }
        }, TaskScheduler.Default);
    }

    public async Task AddGsCharacterCountAsync(int accountId, int gsId, int characterCount, CancellationToken ct = default)
    {
        bool complete;
        lock (_charCountLock)
        {
            if (!_gsCharacterCounts.TryGetValue(accountId, out var counts))
                return; // not pending (already sent or timed out)
            counts[gsId] = characterCount;
            complete = counts.Count >= GameServerTable.GetGameServers().Count;
        }

        if (complete)
            await SendServerListAsync(accountId, ct);
    }

    private async Task SendServerListAsync(int accountId, CancellationToken ct)
    {
        LoginConnection? conn;
        Dictionary<int, int>? counts;
        lock (_charCountLock)
        {
            if (!_pendingServerList.Remove(accountId, out conn))
                return; // already sent
            _gsCharacterCounts.Remove(accountId, out counts);
        }

        var servers = GameServerTable.GetGameServers().ToList();
        sbyte lastServer = conn!.Account?.LastServer ?? -1;
        try
        {
            await conn.SendAsync(new SM_SERVER_LIST(servers, lastServer, conn.IP, counts), ct);
        }
        catch (Exception ex)
        {
            _log.LogDebug(ex, "Failed to send SM_SERVER_LIST to account {Id} (connection gone?)", accountId);
        }
    }

    private static string HashPassword(string password)
    {
        var hash = SHA1.HashData(Encoding.UTF8.GetBytes(password));
        return Convert.ToBase64String(hash);
    }
}
