using AionLightning.Commons.Events;
using AionLightning.Commons.Network;
using AionLightning.Game.Configs.Options;
using AionLightning.Game.Controllers;
using AionLightning.Game.Events;
using AionLightning.Game.Model;
using AionLightning.Game.Network.Aion.ServerPackets;
using AionLightning.Game.Services;
using Microsoft.Extensions.Options;
using GameWorld = AionLightning.Game.World.World;

namespace AionLightning.Game.Network.Aion.ClientPackets;

public sealed class CM_LEVEL_READY : AionClientPacket
{
    private readonly GsClientConnection _conn;
    private readonly GameWorld _world;
    private readonly PlayerConnectionRegistry _connRegistry;
    private readonly IEventBus _eventBus;
    private readonly SiegeService _siegeService;
    private readonly HousingService _housingService;
    private readonly HouseController _houseController;
    private readonly HousingOptions _housingOptions;

    public CM_LEVEL_READY(GsClientConnection conn, GameWorld world,
        PlayerConnectionRegistry connRegistry, IEventBus eventBus, SiegeService siegeService,
        HousingService housingService, HouseController houseController, IOptions<HousingOptions> housingOptions)
    {
        _conn         = conn;
        _world        = world;
        _connRegistry = connRegistry;
        _eventBus     = eventBus;
        _siegeService = siegeService;
        _housingService  = housingService;
        _houseController = houseController;
        _housingOptions  = housingOptions.Value;
    }

    public override void Read(ref PacketReader r) { }

    public override async ValueTask RunAsync(CancellationToken ct)
    {
        var player = _conn.ActivePlayer;
        if (player is null) return;

        // Gather equipped items for this player (used in SM_PLAYER_INFO)
        var playerEquipment = player.Inventory.All.Where(i => i.IsEquipped).ToList();

        // Send this player's info to themselves
        await _conn.SendAsync(new SM_PLAYER_INFO(player, player.Appearance, enemy: false, playerEquipment), ct);

        // Restore bind point display (client forgets it on zone reload)
        if (player.BindPosition.HasValue)
            try { await _conn.SendAsync(new SM_BIND_POINT_INFO(player.BindPosition.Value), ct); } catch { }

        // Broadcast entering player's motion and current active effects to zone
        var motionBroadcast  = SM_MOTION.Broadcast(player.ObjectId, player.ActiveMotions);
        var selfAbnormal     = new SM_ABNORMAL_EFFECT(player.ObjectId, isPlayer: true, player.GetActiveEffects());
        await _conn.SendAsync(selfAbnormal, ct);
        // Own buff bar with remaining durations (Java PlayerEffectController → SM_ABNORMAL_STATE)
        try { await _conn.SendAsync(new SM_ABNORMAL_STATE(player.GetActiveEffects()), ct); } catch { }

        // Introduce each already-online player in the same zone/channel to the newcomer and vice versa
        var scope          = player.Position;
        var playerSettings = new SM_CUSTOM_SETTINGS(player.ObjectId, player.DisplaySettings, player.DenySettings);
        var playerInfo     = new SM_PLAYER_INFO(player, player.Appearance, enemy: false, playerEquipment);
        foreach (var otherConn in _connRegistry.GetAllExcept(player.ObjectId))
        {
            var other = otherConn.ActivePlayer;
            if (other is null || !other.Position.SameScope(scope)) continue;

            var otherEquipment = other.Inventory.All.Where(i => i.IsEquipped).ToList();

            // New player sees existing player + their motion + active effects + social settings + legion title
            try { await _conn.SendAsync(new SM_PLAYER_INFO(other, other.Appearance, enemy: false, otherEquipment), ct); } catch { }
            try { await _conn.SendAsync(SM_MOTION.Broadcast(other.ObjectId, other.ActiveMotions), ct); } catch { }
            try { await _conn.SendAsync(new SM_ABNORMAL_EFFECT(other.ObjectId, isPlayer: true, other.GetActiveEffects()), ct); } catch { }
            try { await _conn.SendAsync(new SM_CUSTOM_SETTINGS(other.ObjectId, other.DisplaySettings, other.DenySettings), ct); } catch { }
            if (other.Legion is { } otherLegion && otherLegion.Members.TryGetValue(other.ObjectId, out var otherMember))
                try { await _conn.SendAsync(new SM_LEGION_UPDATE_TITLE(other.ObjectId, otherLegion.LegionId, otherLegion.Name, otherMember.Rank), ct); } catch { }

            // Existing player sees new player + their active effects + social settings + legion title
            try { await otherConn.SendAsync(playerInfo, ct); } catch { }
            try { await otherConn.SendAsync(motionBroadcast, ct); } catch { }
            try { await otherConn.SendAsync(new SM_ABNORMAL_EFFECT(player.ObjectId, isPlayer: true, player.GetActiveEffects()), ct); } catch { }
            try { await otherConn.SendAsync(playerSettings, ct); } catch { }
            if (player.Legion is { } myLegion && myLegion.Members.TryGetValue(player.ObjectId, out var myMember))
                try { await otherConn.SendAsync(new SM_LEGION_UPDATE_TITLE(player.ObjectId, myLegion.LegionId, myLegion.Name, myMember.Rank), ct); } catch { }
        }

        // Introduce spawned NPCs in the same zone/channel to the entering player
        foreach (var npc in _world.GetNpcsInScope(scope))
            try { await _conn.SendAsync(new SM_NPC_INFO(npc), ct); } catch { }

        // M381: introduce active summons (Spiritmaster spirits, etc.) in the same zone/channel
        foreach (var summon in _world.GetSummonsInScope(scope))
            try { await _conn.SendAsync(new SM_NPC_INFO(summon), ct); } catch { }

        // Introduce gatherables in the same zone/channel
        foreach (var g in _world.GetGatherablesInScope(scope).Where(g => !g.IsGathered))
            try { await _conn.SendAsync(new SM_GATHERABLE_INFO(g), ct); } catch { }

        // Housing P2: introduce every spawned house in the entering player's scope. note: this only
        // renders houses at enter-world time — Java also re-evaluates house visibility on every move
        // (HouseController.see/notSee via the knownlist range check); that per-move wiring isn't hooked
        // into CM_MOVE/ZoneService here to avoid touching the byte-verified movement path, so a house
        // that comes into/out of range without a full zone reload won't render/despawn until the next
        // enter-world. No-op entirely unless HousingOptions.Enable is true.
        if (_housingOptions.Enable)
        {
            foreach (var house in _housingService.GetHousesInScope(scope))
                try { await _houseController.SeeAsync(house, player, _conn, ct); } catch { }

            // P3: Java CM_LEVEL_READY also sends the player's own placed-furniture list here
            // (player.getHouseRegistry() != null branch).
            if (player.ActiveHouse is { } activeHouse)
                try { await _conn.SendAsync(new SM_HOUSE_OBJECTS(activeHouse.Registry.SpawnedObjects), ct); } catch { }
        }

        await _eventBus.PublishAsync(new PlayerEnteredWorldEvent(player), ct);

        await _siegeService.OnEnterSiegeWorldAsync(player, _conn, ct);
    }
}
