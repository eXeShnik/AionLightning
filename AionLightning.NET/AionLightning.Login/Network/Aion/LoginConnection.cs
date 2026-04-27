using System.Buffers;
using System.Net.Sockets;
using AionLightning.Commons.Network;
using AionLightning.Commons.Network.Ncrypt;
using AionLightning.Login.Model;
using Microsoft.Extensions.Logging;

namespace AionLightning.Login.Network.Aion;

public sealed class LoginConnection : AConnection
{
    private readonly ILogger<LoginConnection> _log;
    private readonly CryptEngine _crypt = new();
    private readonly EncryptedRSAKeyPair _rsaKeyPair;

    public int SessionId { get; }
    public LoginState State { get; set; } = LoginState.CONNECTED;
    public Account? Account { get; set; }
    public SessionKey? SessionKey { get; set; }
    public global::AionLightning.Login.GameServerInfo? GameServerInfo { get; set; }
    public EncryptedRSAKeyPair RsaKeyPair => _rsaKeyPair;
    public CryptEngine Crypt => _crypt;

    public enum LoginState { CONNECTED, AUTHED_GG, AUTHED_LOGIN, AUTHED_GS }

    public LoginConnection(Socket socket, ILogger<LoginConnection> log) : base(socket)
    {
        _log = log;
        SessionId = GetHashCode();
        _rsaKeyPair = KeyGen.GetEncryptedRSAKeyPair()!;
    }

    protected override ValueTask OnPacketAsync(ReadOnlySequence<byte> frame, CancellationToken ct)
    {
        // TODO M2: decrypt, read opcode, dispatch via AionPacketHandlerFactory
        return ValueTask.CompletedTask;
    }

    public ValueTask SendPacketAsync(AionServerPacket packet, CancellationToken ct = default)
    {
        // TODO M2: serialize, encrypt, write with length prefix
        return ValueTask.CompletedTask;
    }
}
