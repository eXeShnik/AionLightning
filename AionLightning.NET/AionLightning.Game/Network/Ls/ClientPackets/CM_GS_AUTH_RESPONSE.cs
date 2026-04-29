using AionLightning.Commons.Network;
using AionLightning.Game.Network.Ls.ServerPackets;
using Microsoft.Extensions.Logging;

namespace AionLightning.Game.Network.Ls.ClientPackets;

public sealed class CM_GS_AUTH_RESPONSE : LsClientPacket
{
    private readonly LsConnection _conn;
    private byte _response;
    private byte _serverCount;

    public CM_GS_AUTH_RESPONSE(LsConnection conn)
    {
        _conn = conn;
    }

    public override void Read(ref PacketReader r)
    {
        _response = r.ReadC();
        if (_response == 0)
            _serverCount = r.ReadC();
    }

    public override async ValueTask RunAsync(CancellationToken ct)
    {
        switch (_response)
        {
            case 0: // AUTHED
                _conn.State = LsConnection.LsState.AUTHED;
                // Send current accounts online list (empty for M3)
                break;
            case 1: // NOT_AUTHED
                // Shutdown — wrong password / not registered
                break;
            case 2: // ALREADY_REGISTERED
                await Task.Delay(10_000, ct);
                await _conn.SendAsync(new SM_GS_AUTH(_conn.Info), ct);
                break;
        }
    }
}
