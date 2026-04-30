using AionLightning.Commons.Events;
using AionLightning.Commons.Network;
using AionLightning.Game.Events;
using AionLightning.Game.Model;
using AionLightning.Game.Network.Aion.ServerPackets;
using GameWorld = AionLightning.Game.World.World;

namespace AionLightning.Game.Network.Aion.ClientPackets;

public sealed class CM_LEVEL_READY : AionClientPacket
{
    private readonly GsClientConnection _conn;
    private readonly GameWorld _world;
    private readonly PlayerConnectionRegistry _connRegistry;
    private readonly IEventBus _eventBus;

    public CM_LEVEL_READY(GsClientConnection conn, GameWorld world,
        PlayerConnectionRegistry connRegistry, IEventBus eventBus)
    {
        _conn         = conn;
        _world        = world;
        _connRegistry = connRegistry;
        _eventBus     = eventBus;
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

        // Broadcast entering player's motion and clear abnormal effects to others
        var motionBroadcast   = SM_MOTION.Broadcast(player.ObjectId);
        var abnormalClearSelf = new SM_ABNORMAL_EFFECT(player.ObjectId, isPlayer: true);
        await _conn.SendAsync(abnormalClearSelf, ct);

        // Introduce each already-online player in the same zone to the newcomer and vice versa
        int worldId        = player.Position.WorldId;
        var playerSettings = new SM_CUSTOM_SETTINGS(player.ObjectId, player.DisplaySettings, player.DenySettings);
        var playerInfo     = new SM_PLAYER_INFO(player, player.Appearance, enemy: false, playerEquipment);
        foreach (var otherConn in _connRegistry.GetAllExcept(player.ObjectId))
        {
            var other = otherConn.ActivePlayer;
            if (other is null || other.Position.WorldId != worldId) continue;

            var otherEquipment = other.Inventory.All.Where(i => i.IsEquipped).ToList();

            // New player sees existing player + their motion + clear abnormal + social settings + legion title
            try { await _conn.SendAsync(new SM_PLAYER_INFO(other, other.Appearance, enemy: false, otherEquipment), ct); } catch { }
            try { await _conn.SendAsync(SM_MOTION.Broadcast(other.ObjectId), ct); } catch { }
            try { await _conn.SendAsync(new SM_ABNORMAL_EFFECT(other.ObjectId, isPlayer: true), ct); } catch { }
            try { await _conn.SendAsync(new SM_CUSTOM_SETTINGS(other.ObjectId, other.DisplaySettings, other.DenySettings), ct); } catch { }
            if (other.Legion is { } otherLegion && otherLegion.Members.TryGetValue(other.ObjectId, out var otherMember))
                try { await _conn.SendAsync(new SM_LEGION_UPDATE_TITLE(other.ObjectId, otherLegion.LegionId, otherLegion.Name, otherMember.Rank), ct); } catch { }

            // Existing player sees new player + their social settings + legion title
            try { await otherConn.SendAsync(playerInfo, ct); } catch { }
            try { await otherConn.SendAsync(motionBroadcast, ct); } catch { }
            try { await otherConn.SendAsync(new SM_ABNORMAL_EFFECT(player.ObjectId, isPlayer: true), ct); } catch { }
            try { await otherConn.SendAsync(playerSettings, ct); } catch { }
            if (player.Legion is { } myLegion && myLegion.Members.TryGetValue(player.ObjectId, out var myMember))
                try { await otherConn.SendAsync(new SM_LEGION_UPDATE_TITLE(player.ObjectId, myLegion.LegionId, myLegion.Name, myMember.Rank), ct); } catch { }
        }

        // Introduce spawned NPCs in the same zone to the entering player
        foreach (var npc in _world.GetAllNpcs().Where(n => n.Position.WorldId == worldId))
            try { await _conn.SendAsync(new SM_NPC_INFO(npc), ct); } catch { }

        await _eventBus.PublishAsync(new PlayerEnteredWorldEvent(player), ct);
    }
}
