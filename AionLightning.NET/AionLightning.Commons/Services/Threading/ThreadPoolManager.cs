using AionLightning.Commons.Network;
using Microsoft.Extensions.Logging;

namespace AionLightning.Commons.Services.Threading
{
    public class ThreadPoolManager
    {
        private readonly ILogger<ThreadPoolManager> _log;
        private readonly ILogger<Executor> _executorLogger;
        private static ThreadPoolManager _instance;

        public static ThreadPoolManager Instance
        {
            get
            {
                if (_instance == null)
                    throw new InvalidOperationException("ThreadPoolManager is not initialized");
                return _instance;
            }
        }

        public ThreadPoolManager(ILogger<ThreadPoolManager> log, ILogger<Executor> executorLogger)
        {
            _log = log;
            _executorLogger = executorLogger;
            _instance = this;
        }

        public Executor GetExecutor()
        {
            return new Executor(_executorLogger);
        }

        public void Execute(IRunnable runnable)
        {
            GetExecutor().Execute(runnable);
        }
    }
}
