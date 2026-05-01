using AionLightning.Game.Model.Broker;

namespace AionLightning.Game.Dao;

public interface IBrokerDao
{
    Task<IReadOnlyList<BrokerItem>> LoadAllAsync(CancellationToken ct = default);
    Task<int> InsertAsync(BrokerItem item, CancellationToken ct = default);
    Task MarkSoldAsync(int id, CancellationToken ct = default);
    Task MarkCanceledAsync(int id, CancellationToken ct = default);
    Task MarkSettledAsync(int id, CancellationToken ct = default);
    Task DeleteAsync(int id, CancellationToken ct = default);
}
