using AionLightning.Commons.Network;
using AionLightning.Game.Network.Aion.ServerPackets;
using GameWorld = AionLightning.Game.World.World;

namespace AionLightning.Game.Network.Aion.ClientPackets;

public sealed class CM_VIEW_PLAYER_DETAILS : AionClientPacket
{
    private readonly GsClientConnection _conn;
    private readonly GameWorld _world;
    private int _targetObjectId;

    public CM_VIEW_PLAYER_DETAILS(GsClientConnection conn, GameWorld world)
    {
        _conn  = conn;
        _world = world;
    }

    public override void Read(ref PacketReader r) => _targetObjectId = r.ReadD();

    public override async ValueTask RunAsync(CancellationToken ct)
    {
        if (_conn.ActivePlayer is null) return;

        var target = _world.GetPlayerByObjectId(_targetObjectId);
        if (target is null) return;

        var equipped = target.Inventory.All.Where(i => i.IsEquipped);
        await _conn.SendAsync(new SM_VIEW_PLAYER_DETAILS(target.ObjectId, equipped), ct);
    }
}
