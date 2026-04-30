using AionLightning.Commons.Network;
using AionLightning.Game.Network.Aion.ServerPackets;

namespace AionLightning.Game.Network.Aion.ClientPackets;

/// <summary>Client issues a /roll command. Broadcasts dice-roll result to zone peers. Opcode 0x109.</summary>
public sealed class CM_CLIENT_COMMAND_ROLL : AionClientPacket
{
    private readonly GsClientConnection       _conn;
    private readonly PlayerConnectionRegistry _connRegistry;

    private int _maxRoll;

    public CM_CLIENT_COMMAND_ROLL(GsClientConnection conn, PlayerConnectionRegistry connRegistry)
    {
        _conn         = conn;
        _connRegistry = connRegistry;
    }

    public override void Read(ref PacketReader r) => _maxRoll = r.ReadD();

    public override async ValueTask RunAsync(CancellationToken ct)
    {
        var player = _conn.ActivePlayer;
        if (player is null) return;

        if (_maxRoll < 2) _maxRoll = 2;

        int roll    = Random.Shared.Next(1, _maxRoll + 1);
        int worldId = player.Position.WorldId;

        await _conn.SendAsync(SM_SYSTEM_MESSAGE.RollSelf(roll, _maxRoll), ct);

        var broadcast = SM_SYSTEM_MESSAGE.RollOther(player.Name, roll, _maxRoll);
        foreach (var other in _connRegistry.GetAllExcept(player.ObjectId))
            if (other.ActivePlayer?.Position.WorldId == worldId)
                try { await other.SendAsync(broadcast, ct); } catch { }
    }
}
