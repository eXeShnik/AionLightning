namespace AionLightning.Game.Model.Templates.Housing;

/// <summary>&lt;parts&gt; child element of a &lt;building&gt; in house_buildings.xml — decoration part ids
/// used to render the default appearance of a building (Java model.templates.housing.Parts).
/// Door, InWall and InFloor are required by the schema; the rest are optional.</summary>
public sealed record HouseParts(
    int? Roof,
    int? OutWall,
    int? Frame,
    int Door,
    int? Garden,
    int? Fence,
    int InWall,
    int InFloor);
