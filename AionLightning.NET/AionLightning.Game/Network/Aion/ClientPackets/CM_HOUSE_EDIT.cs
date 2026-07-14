using AionLightning.Commons.Network;
using AionLightning.Game.Configs.Options;
using AionLightning.Game.Dao;
using AionLightning.Game.DataHolders;
using AionLightning.Game.Model;
using AionLightning.Game.Model.GameObjects;
using AionLightning.Game.Model.House;
using AionLightning.Game.Network.Aion.ServerPackets;
using AionLightning.Game.Services;
using Microsoft.Extensions.Options;

namespace AionLightning.Game.Network.Aion.ClientPackets;

/// <summary>
/// Java clientpackets.CM_HOUSE_EDIT — the main furniture-placement packet: enter/exit decoration mode,
/// register an inventory item into the house registry (as a placed-object or a swappable decoration part),
/// delete/despawn/spawn/move a registered object. Only ever mutates the player's own active house (see
/// CM_HOUSE_DECORATE's doc). Opcode 0x110.
///
/// note: HousingAction.ENTER_RENOVATION/EXIT_RENOVATION are acknowledged with a bare SM_HOUSE_EDIT (Java
/// does the same — no other side effect for either). CHANGE_APPEARANCE (building-renovation-coupon swap)
/// is out of this phase's scope (no HousingService.SwitchHouseBuilding exists yet — that's a P4-sized
/// building-renovation feature, not furniture placement) and is a deliberate no-op here rather than acking
/// success the client would otherwise act on.
/// </summary>
public sealed class CM_HOUSE_EDIT : AionClientPacket
{
    private readonly GsClientConnection _conn;
    private readonly IDataManager _dataManager;
    private readonly IItemDao _itemDao;
    private readonly IPlayerRegisteredItemsDao _registeredItemsDao;
    private readonly IOptions<HousingOptions> _housingOptions;

    private byte _actionId;
    private HousingAction _action;
    private int _itemObjectId;
    private float _x, _y, _z;
    private int _rotation;
    private int _buildingId;

    public CM_HOUSE_EDIT(GsClientConnection conn, IDataManager dataManager, IItemDao itemDao,
        IPlayerRegisteredItemsDao registeredItemsDao, IOptions<HousingOptions> housingOptions)
    {
        _conn = conn;
        _dataManager = dataManager;
        _itemDao = itemDao;
        _registeredItemsDao = registeredItemsDao;
        _housingOptions = housingOptions;
    }

    public override void Read(ref PacketReader r)
    {
        _actionId = (byte)r.ReadC();
        _action = HousingActionExtensions.GetActionTypeById(_actionId);

        switch (_action)
        {
            case HousingAction.AddItem:
            case HousingAction.DeleteItem:
            case HousingAction.DespawnObject:
                _itemObjectId = r.ReadD();
                break;
            case HousingAction.SpawnObject:
            case HousingAction.MoveObject:
                _itemObjectId = r.ReadD();
                _x = r.ReadF();
                _y = r.ReadF();
                _z = r.ReadF();
                _rotation = r.ReadH();
                break;
            case HousingAction.ChangeAppearance:
                _buildingId = r.ReadD();
                break;
            default:
                break; // ENTER/EXIT_DECORATION, ENTER/EXIT_RENOVATION, unknown — nothing else on the wire.
        }
    }

    public override async ValueTask RunAsync(CancellationToken ct)
    {
        if (!_housingOptions.Value.Enable) return;

        var player = _conn.ActivePlayer;
        if (player is null) return;
        var house = player.ActiveHouse;
        if (house is null) return;

        switch (_action)
        {
            case HousingAction.EnterDecoration:
                await _conn.SendAsync(new SM_HOUSE_EDIT(_actionId), ct);
                await _conn.SendAsync(new SM_HOUSE_REGISTRY(1, house.Registry, player.GetHouseObjectReuseDelay), ct);
                await _conn.SendAsync(new SM_HOUSE_REGISTRY(2, house.Registry), ct);
                break;

            case HousingAction.ExitDecoration:
                await _conn.SendAsync(new SM_HOUSE_EDIT(_actionId), ct);
                break;

            case HousingAction.AddItem:
                await HandleAddItemAsync(player, house, ct);
                break;

            case HousingAction.DeleteItem:
                await HandleDeleteItemAsync(house, ct);
                break;

            case HousingAction.SpawnObject:
                await HandleSpawnObjectAsync(player, house, ct);
                break;

            case HousingAction.MoveObject:
                await HandleMoveObjectAsync(player, house, ct);
                break;

            case HousingAction.DespawnObject:
                await HandleDespawnObjectAsync(house, ct);
                break;

            case HousingAction.EnterRenovation:
                await _conn.SendAsync(new SM_HOUSE_EDIT(14), ct);
                break;

            case HousingAction.ExitRenovation:
                await _conn.SendAsync(new SM_HOUSE_EDIT(15), ct);
                break;

            case HousingAction.ChangeAppearance:
                // note: building renovation (coupon consumption + HousingService.SwitchHouseBuilding) is
                // not implemented in this phase — see class doc.
                break;
        }
    }

    private async Task HandleAddItemAsync(Player player, House house, CancellationToken ct)
    {
        var item = player.Inventory.Get(_itemObjectId);
        if (item is null) return;

        var template = _dataManager.Items.GetTemplate(item.ItemId);
        if (template is null) return;

        player.Inventory.Remove(_itemObjectId);
        await _itemDao.DeleteAsync(_itemObjectId, ct);
        await _conn.SendAsync(new SM_DELETE_ITEM(_itemObjectId), ct);

        var decoAction = template.Actions?.HouseDeco;
        if (decoAction is not null)
        {
            var part = _dataManager.HouseParts.GetPartById(decoAction.TemplateId);
            if (part is null) return; // note: house_parts.xml missing this id — nothing to register.

            var decor = new HouseDecoration(decoAction.TemplateId, part.Type, floor: -1, objectId: ObjectIdFactory.Next());
            house.Registry.PutCustomPart(decor);
            await _registeredItemsDao.UpsertAsync(house.PlayerObjectId, RegisteredItemRowMapper.ForDecoration(decor), ct);
            decor.PersistentState = PersistentState.Updated;

            await _conn.SendAsync(new SM_HOUSE_EDIT(3, 2, decor.ObjectId, decor: decor), ct);
            return;
        }

        var obj = HouseObjectFactory.CreateFromItem(house, template, _dataManager);
        if (obj is null) return; // note: item has neither a <houseobject> nor a <housedeco> action — nothing to register.

        house.Registry.PutObject(obj);
        await _registeredItemsDao.UpsertAsync(house.PlayerObjectId, RegisteredItemRowMapper.ForObject(obj), ct);
        obj.PersistentState = PersistentState.Updated;

        await _conn.SendAsync(new SM_HOUSE_EDIT(3, 1, obj.ObjectId, obj: obj), ct);
    }

    private async Task HandleDeleteItemAsync(House house, CancellationToken ct)
    {
        var removed = house.Registry.RemoveObject(_itemObjectId);
        if (removed is null) return;

        if (removed.PersistentState == PersistentState.Deleted)
        {
            await _registeredItemsDao.DeleteAsync(_itemObjectId, ct);
            house.Registry.DiscardObject(_itemObjectId);
        }

        await _conn.SendAsync(new SM_HOUSE_EDIT(4, 1, _itemObjectId), ct);
        await _conn.SendAsync(new SM_HOUSE_EDIT(4, 1, _itemObjectId), ct);
    }

    private async Task HandleSpawnObjectAsync(Player player, House house, CancellationToken ct)
    {
        var obj = house.Registry.GetObjectByObjId(_itemObjectId);
        if (obj is null) return;

        obj.X = _x;
        obj.Y = _y;
        obj.Z = _z;
        obj.Rotation = _rotation;
        obj.MarkDirty();

        await _conn.SendAsync(new SM_HOUSE_EDIT(5, obj, _x, _y, _z, _rotation,
            player.GetHouseObjectReuseDelay(obj.ObjectId), player.ObjectId, house.PlayerObjectId), ct);

        await _registeredItemsDao.UpsertAsync(house.PlayerObjectId, RegisteredItemRowMapper.ForObject(obj), ct);
        obj.PersistentState = PersistentState.Updated;

        // "Removed from not-yet-placed pool" ack — Java's trailing SM_HOUSE_EDIT(4, 1, itemObjectId).
        await _conn.SendAsync(new SM_HOUSE_EDIT(4, 1, _itemObjectId), ct);
        // note: Java also fires QuestEngine.onHouseItemUseEvent here — no quest hook for house furniture
        // placement exists in this port's quest engine yet.
    }

    private async Task HandleMoveObjectAsync(Player player, House house, CancellationToken ct)
    {
        var obj = house.Registry.GetObjectByObjId(_itemObjectId);
        if (obj is null) return;

        // Temporary "despawn" animation frame while the object is being dragged.
        await _conn.SendAsync(new SM_HOUSE_EDIT(7, 0, _itemObjectId), ct);

        obj.X = _x;
        obj.Y = _y;
        obj.Z = _z;
        obj.Rotation = _rotation;
        obj.MarkDirty();

        await _registeredItemsDao.UpsertAsync(house.PlayerObjectId, RegisteredItemRowMapper.ForObject(obj), ct);
        obj.PersistentState = PersistentState.Updated;

        await _conn.SendAsync(new SM_HOUSE_EDIT(5, obj, _x, _y, _z, _rotation,
            player.GetHouseObjectReuseDelay(obj.ObjectId), player.ObjectId, house.PlayerObjectId), ct);
    }

    private async Task HandleDespawnObjectAsync(House house, CancellationToken ct)
    {
        var obj = house.Registry.GetObjectByObjId(_itemObjectId);
        if (obj is null) return;

        await _conn.SendAsync(new SM_HOUSE_EDIT(7, 0, _itemObjectId), ct);

        obj.RemoveFromHouse();
        obj.MarkDirty();

        await _registeredItemsDao.UpsertAsync(house.PlayerObjectId, RegisteredItemRowMapper.ForObject(obj), ct);
        obj.PersistentState = PersistentState.Updated;

        // "Place it back" into the not-yet-placed pool ack.
        await _conn.SendAsync(new SM_HOUSE_EDIT(3, 1, _itemObjectId, obj: obj), ct);
    }
}
