using AionLightning.Commons.Network;
using AionLightning.Game.Dao;
using AionLightning.Game.Model;
using AionLightning.Game.Network.Aion.ServerPackets;
using AionLightning.Game.Services;

namespace AionLightning.Game.Network.Aion.ClientPackets;

/// <summary>
/// Client submits an appearance or rename request. Opcode 0x167.
/// type=0: player rename (consumes item 169670000/169670001, updates name, re-broadcasts — see
///         <see cref="RenameService"/>).
/// type=1: legion rename (consumes item 169680000/169680001, updates legion name, notifies members).
/// type=2: cosmetic item — stub (no-op; requires CosmeticItemAction).
/// </summary>
public sealed class CM_APPEARANCE : AionClientPacket
{
    // Legion-rename coupon itemIds (Java RenameService.renameLegion constants)
    private static readonly HashSet<int> LegionRenameItemIds = [169680000, 169680001];
    // Player-rename coupon itemIds (Java RenameService.renamePlayer constants)
    private static readonly HashSet<int> PlayerRenameItemIds = [RenameService.RenameCouponItemId1, RenameService.RenameCouponItemId2];

    private readonly GsClientConnection       _conn;
    private readonly PlayerConnectionRegistry _connRegistry;
    private readonly IItemDao                 _itemDao;
    private readonly ILegionDao               _legionDao;
    private readonly RenameService             _renameService;

    private byte   _type;
    private int    _itemObjId;
    private string _name = string.Empty;

    public CM_APPEARANCE(GsClientConnection conn,
        PlayerConnectionRegistry connRegistry, IItemDao itemDao, ILegionDao legionDao, RenameService renameService)
    {
        _conn         = conn;
        _connRegistry = connRegistry;
        _itemDao      = itemDao;
        _legionDao    = legionDao;
        _renameService = renameService;
    }

    public override void Read(ref PacketReader r)
    {
        _type     = (byte)r.ReadC();
        r.ReadC();
        r.ReadH();
        _itemObjId = r.ReadD();
        if (_type is 0 or 1)
            _name = r.ReadS();
    }

    public override async ValueTask RunAsync(CancellationToken ct)
    {
        var player = _conn.ActivePlayer;
        if (player is null) return;

        switch (_type)
        {
            case 0: await HandlePlayerRenameAsync(player, ct); break;
            case 1: await HandleLegionRenameAsync(player, ct); break;
        }
    }

    private async ValueTask HandlePlayerRenameAsync(Player player, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(_name)) return;

        var renameItem = player.Inventory.Get(_itemObjId);
        if (renameItem is null || !PlayerRenameItemIds.Contains(renameItem.ItemId)) return;

        var result = await _renameService.RenameAsync(player, _name, ct);
        switch (result)
        {
            case RenameResult.InvalidFormat:
                await _conn.SendAsync(SM_SYSTEM_MESSAGE.RenameNameInvalid(), ct);
                return;
            case RenameResult.Forbidden:
            case RenameResult.Taken:
                await _conn.SendAsync(SM_SYSTEM_MESSAGE.RenameNameTaken(), ct);
                return;
            case RenameResult.Unchanged:
                await _conn.SendAsync(SM_SYSTEM_MESSAGE.RenameNameUnchanged(), ct);
                return;
        }

        renameItem.Count--;
        if (renameItem.Count <= 0)
        {
            player.Inventory.Remove(renameItem.UniqueId);
            await _itemDao.DeleteAsync(renameItem.UniqueId, ct);
            await _conn.SendAsync(new SM_DELETE_ITEM((int)renameItem.UniqueId), ct);
        }
        else
        {
            await _conn.SendAsync(new SM_INVENTORY_ADD_ITEM([renameItem]), ct);
        }

        int worldId = player.Position.WorldId;
        var equipment = player.Inventory.All.Where(i => i.IsEquipped).ToList();
        var playerInfo = new SM_PLAYER_INFO(player, player.Appearance, enemy: false, equipment);
        await _conn.SendAsync(playerInfo, ct);
        foreach (var other in _connRegistry.GetAllExcept(player.ObjectId))
            if (other.ActivePlayer?.Position.WorldId == worldId)
                try { await other.SendAsync(playerInfo, ct); } catch { }
    }

    private async ValueTask HandleLegionRenameAsync(Player player, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(_name) || _name.Length < 2 || _name.Length > 20) return;

        var legion = player.Legion;
        if (legion is null) return;

        // Validate rename item
        var renameItem = player.Inventory.Get(_itemObjId);
        if (renameItem is null || !LegionRenameItemIds.Contains(renameItem.ItemId)) return;

        // Name validations matching Java RenameService.renameLegion
        if (string.Equals(legion.Name, _name, StringComparison.OrdinalIgnoreCase))
        {
            await _conn.SendAsync(SM_SYSTEM_MESSAGE.LegionNameUnchanged(), ct);
            return;
        }

        if (await _legionDao.IsNameUsedAsync(_name, ct))
        {
            await _conn.SendAsync(SM_SYSTEM_MESSAGE.LegionNameTaken(), ct);
            return;
        }

        // Consume rename item
        renameItem.Count--;
        if (renameItem.Count <= 0)
        {
            player.Inventory.Remove(renameItem.UniqueId);
            await _itemDao.DeleteAsync(renameItem.UniqueId, ct);
            await _conn.SendAsync(new SM_DELETE_ITEM((int)renameItem.UniqueId), ct);
        }
        else
        {
            await _conn.SendAsync(new SM_INVENTORY_ADD_ITEM([renameItem]), ct);
        }

        // Update legion name in DB and in-memory
        await _legionDao.UpdateNameAsync(legion.LegionId, _name, ct);
        legion.Name = _name;

        // Notify all online legion members
        var successMsg = SM_SYSTEM_MESSAGE.LegionRenamed(_name);
        foreach (var member in legion.Members.Values)
        {
            var memberConn = _connRegistry.Get(member.ObjectId);
            if (memberConn is not null)
                try { await memberConn.SendAsync(successMsg, ct); } catch { }
        }
    }
}
