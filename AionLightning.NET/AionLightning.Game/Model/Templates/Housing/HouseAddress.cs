namespace AionLightning.Game.Model.Templates.Housing;

/// <summary>One &lt;address&gt; element from houses.xml (Java model.templates.housing.HouseAddress) —
/// a single buildable plot within a <see cref="HousingLand"/>. Exit coordinates are only present for
/// addresses that teleport the player out of an instanced house map (e.g. estates/palaces).</summary>
public sealed record HouseAddress(
    int Id,
    int MapId,
    int TownId,
    float X,
    float Y,
    float Z,
    int? ExitMapId,
    float? ExitX,
    float? ExitY,
    float? ExitZ);
