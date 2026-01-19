using System;
using System.Diagnostics;
using System.Text;
using System.Threading.Tasks;
using AionLightning.Commons.Configs;
using Microsoft.Extensions.Logging;

namespace AionLightning.Commons.Utils.Concurrent
{
    public static class ExecuteWrapper
    {
        private static ILogger _logger;
        private static CommonsConfig _config;

        public static void Configure(ILogger logger, CommonsConfig config)
        {
            _logger = logger;
            _config = config;
        }

        public static void Execute(Action runnable)
        {
            Execute(runnable, long.MaxValue);
        }

        public static void Execute(Action runnable, long maximumRuntimeInMillisecWithoutWarning)
        {
            var stopwatch = Stopwatch.StartNew();

            try
            {
                runnable();
            }
            catch (Exception t)
            {
                _logger?.LogWarning(t, "Exception in a Runnable execution:");
            }
            finally
            {
                stopwatch.Stop();
                var runtimeInNanosec = stopwatch.Elapsed.Ticks * 100;
                var clazz = runnable.Target.GetType();

                if (_config?.RunnableStatsEnable ?? false)
                {
                    RunnableStatsManager.AddRunnableStats(clazz, runtimeInNanosec);
                }

                var runtimeInMillisec = stopwatch.ElapsedMilliseconds;
                if (runtimeInMillisec > maximumRuntimeInMillisecWithoutWarning)
                {
                    var sb = new StringBuilder();
                    sb.Append(clazz);
                    sb.Append(" - execution time: ");
                    sb.Append(runtimeInMillisec);
                    sb.Append("msec");
                    _logger?.LogWarning(sb.ToString());
                }
            }
        }
    }
}
