using System.Buffers;
using System.Net.Sockets;
using AionLightning.Commons.Network;
using AionLightning.Login;
using Microsoft.Extensions.Logging;

namespace AionLightning.Login.Network.GameServer;

public sealed class GsConnection : AConnection
{
    private readonly ILogger<GsConnection> _log;

    public GsState State { get; set; } = GsState.CONNECTED;
    public GameServerInfo? GameServerInfo { get; set; }

    public enum GsState { CONNECTED, AUTHED }

    public GsConnection(Socket socket, ILogger<GsConnection> log) : base(socket)
    {
        _log = log;
    }

    protected override ValueTask OnPacketAsync(ReadOnlySequence<byte> frame, CancellationToken ct)
    {
        // TODO M2: read opcode, dispatch via GsPacketHandlerFactory
        return ValueTask.CompletedTask;
    }

    public ValueTask SendPacketAsync(AionServerPacket packet, CancellationToken ct = default)
    {
        // TODO M2: serialize, write with length prefix
        return ValueTask.CompletedTask;
    }
}
