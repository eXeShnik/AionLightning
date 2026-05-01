using System.Collections.Concurrent;
using AionLightning.Game.Network.Aion;

namespace AionLightning.Game;

/// <summary>
/// Tracks in-flight reconnect requests: accountId → connection waiting for LS reconnect key.
/// </summary>
public sealed class ReconnectRegistry
{
    private readonly ConcurrentDictionary<int, (GsClientConnection Conn, TaskCompletionSource<int> Tcs)> _pending = new();

    public TaskCompletionSource<int> RegisterPending(int accountId, GsClientConnection conn)
    {
        var tcs = new TaskCompletionSource<int>(TaskCreationOptions.RunContinuationsAsynchronously);
        _pending[accountId] = (conn, tcs);
        return tcs;
    }

    public void CompleteReconnect(int accountId, int reconnectKey)
    {
        if (_pending.TryRemove(accountId, out var entry))
            entry.Tcs.TrySetResult(reconnectKey);
    }

    public void CancelPending(int accountId)
    {
        if (_pending.TryRemove(accountId, out var entry))
            entry.Tcs.TrySetCanceled();
    }
}
