using AionLightning.Chat.Configs.Options;
using AionLightning.Chat.Network.Gs.ServerPackets;
using AionLightning.Commons.Network;
using Microsoft.Extensions.Logging;

namespace AionLightning.Chat.Network.Gs.ClientPackets;

public sealed class CM_CS_AUTH : AionClientPacket
{
    private readonly GsConnection _conn;
    private readonly ChatAuthOptions _auth;
    private readonly ChatNetworkOptions _net;

    private byte _gsId;
    private byte[] _defaultAddress = [];
    private string _password = string.Empty;

    public CM_CS_AUTH(GsConnection conn, ChatAuthOptions auth, ChatNetworkOptions net)
    {
        _conn = conn;
        _auth = auth;
        _net = net;
    }

    public override void Read(ref PacketReader r)
    {
        _gsId = r.ReadC();
        int addrLen = r.ReadC();
        _defaultAddress = r.ReadB(addrLen);
        _password = r.ReadS();
    }

    public override async ValueTask RunAsync(CancellationToken ct)
    {
        byte responseId;
        if (_password == _auth.Password)
        {
            _conn.GsId = _gsId;
            _conn.State = GsConnection.GsState.AUTHED;
            responseId = 0; // AUTHED
        }
        else
        {
            responseId = 1; // NOT_AUTHED
        }

        await _conn.SendAsync(new SM_GS_AUTH_RESPONSE(responseId, _net.BindAddress, _net.ClientPort), ct);
    }
}
