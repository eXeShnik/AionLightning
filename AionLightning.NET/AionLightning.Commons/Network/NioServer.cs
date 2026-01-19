using System.Collections.Generic;
using System.Threading;
using AionLightning.Commons.Network.Util;
using Microsoft.Extensions.Logging;

namespace AionLightning.Commons.Network
{
    public class NioServer
    {
        private readonly ILogger<NioServer> _log;
        private readonly ILogger<AcceptDispatcherImpl> _acceptDispatcherLogger;
        private readonly ILogger<Dispatcher> _dispatcherLogger;
        private readonly ILogger<AcceptReadWriteDispatcherImpl> _acceptReadWriteDispatcherLogger;

        private readonly List<AcceptDispatcherImpl> _acceptDispatchers = new List<AcceptDispatcherImpl>();
        private readonly List<Dispatcher> _readWriteDispatchers = new List<Dispatcher>();
        private int _currentReadWriteDispatcher;
        private readonly Executor _dcPool;
        private readonly int _readWriteThreads;
        private readonly ServerCfg[] _cfgs;

        public NioServer(int readWriteThreads, ILogger<NioServer> log, ILogger<AcceptDispatcherImpl> acceptDispatcherLogger, ILogger<Dispatcher> dispatcherLogger, ILogger<AcceptReadWriteDispatcherImpl> acceptReadWriteDispatcherLogger, params ServerCfg[] cfgs)
        {
            _log = log;
            _acceptDispatcherLogger = acceptDispatcherLogger;
            _dispatcherLogger = dispatcherLogger;
            _acceptReadWriteDispatcherLogger = acceptReadWriteDispatcherLogger;
            _dcPool = new Executor(readWriteThreads);
            _readWriteThreads = readWriteThreads;
            _cfgs = cfgs;
        }

        public void Connect()
        {
            try
            {
                InitDispatchers(_readWriteThreads, _dcPool);

                foreach (var cfg in _cfgs)
                {
                    var acceptor = new Acceptor(cfg.Factory, this);
                    var acceptDispatcher = new AcceptDispatcherImpl($"Accept-{cfg.ConnectionName}", acceptor, _acceptDispatcherLogger, _dispatcherLogger);
                    acceptDispatcher.Bind(cfg.HostName, cfg.Port);
                    new Thread(acceptDispatcher.Run).Start();
                    _acceptDispatchers.Add(acceptDispatcher);
                }
            }
            catch (System.Exception e)
            {
                _log.LogError(e, "NioServer Initialization Error: " + e);
                throw new System.Exception("NioServer Initialization Error!");
            }
        }

        public Dispatcher GetReadWriteDispatcher()
        {
            if (_readWriteDispatchers.Count == 0)
                return null;

            if (_readWriteDispatchers.Count == 1)
                return _readWriteDispatchers[0];

            if (_currentReadWriteDispatcher >= _readWriteDispatchers.Count)
                _currentReadWriteDispatcher = 0;
            return _readWriteDispatchers[_currentReadWriteDispatcher++];
        }

        private void InitDispatchers(int readWriteThreads, Executor dcPool)
        {
            if (readWriteThreads < 1)
            {
                var dispatcher = new AcceptReadWriteDispatcherImpl("AcceptReadWrite Dispatcher", dcPool, _acceptReadWriteDispatcherLogger, _dispatcherLogger);
                new Thread(dispatcher.Run).Start();
                _readWriteDispatchers.Add(dispatcher);
            }
            else
            {
                for (int i = 0; i < readWriteThreads; i++)
                {
                    var dispatcher = new AcceptReadWriteDispatcherImpl($"ReadWrite-{i} Dispatcher", dcPool, _acceptReadWriteDispatcherLogger, _dispatcherLogger);
                    new Thread(dispatcher.Run).Start();
                    _readWriteDispatchers.Add(dispatcher);
                }
            }
        }

        public int GetActiveConnections()
        {
            int count = 0;
            foreach (var d in _readWriteDispatchers)
                count += ((AcceptReadWriteDispatcherImpl)d).GetConnections().Count;
            return count;
        }

        public void Shutdown()
        {
            foreach (var d in _acceptDispatchers)
                d.Shutdown();
            foreach (var d in _readWriteDispatchers)
                d.Shutdown();
        }
    }
}
