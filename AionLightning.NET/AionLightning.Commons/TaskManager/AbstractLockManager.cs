namespace AionLightning.Commons.TaskManager;

/// <summary>
/// Base class providing read/write lock primitives.
/// Java equivalent: com.aionemu.commons.taskmanager.AbstractLockManager
/// </summary>
public abstract class AbstractLockManager : IDisposable
{
    private readonly ReaderWriterLockSlim _lock = new(LockRecursionPolicy.SupportsRecursion);

    public void WriteLock()   => _lock.EnterWriteLock();
    public void WriteUnlock() => _lock.ExitWriteLock();
    public void ReadLock()    => _lock.EnterReadLock();
    public void ReadUnlock()  => _lock.ExitReadLock();

    public void Dispose() => _lock.Dispose();
}
