using System;
using System.Threading;

namespace AionLightning.Commons.Utils.Concurrent
{
    public class PriorityThreadFactory
    {
        private readonly string _name;
        private readonly ThreadPriority _priority;
        private int _threadNumber = 1;

        public PriorityThreadFactory(string name, ThreadPriority priority)
        {
            _name = name;
            _priority = priority;
        }

        public Thread NewThread(Action runnable)
        {
            var t = new Thread(() => runnable())
            {
                Name = _name + "-" + _threadNumber++,
                Priority = _priority
            };
            // In .NET, uncaught exceptions on threads will terminate the process by default on .NET Framework 2.0+
            // AppDomain.CurrentDomain.UnhandledException can be used to log them.
            return t;
        }
    }
}
