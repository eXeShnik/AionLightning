using AionLightning.Commons.Network;
using AionLightning.Game.Services;

namespace AionLightning.Game.Network.Aion.ClientPackets;

public sealed class CM_ENTER_WORLD : AionClientPacket
{
    private readonly GsClientConnection       _conn;
    private readonly PlayerEnterWorldService  _enterWorldService;
    private int _objectId;

    public CM_ENTER_WORLD(GsClientConnection conn, PlayerEnterWorldService enterWorldService)
    {
        _conn              = conn;
        _enterWorldService = enterWorldService;
    }

    public override void Read(ref PacketReader r) => _objectId = r.ReadD();

    public override ValueTask RunAsync(CancellationToken ct) =>
        _enterWorldService.EnterWorldAsync(_conn, _objectId, ct);
}
