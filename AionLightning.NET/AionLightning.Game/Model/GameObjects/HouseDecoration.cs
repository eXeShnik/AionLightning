using AionLightning.Game.Model.Templates.Housing;

namespace AionLightning.Game.Model.GameObjects;

/// <summary>
/// Java model.gameobjects.HouseDecoration — either a default building-part slot
/// (roof/outer-wall/frame/door/garden/fence/inner-wall/inner-floor, <paramref name="objectId"/> 0, from
/// <see cref="AionLightning.Game.Model.House.HouseRegistry.LoadDefaultParts"/>) or a player-placed custom
/// decoration swapped in via CM_HOUSE_DECORATE (non-zero <paramref name="objectId"/>, persisted through
/// <c>player_registered_items</c>). <see cref="Floor"/>/<see cref="IsUsed"/> mirror Java's mutable
/// setFloor/setUsed — this port has no per-field auto-dirty-marking on the setters; callers mutate then
/// call <see cref="MarkDirty"/> explicitly (matching <see cref="HouseObject"/>'s convention).
/// </summary>
public sealed class HouseDecoration
{
    public int ObjectId { get; }
    public int PartId { get; }
    public PartType Type { get; }
    public int Floor { get; set; }
    public bool IsUsed { get; set; }
    public PersistentState PersistentState { get; set; }

    public HouseDecoration(int partId, PartType type, int floor, int objectId = 0)
    {
        PartId = partId;
        Type = type;
        Floor = floor;
        ObjectId = objectId;
        // Default parts (objectId 0) are never persisted — Java's putDefaultPart() forces NOACTION
        // immediately after construction; everything else starts NEW until its first DB insert.
        PersistentState = objectId == 0 ? PersistentState.NoAction : PersistentState.New;
    }

    /// <summary>See <see cref="HouseObject.MarkDirty"/> — same New/Deleted/NoAction-are-terminal rule.</summary>
    public void MarkDirty()
    {
        if (PersistentState is PersistentState.New or PersistentState.Deleted or PersistentState.NoAction) return;
        PersistentState = PersistentState.UpdateRequired;
    }
}
