using AionLightning.Commons.Network;
using AionLightning.Game.Model;
using AionLightning.Game.Network.Aion.ServerPackets;

namespace AionLightning.Game.Network.Aion.ClientPackets;

/// <summary>Client requests opening a static door object. Opcode 0xF5.</summary>
public sealed class CM_OPEN_STATICDOOR : AionClientPacket
{
    private readonly GsClientConnection _conn;
    private readonly PlayerConnectionRegistry _connRegistry;

    private int _doorObjectId;

    public CM_OPEN_STATICDOOR(GsClientConnection conn, PlayerConnectionRegistry connRegistry)
    {
        _conn         = conn;
        _connRegistry = connRegistry;
    }

    public override void Read(ref PacketReader r) => _doorObjectId = r.ReadD();

    public override async ValueTask RunAsync(CancellationToken ct)
    {
        if (_conn.ActivePlayer is null) return;

        var packet  = new SM_EMOTION(_doorObjectId, EmotionType.OPEN_DOOR, state: 0);
        int worldId = _conn.ActivePlayer.Position.WorldId;
        await _conn.SendAsync(packet, ct);
        foreach (var other in _connRegistry.GetAllExcept(_conn.ActivePlayer.ObjectId))
            if (other.ActivePlayer?.Position.WorldId == worldId)
                await other.SendAsync(packet, ct);
    }
}
