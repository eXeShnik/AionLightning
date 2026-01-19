using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Threading;
using Microsoft.Extensions.Logging;

namespace AionLightning.Commons.Network
{
    public class AcceptReadWriteDispatcherImpl : Dispatcher
    {
        private readonly ILogger<AcceptReadWriteDispatcherImpl> _log;
        private readonly List<AConnection> _pendingClose = new List<AConnection>();
        private readonly List<AConnection> _connections = new List<AConnection>();

        public AcceptReadWriteDispatcherImpl(string name, Executor dcPool, ILogger<AcceptReadWriteDispatcherImpl> log, ILogger<Dispatcher> dispatcherLog) : base(name, dcPool, dispatcherLog)
        {
            _log = log;
        }

        public override void Dispatch()
        {
            ProcessPendingClose();

            lock (_connections)
            {
                foreach (var connection in _connections)
                {
                    if (connection.IsPendingClose())
                    {
                        _pendingClose.Add(connection);
                        continue;
                    }

                    try
                    {
                        Process(connection);
                    }
                    catch (Exception e)
                    {
                        _log.LogError(e, "Error in connection loop");
                        _pendingClose.Add(connection);
                    }
                }
            }
        }

        private void Process(AConnection connection)
        {
            if (connection.IsPendingClose())
            {
                CloseConnection(connection);
                return;
            }

            ByteBuffer r;
            while ((r = connection.Read()) != null)
            {
                if (!connection.Process(r))
                {
                    CloseConnection(connection);
                    return;
                }
            }

            ByteBuffer w;
            while ((w = connection.Write()) != null)
            {
                // This is not going to work as intended without a proper implementation
            }
        }

        private void ProcessPendingClose()
        {
            lock (_pendingClose)
            {
                foreach (var connection in _pendingClose)
                {
                    CloseConnection(connection);
                }
                _pendingClose.Clear();
            }
        }

        protected override void Register(AConnection connection)
        {
            lock (_connections)
            {
                _connections.Add(connection);
            }
        }

        public override void CloseConnection(AConnection connection)
        {
            lock (_pendingClose)
            {
                _pendingClose.Add(connection);
            }
        }

        public List<AConnection> GetConnections()
        {
            return new List<AConnection>(_connections);
        }
    }
}