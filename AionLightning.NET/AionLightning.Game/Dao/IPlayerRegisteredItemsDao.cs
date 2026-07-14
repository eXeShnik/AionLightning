namespace AionLightning.Game.Dao;

/// <summary>One row of <c>player_registered_items</c> — either a placed/not-yet-placed
/// <see cref="Model.GameObjects.HouseObject"/> (<see cref="Area"/> holds its <c>PlaceArea</c> string, or
/// "NONE" while unplaced) or a custom building-part <see cref="Model.GameObjects.HouseDecoration"/>
/// (<see cref="Area"/> is always the "DECOR" sentinel). Mirrors Java's single shared table for both kinds
/// (mysql5.MySQL5PlayerRegisteredItemsDAO) rather than splitting into two tables.</summary>
public sealed record PlayerRegisteredItemRow(
    int ItemUniqueId,
    int ItemId,
    int? ExpireTime,
    int? Color,
    int ColorExpires,
    int OwnerUseCount,
    int VisitorUseCount,
    float X,
    float Y,
    float Z,
    short H,
    string Area,
    // int, not byte: Java stores -1 here as the "not currently placed on any floor" sentinel for a
    // just-created custom decoration part (HouseDecoration(objectId, templateId) -> floor -1) — an
    // unsigned byte can't represent that.
    int Floor);

/// <summary>Port of Java <c>dao.PlayerRegisteredItemsDAO</c> — persists a house registry's placed objects
/// and custom decoration parts for one player. Java's DAO also does the differential add/update/delete
/// batching (see MySQL5PlayerRegisteredItemsDAO.store) driven off HouseRegistry's aggregate dirty flag;
/// this port's callers (CM_HOUSE_EDIT/CM_HOUSE_DECORATE/CM_RELEASE_OBJECT) persist each row they touch
/// immediately instead — see <see cref="Model.House.HouseRegistry"/>'s class doc.</summary>
public interface IPlayerRegisteredItemsDao
{
    Task<List<PlayerRegisteredItemRow>> LoadByPlayerAsync(int playerId, CancellationToken ct = default);

    Task UpsertAsync(int playerId, PlayerRegisteredItemRow row, CancellationToken ct = default);

    Task DeleteAsync(int itemUniqueId, CancellationToken ct = default);

    Task DeleteAllForPlayerAsync(int playerId, CancellationToken ct = default);
}
