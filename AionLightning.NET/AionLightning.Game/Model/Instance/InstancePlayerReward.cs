namespace AionLightning.Game.Model.Instance;

/// <summary>
/// Per-player scoring state inside an instance run (Java
/// <c>model.instance.playerreward.InstancePlayerReward</c>). Base for map-specific reward types
/// (e.g. <see cref="DredgionPlayerReward"/>).
/// </summary>
public class InstancePlayerReward
{
    public int OwnerObjectId { get; }
    public int Points { get; private set; }
    public int PvPKills { get; private set; }
    public int MonsterKills { get; private set; }

    public InstancePlayerReward(int ownerObjectId) => OwnerObjectId = ownerObjectId;

    public void AddPoints(int points)
    {
        Points += points;
        if (Points < 0) Points = 0;
    }

    public void AddPvPKill() => PvPKills++;
    public void AddMonsterKill() => MonsterKills++;
}
