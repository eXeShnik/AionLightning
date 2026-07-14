using AionLightning.Game.Model.GameObjects;
using AionLightning.Game.Model.Templates.Housing;

namespace AionLightning.Game.Model.House;

/// <summary>
/// Java model.house.HouseRegistry, restricted to this phase's scope: the default building-part
/// decorations used to render a house's appearance (SM_HOUSE_RENDER/SM_HOUSE_UPDATE's "roof/wall/floor"
/// section). Java's custom/player-placed furniture tracking (objects map, customParts map, save/despawn,
/// PersistentState bookkeeping) is not ported — no HouseObject/decoration-placement system exists yet, so
/// <see cref="GetRenderPart"/> always resolves to the building's default part (Java's own fallback when
/// no custom part is in use).
/// </summary>
public sealed class HouseRegistry
{
    private const int Floors = 6;

    private readonly Dictionary<(PartType Type, int Floor), HouseDecoration> _defaultParts = new();

    public House Owner { get; }

    public HouseRegistry(House owner) => Owner = owner;

    /// <summary>Java House.putDefaultParts() — populates the default decoration for every part slot from
    /// the building's literal part ids (this port's house_buildings.xml stores one concrete part id per
    /// slot already, unlike Java's tag-matched HousePartsData lookup — see HousingData/HousePartsData).
    /// Java replicates the same part id across every floor of a multi-floor slot (INWALL_ANY/INFLOOR_ANY);
    /// reproduced here identically since our data only carries one id per slot too.</summary>
    public void LoadDefaultParts(Building building)
    {
        _defaultParts.Clear();
        var parts = building.Parts;
        if (parts is null) return; // note: building has no <parts> element — nothing to render.

        AddSingleFloor(PartType.ROOF, parts.Roof);
        AddSingleFloor(PartType.OUTWALL, parts.OutWall);
        AddSingleFloor(PartType.FRAME, parts.Frame);
        AddSingleFloor(PartType.DOOR, parts.Door);
        AddSingleFloor(PartType.GARDEN, parts.Garden);
        AddSingleFloor(PartType.FENCE, parts.Fence);
        AddAllFloors(PartType.INWALL_ANY, parts.InWall);
        AddAllFloors(PartType.INFLOOR_ANY, parts.InFloor);
    }

    /// <summary>Java House.getRenderPart(PartType, floor) — always the default part in this phase (no
    /// custom/player-placed parts are tracked yet).</summary>
    public HouseDecoration? GetRenderPart(PartType type, int floor) => _defaultParts.GetValueOrDefault((type, floor));

    private void AddSingleFloor(PartType type, int? partId)
    {
        if (partId is null or 0) return;
        _defaultParts[(type, 0)] = new HouseDecoration(partId.Value, type, 0);
    }

    private void AddAllFloors(PartType type, int partId)
    {
        if (partId == 0) return;
        for (int floor = 0; floor < Floors; floor++)
            _defaultParts[(type, floor)] = new HouseDecoration(partId, type, floor);
    }
}
