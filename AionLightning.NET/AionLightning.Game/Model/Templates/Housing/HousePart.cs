namespace AionLightning.Game.Model.Templates.Housing;

/// <summary>One &lt;house_part&gt; element from house_parts.xml (Java model.templates.housing.HousePart) —
/// a placeable decoration template: which <see cref="Templates.Housing.PartType"/> slot it fills, an
/// item-quality tier, and the building tags (<see cref="Building.PartsMatchTag"/>) it applies to.</summary>
public sealed record HousePart(int Id, string? Name, PartType Type, string? Quality, IReadOnlyList<string> Tags);
