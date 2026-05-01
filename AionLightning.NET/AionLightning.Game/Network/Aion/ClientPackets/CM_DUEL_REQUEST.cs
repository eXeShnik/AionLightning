using AionLightning.Commons.Network;
using AionLightning.Game.Network.Aion.ServerPackets;
using AionLightning.Game.Services;
using GameWorld = AionLightning.Game.World.World;

namespace AionLightning.Game.Network.Aion.ClientPackets;

/// <summary>
/// Client requests a duel with a target player. Opcode 0x130.
/// Sends an SM_QUESTION_WINDOW to the target; duel starts only on acceptance.
/// </summary>
public sealed class CM_DUEL_REQUEST : AionClientPacket
{
    private readonly GsClientConnection       _conn;
    private readonly PlayerConnectionRegistry _connRegistry;
    private readonly DuelService              _duelService;
    private readonly GameWorld                _world;
    private readonly PlayerResponseRegistry   _responseRegistry;

    private int _targetObjectId;

    public CM_DUEL_REQUEST(GsClientConnection conn, PlayerConnectionRegistry connRegistry,
        DuelService duelService, GameWorld world, PlayerResponseRegistry responseRegistry)
    {
        _conn             = conn;
        _connRegistry     = connRegistry;
        _duelService      = duelService;
        _world            = world;
        _responseRegistry = responseRegistry;
    }

    public override void Read(ref PacketReader r) => _targetObjectId = r.ReadD();

    public override async ValueTask RunAsync(CancellationToken ct)
    {
        var player = _conn.ActivePlayer;
        if (player is null || player.IsAlreadyDead) return;

        var target = _world.GetPlayerByObjectId(_targetObjectId);
        if (target is null || target.IsAlreadyDead || target.ObjectId == player.ObjectId) return;

        if (_duelService.IsDueling(player.ObjectId) || _duelService.IsDueling(target.ObjectId)) return;

        var targetConn = _connRegistry.Get(target.ObjectId);
        if (targetConn is null) return;

        var tcs = _responseRegistry.RegisterPending(target.ObjectId);
        try
        {
            await targetConn.SendAsync(new SM_QUESTION_WINDOW(50028, 0, 0, player.Name), ct);
        }
        catch
        {
            _responseRegistry.CancelPending(target.ObjectId);
            return;
        }

        bool accepted;
        try
        {
            accepted = await tcs.Task.WaitAsync(TimeSpan.FromSeconds(30), ct);
        }
        catch
        {
            _responseRegistry.CancelPending(target.ObjectId);
            return;
        }

        if (!accepted) return;

        if (_duelService.IsDueling(player.ObjectId) || _duelService.IsDueling(target.ObjectId)) return;

        _duelService.StartDuel(player.ObjectId, target.ObjectId);

        await _conn.SendAsync(SM_DUEL.Started(target.ObjectId), ct);
        try { await targetConn.SendAsync(SM_DUEL.Started(player.ObjectId), ct); } catch { }
    }
}
