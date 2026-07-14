using AionLightning.Commons.Network;
using AionLightning.Game.Dao;
using AionLightning.Game.DataHolders;
using AionLightning.Game.Model;
using AionLightning.Game.Network.Aion.ServerPackets;
using AionLightning.Game.Services;
using GameWorld = AionLightning.Game.World.World;

namespace AionLightning.Game.Network.Aion.ClientPackets;

/// <summary>Client requests NPC dialog. Opcode 0x116.</summary>
public sealed class CM_SHOW_DIALOG : AionClientPacket
{
    private const int KinahItemId = 182400001;

    private readonly GsClientConnection  _conn;
    private readonly GameWorld           _world;
    private readonly IDataManager        _dataManager;
    private readonly IPlayerDao          _playerDao;
    private readonly IItemDao            _itemDao;
    private readonly PortalService       _portalService;
    private readonly RiftService         _riftService;
    private readonly PrivateStoreService _privateStoreService;

    private int _targetObjectId;

    public CM_SHOW_DIALOG(GsClientConnection conn, GameWorld world,
        IDataManager dataManager, IPlayerDao playerDao, IItemDao itemDao, PortalService portalService,
        RiftService riftService, PrivateStoreService privateStoreService)
    {
        _conn                = conn;
        _world               = world;
        _dataManager         = dataManager;
        _playerDao           = playerDao;
        _itemDao             = itemDao;
        _portalService       = portalService;
        _riftService         = riftService;
        _privateStoreService = privateStoreService;
    }

    public override void Read(ref PacketReader r) => _targetObjectId = r.ReadD();

    private const float MaxInteractRange = 10.0f;

    public override async ValueTask RunAsync(CancellationToken ct)
    {
        var player = _conn.ActivePlayer;
        if (player is null) return;

        // Private store: if the target is a player, try to send its store listing (no-op if closed)
        var storeOwner = _world.GetPlayerByObjectId(_targetObjectId);
        if (storeOwner is not null)
        {
            await _privateStoreService.GetStoreListAsync(player, _targetObjectId, ct);
            return;
        }

        var npc = _world.GetNpcByObjectId(_targetObjectId);
        if (npc is null) return;
        if (player.Position.DistanceTo(npc.Position) > MaxInteractRange) return;

        // Portal NPC: instantly teleport without dialog (routes through PortalService, which also
        // handles instance allocation/re-entry when the destination is an instanced world).
        if (await _portalService.UsePortalAsync(player, npc, ct)) return;

        // Rift invasion portal NPC (master or slave of a currently-open RiftService rift): teleport
        // through on interact, or silently no-op on the slave/arrival side. See
        // RiftService.TryUseRiftPortalAsync's doc comment for why there's no confirmation dialog here.
        if (await _riftService.TryUseRiftPortalAsync(player, npc, ct)) return;

        if (string.Equals(npc.Template.NpcType, "BINDSTONE", StringComparison.OrdinalIgnoreCase))
        {
            long price = _dataManager.BindPoints.GetPrice(npc.Template.NpcId);
            if (price > 0)
            {
                var kinah = player.Inventory.FindByItemId(KinahItemId);
                if (kinah is null || kinah.Count < price)
                {
                    await _conn.SendAsync(SM_SYSTEM_MESSAGE.NoEnoughKinah(), ct);
                    return;
                }
                kinah.Count -= price;
                await _itemDao.SaveAllAsync(player.ObjectId, player.Inventory.All, ct);
                await _conn.SendAsync(new SM_INVENTORY_ADD_ITEM([kinah]), ct);
            }

            player.BindPosition = npc.Position;
            await _playerDao.UpdateBindPointAsync(player.ObjectId, npc.Position, ct);
            await _conn.SendAsync(new SM_BIND_POINT_INFO(npc.Position), ct);
            return;
        }

        // dialogId 10 = standard NPC greeting (shows Buy/Sell/Quest buttons depending on NPC type)
        await _conn.SendAsync(new SM_DIALOG_WINDOW(_targetObjectId, dialogId: 10, questId: 0), ct);
    }
}
