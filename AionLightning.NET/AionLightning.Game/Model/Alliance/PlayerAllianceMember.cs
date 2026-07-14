namespace AionLightning.Game.Model.Alliance;

/// <summary>
/// A single alliance seat wrapping a live <see cref="Player"/> (Java <c>PlayerAllianceMember</c>).
/// Alliance membership is in-memory only (no persistence, same as <c>PlayerGroup</c>) — this wrapper
/// exists purely to carry the member's alliance-subgroup assignment alongside the player reference.
/// </summary>
public sealed class PlayerAllianceMember
{
    public Player Player { get; }
    public int ObjectId => Player.ObjectId;
    public string Name => Player.Name;

    /// <summary>
    /// The alliance subgroup id (1000-1003) this member currently sits in. Java confusingly calls this
    /// field "allianceId" on PlayerAllianceMember — it is actually the subgroup's team id, not the
    /// overall alliance's id.
    /// </summary>
    public int AllianceGroupId { get; set; }

    public PlayerAllianceMember(Player player) => Player = player;
}
