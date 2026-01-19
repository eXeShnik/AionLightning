using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace AionLightning.Commons.Utils.Concurrent
{
    public class ScheduledFutureWrapper
    {
        private readonly Task _task;

        public ScheduledFutureWrapper(Task task)
        {
            _task = task;
        }

        public bool IsDone => _task.IsCompleted;

        public void Cancel(bool mayInterruptIfRunning)
        {
            // Not directly supported in Task, but can be implemented with CancellationToken
        }

        public TaskAwaiter GetAwaiter()
        {
            return _task.GetAwaiter();
        }
    }
}
