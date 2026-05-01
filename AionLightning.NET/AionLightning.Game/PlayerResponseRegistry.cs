using System.Collections.Concurrent;

namespace AionLightning.Game;

/// <summary>
/// Maps a responding player's objectId to a pending TCS waiting for their yes/no dialog answer.
/// Used by SM_QUESTION_WINDOW / CM_QUESTION_RESPONSE round-trips (group invite, duel).
/// </summary>
public sealed class PlayerResponseRegistry
{
    private readonly ConcurrentDictionary<int, TaskCompletionSource<bool>> _pending = new();

    public TaskCompletionSource<bool> RegisterPending(int targetObjectId)
    {
        var tcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        _pending[targetObjectId] = tcs;
        return tcs;
    }

    public void Respond(int targetObjectId, bool accepted)
    {
        if (_pending.TryRemove(targetObjectId, out var tcs))
            tcs.TrySetResult(accepted);
    }

    public void CancelPending(int targetObjectId)
    {
        if (_pending.TryRemove(targetObjectId, out var tcs))
            tcs.TrySetCanceled();
    }
}
