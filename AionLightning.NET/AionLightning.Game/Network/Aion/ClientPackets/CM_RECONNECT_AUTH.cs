using AionLightning.Commons.Network;
using AionLightning.Game.Network.Aion.ServerPackets;
using AionLightning.Game.Network.Ls;
using AionLightning.Game.Network.Ls.ServerPackets;

namespace AionLightning.Game.Network.Aion.ClientPackets;

/// <summary>
/// Client requests fast reconnect back to LoginServer. Opcode 0x195.
/// GS asks LS for a one-time reconnect key; on success, sends SM_RECONNECT_KEY
/// to the Aion client and closes the GS connection.
/// If LS is unreachable the connection is closed without a key.
/// </summary>
public sealed class CM_RECONNECT_AUTH : AionClientPacket
{
    private readonly GsClientConnection _conn;
    private readonly LsConnectionHolder  _ls;
    private readonly ReconnectRegistry   _reconnectRegistry;

    public CM_RECONNECT_AUTH(GsClientConnection conn, LsConnectionHolder ls,
        ReconnectRegistry reconnectRegistry)
    {
        _conn              = conn;
        _ls                = ls;
        _reconnectRegistry = reconnectRegistry;
    }

    public override void Read(ref PacketReader r) { }

    public override async ValueTask RunAsync(CancellationToken ct)
    {
        var lsConn = _ls.Current;
        if (lsConn is null)
        {
            await _conn.DisposeAsync();
            return;
        }

        var tcs = _reconnectRegistry.RegisterPending(_conn.AccountId, _conn);

        await lsConn.SendAsync(new SM_ACCOUNT_RECONNECT_KEY(_conn.AccountId), ct);

        int key;
        try
        {
            key = await tcs.Task.WaitAsync(TimeSpan.FromSeconds(10), ct);
        }
        catch
        {
            _reconnectRegistry.CancelPending(_conn.AccountId);
            await _conn.DisposeAsync();
            return;
        }

        try { await _conn.SendAsync(new SM_RECONNECT_KEY(key), ct); } catch { }
        await _conn.DisposeAsync();
    }
}
