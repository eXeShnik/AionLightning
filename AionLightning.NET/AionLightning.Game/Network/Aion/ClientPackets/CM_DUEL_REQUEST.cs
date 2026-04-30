using AionLightning.Commons.Network;
using AionLightning.Game.Network.Aion.ServerPackets;
using AionLightning.Game.Services;
using GameWorld = AionLightning.Game.World.World;

namespace AionLightning.Game.Network.Aion.ClientPackets;

/// <summary>Client requests a duel with a target player. Auto-accepts. Opcode 0x130.</summary>
public sealed class CM_DUEL_REQUEST : AionClientPacket
{
    private readonly GsClientConnection       _conn;
    private readonly PlayerConnectionRegistry _connRegistry;
    private readonly DuelService              _duelService;
    private readonly GameWorld                _world;

    private int _targetObjectId;

    public CM_DUEL_REQUEST(GsClientConnection conn, PlayerConnectionRegistry connRegistry,
        DuelService duelService, GameWorld world)
    {
        _conn         = conn;
        _connRegistry = connRegistry;
        _duelService  = duelService;
        _world        = world;
    }

    public override void Read(ref PacketReader r) => _targetObjectId = r.ReadD();

    public override async ValueTask RunAsync(CancellationToken ct)
    {
        var player = _conn.ActivePlayer;
        if (player is null || player.IsAlreadyDead) return;

        var target = _world.GetPlayerByObjectId(_targetObjectId);
        if (target is null || target.IsAlreadyDead || target.ObjectId == player.ObjectId) return;

        if (_duelService.IsDueling(player.ObjectId) || _duelService.IsDueling(target.ObjectId)) return;

        _duelService.StartDuel(player.ObjectId, target.ObjectId);

        await _conn.SendAsync(SM_DUEL.Started(target.ObjectId), ct);

        var targetConn = _connRegistry.Get(target.ObjectId);
        if (targetConn is not null)
            try { await targetConn.SendAsync(SM_DUEL.Started(player.ObjectId), ct); } catch { }
    }
}
