using HouseModel = AionLightning.Game.Model.House.House;
using AionLightning.Game.Model.Templates.Housing;

namespace AionLightning.Game.Model.GameObjects;

/// <summary>
/// Java model.gameobjects.HouseObject&lt;T&gt; — a single piece of furniture a player has placed inside
/// their house. Java splits this into an abstract base plus ten near-empty subclasses (ChairObject,
/// JukeBoxObject, MoveableObject, NpcObject, PassiveObject, PictureObject, PostboxObject, StorageObject,
/// UseableItemObject, EmblemObject) whose only real differences are <see cref="Templates.Housing.HousingObjectTemplate.TypeId"/>
/// and a handful of onUse() behaviors (mailbox dialog, warehouse open, cooldown-gated consumable rewards,
/// spawned house-NPC) that this phase does not port (no mailbox/warehouse/quest-reward wiring is in scope
/// here — see the CM_USE_HOUSE_OBJECT handler's own note). This port therefore uses one concrete class for
/// every kind, dispatching on <see cref="Template"/>'s <see cref="HousingObjectKind"/> only where the wire
/// format actually differs (SM_HOUSE_OBJECT/SM_HOUSE_EDIT's per-typeId tail).
/// </summary>
public sealed class HouseObject
{
    public int ObjectId { get; }
    public int TemplateId { get; }
    public HousingObjectTemplate? Template { get; }
    public HouseModel OwnerHouse { get; }

    public float X { get; set; }
    public float Y { get; set; }
    public float Z { get; set; }
    public byte Heading { get; set; }
    public int OwnerUsedCount { get; set; }
    public int VisitorUsedCount { get; set; }
    public int? Color { get; set; }
    public int ColorExpireEnd { get; set; }

    /// <summary>Java IExpirable.getExpireTime() — unix seconds when a use-days-limited object expires.
    /// 0 means "no expiration".</summary>
    public int ExpireEnd { get; set; }

    public PersistentState PersistentState { get; set; } = PersistentState.New;

    public HouseObject(HouseModel ownerHouse, int objectId, int templateId, HousingObjectTemplate? template)
    {
        OwnerHouse = ownerHouse;
        ObjectId = objectId;
        TemplateId = templateId;
        Template = template;
    }

    /// <summary>Java PlaceableHouseObject subclass's getTypeId() byte, resolved from the loaded template
    /// (0/unknown when the template failed to resolve).</summary>
    public byte TypeId => Template?.TypeId ?? 0;

    /// <summary>Java HouseObject.getRotation()/setRotation(int) — the wire rotation value (0-1080, 3 units
    /// per heading tick) derived from/mapped onto <see cref="Heading"/>.</summary>
    public int Rotation
    {
        get => (Heading & 0xFF) * 3;
        set => Heading = (byte)Math.Ceiling(value / 3.0);
    }

    /// <summary>Java HouseObject.isSpawnedByPlayer() — true once the object has a non-zero position
    /// (placed into the house), false while it only exists in the player's not-yet-placed registry pool.</summary>
    public bool IsSpawnedByPlayer => X != 0 || Y != 0 || Z != 0;

    /// <summary>Java IExpirable.getUseSecondsLeft() — -1 when unrestricted, else seconds remaining
    /// (never negative).</summary>
    public int UseSecondsLeft
    {
        get
        {
            if (ExpireEnd == 0) return -1;
            int diff = ExpireEnd - (int)DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            return diff < 0 ? 0 : diff;
        }
    }

    /// <summary>Java HouseObject.removeFromHouse() — pulls the object out of the placed world back into
    /// the player's not-yet-placed registry pool, without discarding it from the registry entirely.</summary>
    public void RemoveFromHouse()
    {
        X = 0;
        Y = 0;
        Z = 0;
        Heading = 0;
    }

    /// <summary>Java HouseObject.setPersistentState's default-case transition (the only branch this port's
    /// field setters need): a not-yet-persisted (New) or already-terminal (Deleted) object is left alone;
    /// anything else becomes dirty and needs a DB re-save. Deletion itself goes through
    /// <see cref="Model.House.HouseRegistry.RemoveObject"/> instead, which mirrors Java's separate
    /// New-vs-persisted branch in HouseRegistry.removeObject.</summary>
    public void MarkDirty()
    {
        if (PersistentState is PersistentState.New or PersistentState.Deleted) return;
        PersistentState = PersistentState.UpdateRequired;
    }
}
