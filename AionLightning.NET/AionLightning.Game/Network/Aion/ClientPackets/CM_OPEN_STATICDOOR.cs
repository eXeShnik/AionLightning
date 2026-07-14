using AionLightning.Commons.Network;
using AionLightning.Game.Services;

namespace AionLightning.Game.Network.Aion.ClientPackets;

/// <summary>Client requests opening a static door. Opcode 0xF5. Java CM_OPEN_STATICDOOR →
/// StaticDoorService.openStaticDoor (this port: DoorService.TryOpenDoor).</summary>
public sealed class CM_OPEN_STATICDOOR : AionClientPacket
{
    private readonly GsClientConnection _conn;
    private readonly DoorService _doorService;

    /// <summary>Java reads this as "doorId" — it is the door's map-design/static id (Java
    /// <c>getSpawn().getStaticId()</c>), not an AION object id.</summary>
    private int _staticId;

    public CM_OPEN_STATICDOOR(GsClientConnection conn, DoorService doorService)
    {
        _conn        = conn;
        _doorService = doorService;
    }

    public override void Read(ref PacketReader r) => _staticId = r.ReadD();

    public override ValueTask RunAsync(CancellationToken ct)
    {
        if (_conn.ActivePlayer is { } player)
            _doorService.TryOpenDoor(player, _staticId);
        return ValueTask.CompletedTask;
    }
}
