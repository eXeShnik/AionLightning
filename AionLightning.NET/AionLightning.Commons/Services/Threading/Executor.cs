using Microsoft.Extensions.Logging;

namespace AionLightning.Commons.Services.Threading
{
    public class Executor
    {
        private readonly ILogger<Executor> _log;

        public Executor(ILogger<Executor> log)
        {
            _log = log;
        }

        public void Execute(IRunnable runnable)
        {
            runnable.Run();
        }
    }
}
