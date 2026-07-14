using AionLightning.Game.Model.Templates.Housing;

namespace AionLightning.Game.Model.GameObjects;

/// <summary>
/// Java model.gameobjects.HouseDecoration, restricted to this phase's scope: a single default
/// building-part slot (roof/outer-wall/frame/door/garden/fence/inner-wall/inner-floor) rendered by
/// SM_HOUSE_RENDER/SM_HOUSE_UPDATE. Player-placed custom furniture (Java's HouseObject-backed
/// decorations, PersistentState tracking, the isUsed/floor-reassignment toggle) is not ported — see
/// <see cref="AionLightning.Game.Model.House.HouseRegistry"/>.
/// </summary>
public sealed class HouseDecoration(int partId, PartType type, int floor)
{
    public int PartId { get; } = partId;
    public PartType Type { get; } = type;
    public int Floor { get; } = floor;
}
