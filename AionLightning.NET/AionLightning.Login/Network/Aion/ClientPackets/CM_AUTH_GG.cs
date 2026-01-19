using System.Buffers;
using AionLightning.Login.Network.Aion.ServerPackets;
using Microsoft.Extensions.Logging;

namespace AionLightning.Login.Network.Aion.ClientPackets;

public class CM_AUTH_GG : AionClientPacket
{
    private int _sessionId;

    public CM_AUTH_GG(ILogger<CM_AUTH_GG> logger, ReadOnlySequence<byte> buffer, LoginConnection client) : base(logger, buffer, client)
    {
    }

    protected override void ReadImpl()
    {
        var reader = new SequenceReader<byte>(_buffer);
        reader.TryReadLittleEndian(out _sessionId);
    }

    protected override void RunImpl()
    {
        if (Connection.SessionId == _sessionId)
        {
            Connection.State = LoginConnection.LoginState.AUTHED_GG;
            Connection.SendPacket(new SM_AUTH_GG(_sessionId));
        }
        else
        {
            _logger.LogWarning("Auth GG failed, session id mismatch. Expected: {ExpectedSessionId}, received: {ReceivedSessionId}", Connection.SessionId, _sessionId);
            Connection.Close(new SM_LOGIN_FAIL(AionAuthResponse.SYSTEM_ERROR), false);
        }
    }
}