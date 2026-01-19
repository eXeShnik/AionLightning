using System;
using System.Threading;
using Microsoft.Extensions.Logging;

namespace AionLightning.Commons.Network.Util
{
    public class ThreadUncaughtExceptionHandler
    {
        private readonly ILogger<ThreadUncaughtExceptionHandler> _log;

        public ThreadUncaughtExceptionHandler(ILogger<ThreadUncaughtExceptionHandler> log)
        {
            _log = log;
        }

        public void UncaughtException(object sender, UnhandledExceptionEventArgs e)
        {
            _log.LogError((Exception)e.ExceptionObject, "Uncaught exception");
        }
    }
}
