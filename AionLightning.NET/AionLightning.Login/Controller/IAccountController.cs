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
}
