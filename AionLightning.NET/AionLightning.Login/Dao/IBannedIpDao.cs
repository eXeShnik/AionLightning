using AionLightning.Login.Model;

namespace AionLightning.Login.Dao;

public interface IBannedIpDao
{
    Task<ISet<BannedIP>> GetAllAsync(CancellationToken ct = default);
    Task<bool> InsertAsync(BannedIP ban, CancellationToken ct = default);
    Task CleanExpiredAsync(CancellationToken ct = default);
}
