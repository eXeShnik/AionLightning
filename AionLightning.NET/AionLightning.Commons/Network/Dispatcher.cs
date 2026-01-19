using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace AionLightning.Commons.Network
{
    public abstract class Dispatcher : IDispatcher
    {
        private readonly ILogger<Dispatcher> _log;
        private readonly Executor _dcPool;
        private readonly object _gate = new object();

        protected Dispatcher(string name, Executor dcPool, ILogger<Dispatcher> log)
        {
            _log = log;
            _dcPool = dcPool;
            Name = name;
        }

        public string Name { get; }

        public abstract void CloseConnection(AConnection con);

        public abstract void Dispatch();

        public void Shutdown()
        {
            // TODO: implement
        }

        public void Run()
        {
            while (true)
            {
                try
                {
                    Dispatch();
                    Thread.Sleep(1);
                }
                catch (Exception e)
                {
                    _log.LogError("Error in dispatcher loop", e);
                }
            }
        }

        public void Register(Socket socket, IConnectionFactory factory)
        {
            lock (_gate)
            {
                var connection = factory.Create(socket, this);
                Register(connection);
            }
        }

        protected abstract void Register(AConnection connection);
    }

    public class Executor
    {
        private readonly Thread[] _threads;
        private readonly Queue<IRunnable> _queue = new Queue<IRunnable>();

        public Executor(int threadCount)
        {
            _threads = new Thread[threadCount];
            for (int i = 0; i < threadCount; i++)
            {
                _threads[i] = new Thread(Run);
                _threads[i].Start();
            }
        }

        public void Execute(IRunnable runnable)
        {
            lock (_queue)
            {
                _queue.Enqueue(runnable);
                Monitor.Pulse(_queue);
            }
        }

        private void Run()
        {
            while (true)
            {
                IRunnable runnable;
                lock (_queue)
                {
                    while (_queue.Count == 0)
                    {
                        Monitor.Wait(_queue);
                    }
                    runnable = _queue.Dequeue();
                }
                runnable.Run();
            }
        }
    }

    public interface IRunnable
    {
        void Run();
    }
}
