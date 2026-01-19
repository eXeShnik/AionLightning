using System;
using System.Collections.Concurrent;
using System.Threading;
using AionLightning.Commons.Utils;

namespace AionLightning.Commons.Network.Util
{
    public class ThreadPoolManager
    {
        private static readonly ThreadPoolManager _instance = new ThreadPoolManager();
        private readonly BlockingCollection<IRunnable> _waitingQueue = new BlockingCollection<IRunnable>();

        public static ThreadPoolManager Instance => _instance;

        private ThreadPoolManager()
        {
            // Start a number of threads to process the queue
            for (int i = 0; i < Environment.ProcessorCount; i++)
            {
                var thread = new Thread(ProcessQueue);
                thread.Start();
            }
        }

        public void Execute(IRunnable runnable)
        {
            _waitingQueue.Add(runnable);
        }

        private void ProcessQueue()
        {
            while (true)
            {
                var runnable = _waitingQueue.Take();
                runnable.Run();
            }
        }
    }
}
