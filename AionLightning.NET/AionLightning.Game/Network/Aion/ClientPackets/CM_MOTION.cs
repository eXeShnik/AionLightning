using AionLightning.Commons.Network;
using AionLightning.Game.Dao;
using AionLightning.Game.Network.Aion.ServerPackets;

namespace AionLightning.Game.Network.Aion.ClientPackets;

/// <summary>
/// Client activates a motion in a combat-animation slot. Opcode 0x2E5.
/// Updates the player's active motion slot and broadcasts action=5 to zone peers.
/// </summary>
public sealed class CM_MOTION : AionClientPacket
{
    private readonly GsClientConnection       _conn;
    private readonly PlayerConnectionRegistry _connRegistry;
    private readonly IMotionDao               _motionDao;

    private short _motionId;
    private byte  _motionSlot;

    public CM_MOTION(GsClientConnection conn, PlayerConnectionRegistry connRegistry, IMotionDao motionDao)
    {
        _conn         = conn;
        _connRegistry = connRegistry;
        _motionDao    = motionDao;
    }

    public override void Read(ref PacketReader r)
    {
        r.ReadC();           // unk
        _motionId   = r.ReadH();
        _motionSlot = (byte)r.ReadC();
    }

    public override async ValueTask RunAsync(CancellationToken ct)
    {
        var player = _conn.ActivePlayer;
        if (player is null) return;

        if (_motionSlot is < 1 or > 5) return;

        player.ActiveMotions[_motionSlot] = _motionId;
        await _motionDao.UpsertAsync(player.ObjectId, _motionSlot, _motionId, ct);

        var packet  = SM_MOTION.SetSlot(_motionId, _motionSlot);
        int worldId = player.Position.WorldId;

        try { await _conn.SendAsync(packet, ct); } catch { }
        foreach (var other in _connRegistry.GetAllExcept(player.ObjectId))
            if (other.ActivePlayer?.Position.WorldId == worldId)
                try { await other.SendAsync(packet, ct); } catch { }
    }
}
