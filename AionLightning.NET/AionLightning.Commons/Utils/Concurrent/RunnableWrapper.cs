using System;

namespace AionLightning.Commons.Utils.Concurrent
{
    public class RunnableWrapper
    {
        private readonly Action _runnable;
        private readonly long _maxRuntimeMsWithoutWarning;

        public RunnableWrapper(Action runnable) : this(runnable, long.MaxValue)
        {
        }

        public RunnableWrapper(Action runnable, long maxRuntimeMsWithoutWarning)
        {
            _runnable = runnable;
            _maxRuntimeMsWithoutWarning = maxRuntimeMsWithoutWarning;
        }

        public void Run()
        {
            ExecuteWrapper.Execute(_runnable, _maxRuntimeMsWithoutWarning);
        }
    }
}
