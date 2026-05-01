namespace AionLightning.Game.Dao;

public interface IMotionDao
{
    Task<IReadOnlyDictionary<byte, short>> LoadByPlayerIdAsync(int playerId, CancellationToken ct = default);
    Task UpsertAsync(int playerId, byte slot, short motionId, CancellationToken ct = default);
    Task DeleteAsync(int playerId, byte slot, CancellationToken ct = default);
}
