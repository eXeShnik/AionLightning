using AionLightning.Commons.Network;

namespace AionLightning.Game.Network.Ls.ClientPackets;

/// <summary>
/// LS→GS response to SM_ACCOUNT_RECONNECT_KEY. Opcode 0x03.
/// Delivers the reconnect key the Aion client must present to LS on reconnect.
/// </summary>
public sealed class CM_ACCOUNT_RECONNECT_KEY : LsClientPacket
{
    private readonly ReconnectRegistry _registry;

    private int _accountId;
    private int _reconnectKey;

    public CM_ACCOUNT_RECONNECT_KEY(ReconnectRegistry registry)
        => _registry = registry;

    public override void Read(ref PacketReader r)
    {
        _accountId    = r.ReadD();
        _reconnectKey = r.ReadD();
    }

    public override ValueTask RunAsync(CancellationToken ct)
    {
        _registry.CompleteReconnect(_accountId, _reconnectKey);
        return ValueTask.CompletedTask;
    }
}
