using System;
using System.IO;

namespace AionLightning.Commons.Network
{
    public class ByteBuffer
    {
        private readonly MemoryStream _stream;
        private readonly BinaryReader _reader;
        private readonly BinaryWriter _writer;

        public ByteBuffer()
        {
            _stream = new MemoryStream();
            _reader = new BinaryReader(_stream);
            _writer = new BinaryWriter(_stream);
        }

        public ByteBuffer(byte[] data)
        {
            _stream = new MemoryStream(data);
            _reader = new BinaryReader(_stream);
            _writer = new BinaryWriter(_stream);
        }

        public void Put(byte[] data)
        {
            _writer.Write(data);
        }

        public byte[] ToArray()
        {
            return _stream.ToArray();
        }

        public int Remaining()
        {
            return (int)(_stream.Length - _stream.Position);
        }

        public byte Get()
        {
            return _reader.ReadByte();
        }

        public void Get(byte[] dst)
        {
            _reader.Read(dst, 0, dst.Length);
        }
    }
}
