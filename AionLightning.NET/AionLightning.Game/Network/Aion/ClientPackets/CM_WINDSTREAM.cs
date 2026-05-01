using AionLightning.Commons.Network;
using AionLightning.Game.Model;
using AionLightning.Game.Network.Aion.ServerPackets;

namespace AionLightning.Game.Network.Aion.ClientPackets;

/// <summary>
/// Client enters/exits/transitions inside a windstream. Opcode 0x2E4.
/// state 0 = exit windstream entirely; 1 = enter; 2 = exit to glide; 3 = exit to land;
/// 4 = unk; 7 = start boost; 8 = end boost.
/// </summary>
public sealed class CM_WINDSTREAM : AionClientPacket
{
    private readonly GsClientConnection       _conn;
    private readonly PlayerConnectionRegistry _connRegistry;

    private int _teleportId;
    private int _distance;
    private int _state;

    public CM_WINDSTREAM(GsClientConnection conn, PlayerConnectionRegistry connRegistry)
    {
        _conn         = conn;
        _connRegistry = connRegistry;
    }

    public override void Read(ref PacketReader r)
    {
        _teleportId = r.ReadD();
        _distance   = r.ReadD();
        _state      = r.ReadD();
    }

    public override async ValueTask RunAsync(CancellationToken ct)
    {
        var player = _conn.ActivePlayer;
        if (player is null || player.IsAlreadyDead) return;

        int worldId = player.Position.WorldId;

        switch (_state)
        {
            case 1:
                // Enter windstream: must be gliding already, not already in windstream
                if (!player.State.HasFlag(CreatureState.Gliding)) return;
                if (player.State.HasFlag(CreatureState.Flying)) return;

                player.State &= ~CreatureState.Active;
                player.State &= ~CreatureState.Gliding;
                player.State |=  CreatureState.Flying;

                await BroadcastAsync(new SM_EMOTION(player, EmotionType.WINDSTREAM, _teleportId, _distance), worldId, ct);
                break;

            case 2:
                // Exit windstream into glide
                player.State &= ~CreatureState.Flying;
                player.State |=  CreatureState.Active;
                player.State |=  CreatureState.Gliding;

                await BroadcastAsync(new SM_EMOTION(player, EmotionType.WINDSTREAM_END), worldId, ct);
                try { await _conn.SendAsync(new SM_WINDSTREAM(_state), ct); } catch { }
                break;

            case 3:
                // Exit windstream to land
                player.State &= ~CreatureState.Flying;
                player.State &= ~CreatureState.Gliding;
                player.State |=  CreatureState.Active;

                await BroadcastAsync(new SM_EMOTION(player, EmotionType.WINDSTREAM_EXIT), worldId, ct);
                try { await _conn.SendAsync(new SM_WINDSTREAM(_state), ct); } catch { }
                break;

            case 0:
            case 4:
                try { await _conn.SendAsync(new SM_WINDSTREAM(_state), ct); } catch { }
                break;

            case 7:
                await BroadcastAsync(new SM_EMOTION(player, EmotionType.WINDSTREAM_START_BOOST), worldId, ct);
                try { await _conn.SendAsync(new SM_WINDSTREAM(_state), ct); } catch { }
                break;

            case 8:
                await BroadcastAsync(new SM_EMOTION(player, EmotionType.WINDSTREAM_END_BOOST), worldId, ct);
                try { await _conn.SendAsync(new SM_WINDSTREAM(_state), ct); } catch { }
                break;
        }
    }

    private async ValueTask BroadcastAsync(AionServerPacket packet, int worldId, CancellationToken ct)
    {
        try { await _conn.SendAsync(packet, ct); } catch { }
        foreach (var other in _connRegistry.GetAllExcept(_conn.ActivePlayer!.ObjectId))
            if (other.ActivePlayer?.Position.WorldId == worldId)
                try { await other.SendAsync(packet, ct); } catch { }
    }
}
