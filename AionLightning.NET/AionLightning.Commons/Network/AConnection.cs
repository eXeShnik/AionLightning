using System;
using System.Net.Sockets;
using AionLightning.Commons.Network.Packet;

namespace AionLightning.Commons.Network
{
    public abstract class AConnection
    {
        private readonly Socket _socket;
        private readonly IDispatcher _dispatcher;
        protected bool _pendingClose;
        protected bool _closed;
        protected string _ip;
        protected int _port;
        protected readonly object _guard = new object();
        protected bool _isForcedClosing;
        protected bool _locked;

        public AConnection(Socket socket)
        {
            _socket = socket;
        }

        public abstract bool Process(ByteBuffer data);
        public abstract ByteBuffer Read();
        public abstract ByteBuffer Write();

        public void OnDisconnect()
        {
            // Implementation needed
        }

        public void Initialized()
        {
            // Implementation needed
        }

        public bool IsPendingClose()
        {
            return _pendingClose;
        }

        public void SendPacket(BaseServerPacket packet)
        {
            // TODO: implement
        }

        protected void EnableWriteInterest()
        {
            // This will be handled by the dispatcher
        }

        internal IDispatcher GetDispatcher()
        {
            return _dispatcher;
        }

        public Socket GetSocket()
        {
            return _socket;
        }

        public void Close(bool forced)
        {
            lock (_guard)
            {
                if (IsWriteDisabled())
                    return;

                _isForcedClosing = forced;
                //getDispatcher().closeConnection(this);
            }
        }

        internal bool OnlyClose()
        {
            lock (_guard)
            {
                if (_closed)
                    return false;
                try
                {
                    if (_socket.Connected)
                    {
                        _socket.Close();
                    }
                    _closed = true;
                }
                catch (Exception)
                {
                }
            }
            return true;
        }

        protected bool IsWriteDisabled()
        {
            return _pendingClose || _closed;
        }

        public string GetIP()
        {
            return _ip;
        }

        internal bool TryLockConnection()
        {
            if (_locked)
                return false;
            return _locked = true;
        }

        internal void UnlockConnection()
        {
            _locked = false;
        }

        protected abstract bool ProcessData(byte[] data);
        protected abstract bool WriteData(byte[] data);
        protected abstract long GetDisconnectionDelay();
        protected abstract void OnServerClose();
    }
}
