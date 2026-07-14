using AionLightning.Game.Dao;
using AionLightning.Game.Model.GameObjects;

namespace AionLightning.Game.Services;

/// <summary>
/// Converts a placed <see cref="HouseObject"/> or custom <see cref="HouseDecoration"/> to the
/// <c>player_registered_items</c> row shape the CM_HOUSE_EDIT/CM_HOUSE_DECORATE/CM_RELEASE_OBJECT handlers
/// upsert after each mutating action (see <see cref="Model.House.HouseRegistry"/>'s class doc for why
/// persistence happens per-action here instead of via Java's periodic differential batch).
/// </summary>
public static class RegisteredItemRowMapper
{
    /// <summary>Java MySQL5PlayerRegisteredItemsDAO.storeObjects — area is the object's PlaceArea while
    /// placed, else the "NONE" sentinel (Java: <c>x&gt;0||y&gt;0||z&gt;0 ? area.toString() : "NONE"</c>).</summary>
    public static PlayerRegisteredItemRow ForObject(HouseObject obj) => new(
        obj.ObjectId,
        obj.TemplateId,
        obj.ExpireEnd > 0 ? obj.ExpireEnd : null,
        obj.Color,
        obj.ColorExpireEnd,
        obj.OwnerUsedCount,
        obj.VisitorUsedCount,
        obj.X,
        obj.Y,
        obj.Z,
        obj.Heading,
        obj.IsSpawnedByPlayer ? obj.Template?.Area ?? "NONE" : "NONE",
        0); // Java's storeObjects always writes floor=0 for HouseObject rows — only decoration rows use it.

    /// <summary>Java MySQL5PlayerRegisteredItemsDAO.storeParts — area is always the "DECOR" sentinel;
    /// owner_use_count doubles as the isUsed bit (Java: <c>part.isUsed() ? 1 : 0</c>).</summary>
    public static PlayerRegisteredItemRow ForDecoration(HouseDecoration decor) => new(
        decor.ObjectId, decor.PartId, null, null, 0, decor.IsUsed ? 1 : 0, 0, 0, 0, 0, 0, "DECOR", decor.Floor);
}
