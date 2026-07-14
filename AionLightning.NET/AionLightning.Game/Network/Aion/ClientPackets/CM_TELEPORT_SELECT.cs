using AionLightning.Commons.Network;
using AionLightning.Game.Dao;
using AionLightning.Game.DataHolders;
using AionLightning.Game.Model;
using AionLightning.Game.Network.Aion.ServerPackets;
using AionLightning.Game.Services;
using GameWorld = AionLightning.Game.World.World;

namespace AionLightning.Game.Network.Aion.ClientPackets;

/// <summary>Player selects a teleport destination from a teleporter NPC. Opcode 0x176.</summary>
public sealed class CM_TELEPORT_SELECT : AionClientPacket
{
    private const int KinahItemId = 182400001;

    private readonly GsClientConnection _conn;
    private readonly GameWorld          _world;
    private readonly IDataManager       _dataManager;
    private readonly IItemDao           _itemDao;
    private readonly TeleportService    _teleport;

    private int _targetObjectId;
    private int _locId;

    public CM_TELEPORT_SELECT(GsClientConnection conn, GameWorld world,
        IDataManager dataManager, IItemDao itemDao, TeleportService teleport)
    {
        _conn        = conn;
        _world       = world;
        _dataManager = dataManager;
        _itemDao     = itemDao;
        _teleport    = teleport;
    }

    public override void Read(ref PacketReader r)
    {
        _targetObjectId = r.ReadD();
        _locId          = r.ReadD();
        r.ReadH(); // unk
    }

    public override async ValueTask RunAsync(CancellationToken ct)
    {
        var player = _conn.ActivePlayer;
        if (player is null || player.IsAlreadyDead) return;

        var npc = _world.GetNpcByObjectId(_targetObjectId);
        if (npc is null) return;

        var dest = _dataManager.Teleports.GetDestination(npc.Template.NpcId, _locId);
        if (dest is null) return;

        // FLIGHT-type destinations glide via a flypath_template.xml route (keyed by the same loc_id)
        // instead of an instant teleport — teleport_location.xml has no real coordinates for these
        // (see the "flying" comment on those entries), so they must branch before the coordinate lookup.
        if (dest.Type == "FLIGHT")
        {
            if (!await TryDeductPriceAsync(player, dest.Price, ct)) return;
            await _teleport.FlyTeleportAsync(player, (short)_locId, ct);
            return;
        }

        var loc = _dataManager.Teleports.GetLocation(_locId);
        if (loc is null || (loc.X == 0f && loc.Y == 0f)) return; // flying-only locations lack coordinates

        if (!await TryDeductPriceAsync(player, dest.Price, ct)) return;

        // Flight-master destinations are all open-world (channel 0).
        await _teleport.TeleportToAsync(player, loc.MapId, 0, loc.X, loc.Y, loc.Z, (byte)loc.Heading, portAnimation: 0, ct);
    }

    /// <summary>Deducts kinah for the destination (Java <c>checkKinahForTransportation</c>). Sends the
    /// update as SM_INVENTORY_ADD_ITEM (not an "item acquired" popup) so the client shows a plain
    /// balance change.</summary>
    private async ValueTask<bool> TryDeductPriceAsync(Player player, long price, CancellationToken ct)
    {
        if (price <= 0) return true;

        var kinah = player.Inventory.FindByItemId(KinahItemId);
        if (kinah is null || kinah.Count < price) return false;

        kinah.Count -= price;
        await _itemDao.SaveAllAsync(player.ObjectId, player.Inventory.All, ct);
        // SM_INVENTORY_ADD_ITEM doubles as an update packet for existing stacks
        await _conn.SendAsync(new SM_INVENTORY_ADD_ITEM([kinah]), ct);
        return true;
    }
}
