using AionLightning.Game.Model.GameObjects;
using AionLightning.Game.Model.Templates.Housing;

namespace AionLightning.Game.Model.House;

/// <summary>
/// Java model.house.HouseRegistry. P2 covered only the default building-part decorations used to render a
/// house's appearance (SM_HOUSE_RENDER/SM_HOUSE_UPDATE's "roof/wall/floor" section). P3 adds player-placed
/// furniture (<see cref="HouseObject"/>) and custom decoration parts on top of that: <see cref="_objects"/>
/// / <see cref="_customParts"/> plus which (type, floor) slot each custom part currently occupies
/// (<see cref="_inUseCustomParts"/>) — <see cref="GetRenderPart"/> now checks that map before falling back
/// to the building's default part.
///
/// This class stays DB-free like the rest of P2's model layer: DAO calls happen in the CM-handler/service
/// layer (see CM_HOUSE_EDIT/CM_HOUSE_DECORATE/CM_RELEASE_OBJECT and HousingService.LoadRegisteredItemsAsync),
/// which populate/persist rows and hand this class already-resolved <see cref="HouseObject"/>/
/// <see cref="HouseDecoration"/> instances. Java's registry-level aggregate UPDATE_REQUIRED dirty flag
/// (used to skip a periodic no-op save) isn't ported — this port has no periodic housing save task; each
/// CM handler persists exactly the row(s) it touched immediately instead (see PersistentState's doc).
/// </summary>
public sealed class HouseRegistry
{
    private const int Floors = 6;

    private readonly Dictionary<(PartType Type, int Floor), HouseDecoration> _defaultParts = new();
    private readonly Dictionary<int, HouseObject> _objects = new();
    private readonly Dictionary<int, HouseDecoration> _customParts = new();
    private readonly Dictionary<(PartType Type, int Floor), HouseDecoration> _inUseCustomParts = new();

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

    /// <summary>Java House.getRenderPart(PartType, floor) — the active custom decoration for the slot if
    /// one has been chosen via <see cref="SetPartInUse"/>, else the building's default part.</summary>
    public HouseDecoration? GetRenderPart(PartType type, int floor) =>
        _inUseCustomParts.TryGetValue((type, floor), out var custom) ? custom : _defaultParts.GetValueOrDefault((type, floor));

    /// <summary>Java HouseRegistry.getDefaultPartByType(PartType, int).</summary>
    public HouseDecoration? GetDefaultPartByType(PartType type, int floor) => _defaultParts.GetValueOrDefault((type, floor));

    /// <summary>Java HouseRegistry.getDefaultParts() — every populated default-part slot.</summary>
    public IReadOnlyCollection<HouseDecoration> DefaultParts => _defaultParts.Values;

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

    // ---- Placed furniture (Java HouseRegistry.objects) ----------------------------------------------

    public IReadOnlyCollection<HouseObject> Objects => _objects.Values;

    /// <summary>Java HouseRegistry.getSpawnedObjects() — objects the player has positioned in the house.</summary>
    public IEnumerable<HouseObject> SpawnedObjects =>
        _objects.Values.Where(o => o.IsSpawnedByPlayer && o.PersistentState != PersistentState.Deleted);

    /// <summary>Java HouseRegistry.getNotSpawnedObjects() — registered but not yet placed anywhere.</summary>
    public IEnumerable<HouseObject> NotSpawnedObjects =>
        _objects.Values.Where(o => !o.IsSpawnedByPlayer && o.PersistentState != PersistentState.Deleted);

    public HouseObject? GetObjectByObjId(int objectId) => _objects.GetValueOrDefault(objectId);

    /// <summary>Java HouseRegistry.putObject(HouseObject).</summary>
    public bool PutObject(HouseObject obj)
    {
        if (!_objects.TryAdd(obj.ObjectId, obj)) return false;
        return true;
    }

    /// <summary>
    /// Java HouseRegistry.removeObject(int) — a never-persisted (New) object is discarded from memory
    /// immediately (nothing to delete from the DB); an already-persisted one is marked
    /// <see cref="PersistentState.Deleted"/> so the caller can issue the matching DB delete and then call
    /// <see cref="DiscardObject"/>.
    /// </summary>
    public HouseObject? RemoveObject(int objectId)
    {
        if (!_objects.TryGetValue(objectId, out var obj)) return null;
        if (obj.PersistentState == PersistentState.New)
            _objects.Remove(objectId);
        else
            obj.PersistentState = PersistentState.Deleted;
        return obj;
    }

    /// <summary>Java HouseRegistry.discardObject(Integer) — unconditional in-memory removal, called by the
    /// CM handler after a successful DB delete for an object <see cref="RemoveObject"/> marked Deleted.</summary>
    public void DiscardObject(int objectId) => _objects.Remove(objectId);

    // ---- Custom decoration parts (Java HouseRegistry.customParts) -----------------------------------

    /// <summary>Java HouseRegistry.getCustomParts() — "in packets used custom parts are not included
    /// (kinda semi-deleted)": only not-currently-displayed, not-deleted custom parts are returned (the
    /// player's un-placed decoration pool for SM_HOUSE_REGISTRY action=2).</summary>
    public IReadOnlyCollection<HouseDecoration> CustomParts =>
        _customParts.Values.Where(d => d.PersistentState != PersistentState.Deleted && !d.IsUsed).ToList();

    public HouseDecoration? GetCustomPartByObjId(int objectId) => _customParts.GetValueOrDefault(objectId);

    /// <summary>Java HouseRegistry.putCustomPart(HouseDecoration).</summary>
    public bool PutCustomPart(HouseDecoration decor) => _customParts.TryAdd(decor.ObjectId, decor);

    /// <summary>Java HouseRegistry.removeCustomPart(int) — same New-vs-persisted split as <see cref="RemoveObject"/>.</summary>
    public HouseDecoration? RemoveCustomPart(int objectId)
    {
        if (!_customParts.TryGetValue(objectId, out var decor)) return null;
        if (decor.PersistentState == PersistentState.New)
            _customParts.Remove(objectId);
        else
            decor.PersistentState = PersistentState.Deleted;
        return decor;
    }

    /// <summary>Java HouseRegistry.discardPart(HouseDecoration).</summary>
    public void DiscardPart(int objectId) => _customParts.Remove(objectId);

    /// <summary>
    /// Java HouseRegistry.setPartInUse(HouseDecoration, int) — activates <paramref name="decorationUse"/>
    /// (fetched via <see cref="GetDefaultPartByType"/> to revert to default, or via
    /// <see cref="GetCustomPartByObjId"/> to pick a custom part) for the given (type, floor) slot,
    /// deactivating whatever was previously active. Reproduces a Java quirk verbatim: reverting a slot to
    /// its default deactivates <em>every</em> in-use custom part of that <see cref="PartType"/> regardless
    /// of floor (no floor filter in that branch), while picking a specific custom part only displaces
    /// other custom parts on the <em>same</em> floor.
    /// </summary>
    /// <returns>Displaced custom parts that had already been persisted (now <see cref="PersistentState.Deleted"/>)
    /// and need a DB delete; parts that were still New are discarded from memory here directly.</returns>
    public IReadOnlyList<HouseDecoration> SetPartInUse(HouseDecoration decorationUse, int floor)
    {
        var deleted = new List<HouseDecoration>();
        bool isDefault = decorationUse.ObjectId == 0;

        if (isDefault)
        {
            decorationUse.IsUsed = true;
            _inUseCustomParts.Remove((decorationUse.Type, floor));
            DeactivateCustomParts(decorationUse.Type, floor: null, exclude: null, deleted);
            return deleted;
        }

        _inUseCustomParts[(decorationUse.Type, floor)] = decorationUse;
        decorationUse.IsUsed = true;
        decorationUse.Floor = floor;
        decorationUse.MarkDirty();

        var defaultDecor = GetDefaultPartByType(decorationUse.Type, floor);
        if (defaultDecor is not null) defaultDecor.IsUsed = false;

        DeactivateCustomParts(decorationUse.Type, floor, decorationUse, deleted);
        return deleted;
    }

    private void DeactivateCustomParts(PartType type, int? floor, HouseDecoration? exclude, List<HouseDecoration> deleted)
    {
        foreach (var decor in _customParts.Values.ToList())
        {
            if (decor.Type != type || decor.PersistentState == PersistentState.Deleted) continue;
            if (ReferenceEquals(decor, exclude) || !decor.IsUsed) continue;
            if (floor is not null && decor.Floor != floor) continue;

            decor.IsUsed = false;
            decor.Floor = -1;
            if (decor.PersistentState == PersistentState.New)
                _customParts.Remove(decor.ObjectId);
            else
            {
                decor.PersistentState = PersistentState.Deleted;
                deleted.Add(decor);
            }
        }
    }
}
