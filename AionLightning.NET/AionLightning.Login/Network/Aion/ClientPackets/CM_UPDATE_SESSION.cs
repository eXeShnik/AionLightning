using System.Buffers;
using AionLightning.Login.Controller;
using Microsoft.Extensions.Logging;

namespace AionLightning.Login.Network.Aion.ClientPackets;

public class CM_UPDATE_SESSION : AionClientPacket
{
    private int _accountId;
    private int _loginOk;
    private int _reconnectKey;

    public CM_UPDATE_SESSION(ILogger<CM_UPDATE_SESSION> logger, ReadOnlySequence<byte> buffer, LoginConnection client) : base(logger, buffer, client)
    {
    }

    protected override void ReadImpl()
    {
        var reader = new SequenceReader<byte>(_buffer);
        reader.TryReadLittleEndian(out _accountId);
        reader.TryReadLittleEndian(out _loginOk);
        reader.TryReadLittleEndian(out _reconnectKey);
    }

    protected override void RunImpl()
    {
        // AccountController.AuthReconnectingAccount(_accountId, _loginOk, _reconnectKey, Connection);
    }
}