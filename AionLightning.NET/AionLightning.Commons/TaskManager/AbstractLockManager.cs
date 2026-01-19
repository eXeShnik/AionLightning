using System.Threading;

namespace AionLightning.Commons.TaskManager
{
    public abstract class AbstractLockManager
    {
        private readonly ReaderWriterLockSlim _lock = new ReaderWriterLockSlim();

        public void WriteLock()
        {
            _lock.EnterWriteLock();
        }

        public void WriteUnlock()
        {
            _lock.ExitWriteLock();
        }

        public void ReadLock()
        {
            _lock.EnterReadLock();
        }

        public void ReadUnlock()
        {
            _lock.ExitReadLock();
        }
    }
}
