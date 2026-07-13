namespace AionLightning.Game.Model.Templates.Housing;

/// <summary>Java model.templates.housing.Building. Merges the per-land reference (id + default flag,
/// from houses.xml &lt;land&gt;&lt;buildings&gt;) with the canonical building definition (type, size,
/// parts, from house_buildings.xml) — <see cref="HousingData"/> resolves the lookup at load time instead
/// of the lazy DataManager fallback the Java model uses.</summary>
public sealed record Building(
    int Id,
    bool IsDefault,
    BuildingType? Type,
    HouseType? Size,
    string? PartsMatchTag,
    HouseParts? Parts);
