using System;
using System.IO;
using System.Text;
using Microsoft.Extensions.Logging;
using AionLightning.Commons.Utils;

namespace AionLightning.Commons.Network.Packet
{
    public abstract class BaseClientPacket : AionPacket, IRunnable
    {
        protected readonly ILogger<BaseClientPacket> _log;
        private MemoryStream _stream;
        private BinaryReader _reader;
        private AConnection _client;

        protected BaseClientPacket(int opcode, ILogger<BaseClientPacket> log) : base(PacketType.CLIENT, opcode)
        {
            _log = log;
        }

        public void SetBuffer(byte[] buf)
        {
            _stream = new MemoryStream(buf);
            _reader = new BinaryReader(_stream);
        }

        public void SetClient(AConnection client)
        {
            _client = client;
        }

        public AConnection GetClient()
        {
            return _client;
        }

        public void Run()
        {
            RunImpl();
        }

        protected abstract void RunImpl();

        public bool Read()
        {
            try
            {
                ReadImpl();

                if (GetRemainingBytes() > 0)
                    _log.LogDebug($"Packet {this} not fully read!");

                return true;
            }
            catch (Exception re)
            {
                _log.LogError($"Reading failed for packet {this}", re);
                return false;
            }
        }

        protected abstract void ReadImpl();

        public long GetRemainingBytes()
        {
            return _stream.Length - _stream.Position;
        }

        protected int ReadD()
        {
            try
            {
                return _reader.ReadInt32();
            }
            catch (Exception e)
            {
                _log.LogError($"Missing D for: {this}", e);
            }
            return 0;
        }

        protected int ReadC()
        {
            try
            {
                return _reader.ReadByte();
            }
            catch (Exception e)
            {
                _log.LogError($"Missing C for: {this}", e);
            }
            return 0;
        }

        protected sbyte ReadSC()
        {
            try
            {
                return _reader.ReadSByte();
            }
            catch (Exception e)
            {
                _log.LogError($"Missing C for: {this}", e);
            }
            return 0;
        }

        protected short ReadSH()
        {
            try
            {
                return _reader.ReadInt16();
            }
            catch (Exception e)
            {
                _log.LogError($"Missing H for: {this}", e);
            }
            return 0;
        }

        protected int ReadH()
        {
            try
            {
                return _reader.ReadUInt16();
            }
            catch (Exception e)
            {
                _log.LogError($"Missing H for: {this}", e);
            }
            return 0;
        }

        protected double ReadDF()
        {
            try
            {
                return _reader.ReadDouble();
            }
            catch (Exception e)
            {
                _log.LogError($"Missing DF for: {this}", e);
            }
            return 0;
        }

        protected float ReadF()
        {
            try
            {
                return _reader.ReadSingle();
            }
            catch (Exception e)
            {
                _log.LogError($"Missing F for: {this}", e);
            }
            return 0;
        }

        protected long ReadQ()
        {
            try
            {
                return _reader.ReadInt64();
            }
            catch (Exception e)
            {
                _log.LogError($"Missing Q for: {this}", e);
            }
            return 0;
        }

        protected string ReadS()
        {
            var sb = new StringBuilder();
            try
            {
                while (true)
                {
                    var ch = _reader.ReadChar();
                    if (ch == 0)
                        break;
                    sb.Append(ch);
                }
            }
            catch (Exception e)
            {
                _log.LogError($"Missing S for: {this}", e);
            }
            return sb.ToString();
        }

        protected byte[] ReadB(int length)
        {
            var result = new byte[length];
            try
            {
                _reader.Read(result, 0, length);
            }
            catch (Exception e)
            {
                _log.LogError($"Missing byte[] for: {this}", e);
            }
            return result;
        }

        protected void SendPacket(BaseServerPacket packet)
        {
            _client.SendPacket(packet);
        }

        protected void ReadB(byte[] data)
        {
            _stream.Read(data, 0, data.Length);
        }

        protected void WriteD(int value)
        {
            _log.LogDebug("WriteD: " + value);
        }

        protected void WriteH(int value)
        {
            _log.LogDebug("WriteH: " + value);
        }
    }
}
