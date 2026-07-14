namespace AionLightning.Game.Model.Instance;

/// <summary>
/// Base scoreboard for one instance run (Java <c>model.instance.instancereward.InstanceReward</c>):
/// tracks per-player reward rows and the overall scoreboard phase. Map-specific rewards (e.g.
/// <see cref="DredgionReward"/>) extend this with their own scoring rules.
/// </summary>
public class InstanceReward<T> where T : InstancePlayerReward
{
    private readonly List<T> _playerRewards = new();

    public int MapId { get; }
    public int InstanceId { get; }
    public InstanceScoreType ScoreType { get; set; } = InstanceScoreType.START_PROGRESS;

    public InstanceReward(int mapId, int instanceId)
    {
        MapId = mapId;
        InstanceId = instanceId;
    }

    public IReadOnlyList<T> PlayerRewards => _playerRewards;

    public bool ContainsPlayer(int objectId) => _playerRewards.Any(r => r.OwnerObjectId == objectId);

    public T? GetPlayerReward(int objectId) => _playerRewards.FirstOrDefault(r => r.OwnerObjectId == objectId);

    public void AddPlayerReward(T reward) => _playerRewards.Add(reward);

    public void RemovePlayerReward(T reward) => _playerRewards.Remove(reward);

    public bool IsRewarded => ScoreType.IsEndProgress();
    public bool IsPreparing => ScoreType.IsPreparing();
    public bool IsStartProgress => ScoreType.IsStartProgress();

    public virtual void Clear() => _playerRewards.Clear();
}
