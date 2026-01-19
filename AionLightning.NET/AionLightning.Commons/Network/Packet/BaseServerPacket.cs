using System.IO;
using System.Text;

namespace AionLightning.Commons.Network.Packet
{
    public abstract class BaseServerPacket : AionPacket
    {
        private MemoryStream _stream;
        private BinaryWriter _writer;

        protected BaseServerPacket(int opcode) : base(PacketType.SERVER, opcode)
        {
            _stream = new MemoryStream();
            _writer = new BinaryWriter(_stream);
        }

        public byte[] GetBytes()
        {
            return _stream.ToArray();
        }

        protected void WriteD(int value)
        {
            _writer.Write(value);
        }

        protected void WriteH(int value)
        {
            _writer.Write((short)value);
        }

        protected void WriteC(int value)
        {
            _writer.Write((byte)value);
        }

        protected void WriteDF(double value)
        {
            _writer.Write(value);
        }

        protected void WriteF(float value)
        {
            _writer.Write(value);
        }

        protected void WriteQ(long value)
        {
            _writer.Write(value);
        }

        protected void WriteS(string text)
        {
            if (text == null)
            {
                _writer.Write((char)0);
            }
            else
            {
                _writer.Write(Encoding.Unicode.GetBytes(text));
                _writer.Write((char)0);
            }
        }

        protected void WriteB(byte[] data)
        {
            _writer.Write(data);
        }
    }
}
