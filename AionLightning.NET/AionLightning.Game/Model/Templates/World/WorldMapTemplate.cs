namespace AionLightning.Game.Model.Templates.World;

/// <summary>
/// One <c>&lt;map&gt;</c> row from <c>world_maps.xml</c> (Java <c>WorldMapTemplate</c>). Carries the
/// attributes the port needs to reason about a world: whether it is an instanced map, its drop
/// table, and world flags. Twin/max-user counts from the Java template are not present in the
/// 4.6.0 dataset this port uses, so they are intentionally absent here.
/// </summary>
public sealed record WorldMapTemplate(
    int WorldId,
    string Name,
    bool IsInstance,
    string WorldType,
    int WorldSize,
    string Flags,
    string DropType,
    int DeathLevel,
    int WaterLevel);
