using AionLightning.Game.Model;

namespace AionLightning.Game.Dao;

/// <summary>Port of Java <c>dao.VeteranRewardsDAO</c>.</summary>
public interface IVeteranRewardDao
{
    Task<List<VeteranReward>> LoadPendingAsync(CancellationToken ct = default);
    Task DeleteAsync(int id, CancellationToken ct = default);
}
