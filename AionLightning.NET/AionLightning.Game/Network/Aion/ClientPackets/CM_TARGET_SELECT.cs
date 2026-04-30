using AionLightning.Commons.Network;
using AionLightning.Game.Model;
using AionLightning.Game.Network.Aion.ServerPackets;
using GameWorld = AionLightning.Game.World.World;

namespace AionLightning.Game.Network.Aion.ClientPackets;

public sealed class CM_TARGET_SELECT : AionClientPacket
{
    private readonly GsClientConnection _conn;
    private readonly GameWorld _world;
    private readonly PlayerConnectionRegistry _connRegistry;

    private int _targetObjectId;
    private int _type;

    public CM_TARGET_SELECT(GsClientConnection conn, GameWorld world,
        PlayerConnectionRegistry connRegistry)
    {
        _conn         = conn;
        _world        = world;
        _connRegistry = connRegistry;
    }

    public override void Read(ref PacketReader r)
    {
        _targetObjectId = r.ReadD();
        _type           = r.ReadC();
    }

    public override async ValueTask RunAsync(CancellationToken ct)
    {
        var player = _conn.ActivePlayer;
        if (player is null) return;

        if (_targetObjectId == 0)
        {
            player.Target = null;
        }
        else if (_targetObjectId == player.ObjectId)
        {
            player.Target = player;
        }
        else
        {
            // Look up in world (players + NPCs + gatherables)
            VisibleObject? obj = _world.GetPlayerByObjectId(_targetObjectId)
                              ?? (VisibleObject?)_world.GetNpcByObjectId(_targetObjectId)
                              ?? (VisibleObject?)_world.GetGatherable(_targetObjectId);
            player.Target = obj;
        }

        await _conn.SendAsync(new SM_TARGET_SELECTED(player), ct);
        int worldId = player.Position.WorldId;
        foreach (var other in _connRegistry.GetAllExcept(player.ObjectId))
            if (other.ActivePlayer?.Position.WorldId == worldId)
                try { await other.SendAsync(new SM_TARGET_UPDATE(player), ct); } catch { }
    }
}
