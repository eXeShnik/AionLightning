using AionLightning.Login.Model;

namespace AionLightning.Login.Dao;

public interface IAccountDao
{
    Task<Account?> FindByNameAsync(string name, CancellationToken ct = default);
    Task<int> InsertAsync(Account account, CancellationToken ct = default);
    Task<bool> UpdateLastIpAsync(int accountId, string ip, CancellationToken ct = default);
    Task<bool> UpdateLastServerAsync(int accountId, sbyte serverId, CancellationToken ct = default);
}
