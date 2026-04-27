using AionLightning.Login.Dao;
using AionLightning.Login.Model;
using AionLightning.Login.Network.Aion;
using AionLightning.Login.Network.GameServer;
using Microsoft.Extensions.Logging;

namespace AionLightning.Login.Controller;

/// <summary>
/// TODO M3: replace static DAO calls with injected IAccountRepository (Dapper/MySqlConnector).
/// Stubs compile for M1 — no actual DB access yet.
/// </summary>
public static class AccountController
{
    private static readonly ILogger _log =
        LoggerFactory.Create(b => b.AddConsole()).CreateLogger(nameof(AccountController));

    public static AionAuthResponse Login(string user, string password, LoginConnection client)
    {
        // TODO M3: query AccountDAO, validate password, check bans, set SessionKey
        _log.LogDebug("Login stub: user={User}", user);
        return AionAuthResponse.SYSTEM_ERROR;
    }

    public static void CheckAuth(SessionKey sessionKey, GsConnection gsConnection)
    {
        // TODO M3: validate session key, notify GS
        _log.LogDebug("CheckAuth stub: accountId={AccountId}", sessionKey.AccountId);
    }

    public static Account? GetAccount(int accountId, GameServerInfo? gsi)
    {
        return gsi?.GetAccount(accountId);
    }

    public static void AuthReconnectingAccount(int accountId, int loginOk, int reconnectKey, LoginConnection client)
    {
        // TODO M3: validate reconnect key
    }

    public static void Logout(int accountId, GsConnection gsConnection)
    {
        var account = gsConnection.GameServerInfo?.GetAccount(accountId);
        if (account != null)
        {
            gsConnection.GameServerInfo?.RemoveAccount(account);
            account.GameServerInfo = null;
        }
    }
}
