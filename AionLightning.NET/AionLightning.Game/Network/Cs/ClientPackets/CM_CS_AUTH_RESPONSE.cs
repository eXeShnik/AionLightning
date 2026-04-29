using AionLightning.Commons.Network;

namespace AionLightning.Game.Network.Cs.ClientPackets;

public sealed class CM_CS_AUTH_RESPONSE : AionClientPacket
{
    private readonly CsConnection _conn;
    private byte _response;
    private byte[] _ip = [];
    private int _port;

    public CM_CS_AUTH_RESPONSE(CsConnection conn) => _conn = conn;

    public override void Read(ref PacketReader r)
    {
        _response = r.ReadC();
        _ip = r.ReadB(4);
        _port = (ushort)r.ReadH();
    }

    public override ValueTask RunAsync(CancellationToken ct)
    {
        if (_response == 0)
        {
            _conn.State = CsConnection.CsState.AUTHED;
            _conn.ChatClientIp = _ip;
            _conn.ChatClientPort = _port;
        }
        return ValueTask.CompletedTask;
    }
}
