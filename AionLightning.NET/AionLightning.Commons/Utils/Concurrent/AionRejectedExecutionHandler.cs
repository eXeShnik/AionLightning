using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace AionLightning.Commons.Utils.Concurrent
{
    public class AionRejectedExecutionHandler
    {
        private readonly ILogger _logger;

        public AionRejectedExecutionHandler(ILogger logger)
        {
            _logger = logger;
        }

        public void RejectedExecution(Action r, ThreadPoolExecutor executor)
        {
            if (executor.IsShutdown)
                return;

            _logger.LogWarning($"{r} from {executor}", new TaskSchedulerException());

            if (Thread.CurrentThread.Priority > ThreadPriority.Normal)
                new Thread(() => r()).Start();
            else
                r();
        }
    }

    public class ThreadPoolExecutor
    {
        public bool IsShutdown { get; set; }
    }
}
