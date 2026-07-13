using System.Collections.Concurrent;
using AionLightning.Game.Instance;

namespace AionLightning.Game.World;

/// <summary>
/// One live channel of an instanced map (Java <c>WorldMapInstance</c>). This is lightweight
/// metadata only — the actual player/NPC objects stay in the flat <see cref="World"/> and are
/// enumerated by <c>(WorldId, InstanceId)</c> scope. This record tracks ownership, registration,
/// the attached script handler, and the empty-channel destroy deadline.
/// </summary>
public sealed class WorldMapInstance
{
    public WorldMapInstance(int worldId, int instanceId, int ownerId)
    {
        WorldId    = worldId;
        InstanceId = instanceId;
        OwnerId    = ownerId;
    }

    public int WorldId { get; }
    public int InstanceId { get; }

    /// <summary>Owning player objectId for a personal instance, else 0.</summary>
    public int OwnerId { get; }
    public bool IsPersonal => OwnerId != 0;

    /// <summary>Object ids allowed to (re)enter this channel (players, or a whole team's members).</summary>
    public ConcurrentDictionary<int, byte> RegisteredObjects { get; } = new();

    /// <summary>Team id when the channel is registered to a group/alliance/league, else null.</summary>
    public int? RegisteredGroupId { get; set; }
    public int? RegisteredAllianceId { get; set; }
    public int? RegisteredLeagueId { get; set; }
    public bool IsTeamRegistered => RegisteredGroupId is not null || RegisteredAllianceId is not null || RegisteredLeagueId is not null;

    public int? SoloPlayerObjId { get; set; }

    /// <summary>The bound script handler (never null once created — a no-op default otherwise).</summary>
    public IInstanceHandler Handler { get; set; } = null!;

    /// <summary>
    /// Unix-ms deadline at which an empty solo channel is destroyed; <c>null</c> while occupied.
    /// The empty-instance checker sets/clears this.
    /// </summary>
    public long? EmptyDestroyDueUtcMs { get; set; }

    public void Register(int objectId) => RegisteredObjects[objectId] = 0;
    public bool IsRegistered(int objectId) => RegisteredObjects.ContainsKey(objectId);
}
