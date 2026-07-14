namespace AionLightning.Game.Model.Templates.Housing;

/// <summary>Java's ten &lt;housing_objects&gt; XML element names (dataholders.HousingObjectData), each
/// backed by its own PlaceableHouseObject subclass in Java. This port keeps one flat
/// <see cref="HousingObjectTemplate"/> record instead (see <see cref="Model.GameObjects.HouseObject"/>'s
/// class doc for why).</summary>
public enum HousingObjectKind
{
    Passive,
    Chair,
    JukeBox,
    MovieJukeBox,
    MoveableItem,
    Npc,
    Picture,
    Postbox,
    Storage,
    UseableItem,
    Emblem,
}

/// <summary>Java model.templates.housing.UseItemAction — the optional &lt;action&gt; child of a
/// &lt;use_item&gt; element (housing_objects.xml). Reward/consume wiring for cook-pots etc. is not
/// implemented in this phase (see CM_USE_HOUSE_OBJECT's note) — only <see cref="CheckType"/> is currently
/// read, for the SM_HOUSE_OBJECT/SM_HOUSE_EDIT usage-data tail.</summary>
public sealed record HousingObjectUseAction(int? FinalRewardId, int? RewardId, int? RemoveCount, int? CheckType);

/// <summary>Java dataholders.HousingObjectData's per-id template — one &lt;passive&gt;/&lt;chair&gt;/
/// &lt;use_item&gt;/etc. element from housing_objects.xml. Fields not common to every kind
/// (<see cref="NpcId"/>, <see cref="WarehouseId"/>, the use-item-only fields, <see cref="Level"/>) default
/// to 0/false/null on kinds that don't carry them.</summary>
public sealed record HousingObjectTemplate(
    int TemplateId,
    HousingObjectKind Kind,
    int NameId,
    string? Quality,
    string? Category,
    string? Area,
    string? Location,
    float TalkingDistance,
    int UseDays,
    string? Limit,
    int NpcId = 0,
    int WarehouseId = 0,
    bool OwnerOnly = false,
    int Cd = 0,
    int Delay = 0,
    int? UseCount = null,
    int? RequiredItem = null,
    int Level = 0,
    HousingObjectUseAction? Action = null)
{
    /// <summary>Java PlaceableHouseObject subclass's getTypeId() — the byte SM_HOUSE_OBJECT/SM_HOUSE_EDIT
    /// switch on for their per-kind wire tail.</summary>
    public byte TypeId => Kind switch
    {
        HousingObjectKind.UseableItem => 1,
        HousingObjectKind.Storage => 2,
        HousingObjectKind.Postbox => 3,
        HousingObjectKind.Chair => 5,
        HousingObjectKind.JukeBox or HousingObjectKind.MovieJukeBox => 6,
        HousingObjectKind.Npc => 7,
        HousingObjectKind.Emblem => 11,
        _ => 0, // Passive, MoveableItem, Picture
    };
}
