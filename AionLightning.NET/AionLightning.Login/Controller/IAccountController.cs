using AionLightning.Login.Model;
using AionLightning.Login.Network.Aion;
using AionLightning.Login.Network.GameServer;

namespace AionLightning.Login.Controller;

public interface IAccountController
{
    Task<(AionAuthResponse Response, Account? Account)> LoginAsync(
        string login, string password, string ip, CancellationToken ct = default);

    void RegisterAccount(Account account);

    void UnregisterAccount(int accountId);

    Task CheckAuthAsync(SessionKey key, GsConnection gsConnection, CancellationToken ct = default);

    /// <summary>
    /// Java AccountController.loadGSCharactersCount: queries every online GS for the
    /// account's character count, then sends SM_SERVER_LIST once all replies arrive
    /// (or immediately when no GS is connected; after a timeout as a safety net).
    /// </summary>
    Task RequestServerListAsync(LoginConnection conn, CancellationToken ct = default);

    /// <summary>Java AccountController.addGSCharacterCountFor + completion check.</summary>
    Task AddGsCharacterCountAsync(int accountId, int gsId, int characterCount, CancellationToken ct = default);

    /// <summary>Java AccountController.addReconnectingAccount — player left GS back to server select.</summary>
    void AddReconnectingAccount(ReconnectingAccount account);

    /// <summary>
    /// Java AccountController.authReconnectingAccount — validates the reconnect key from
    /// CM_UPDATE_SESSION, re-binds the account to this connection and sends SM_UPDATE_SESSION.
    /// Closes the connection on mismatch.
    /// </summary>
    Task AuthReconnectingAccountAsync(int accountId, int loginOk, int reconnectKey, LoginConnection conn, CancellationToken ct = default);
}
