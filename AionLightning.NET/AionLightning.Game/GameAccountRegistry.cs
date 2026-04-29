using System.Collections.Concurrent;

namespace AionLightning.Game;

public sealed class GameAccountRegistry
{
    private readonly ConcurrentDictionary<int, TaskCompletionSource<bool>> _pending = new();

    public TaskCompletionSource<bool> RegisterPending(int accountId)
    {
        var tcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        _pending[accountId] = tcs;
        return tcs;
    }

    public void CompleteAuth(int accountId, bool ok)
    {
        if (_pending.TryRemove(accountId, out var tcs))
            tcs.TrySetResult(ok);
    }

    public void CancelPending(int accountId)
    {
        if (_pending.TryRemove(accountId, out var tcs))
            tcs.TrySetCanceled();
    }
}
