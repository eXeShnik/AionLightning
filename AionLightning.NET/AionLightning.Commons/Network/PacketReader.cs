using System.Buffers;
using System.Buffers.Binary;
using System.Text;

namespace AionLightning.Commons.Network;

/// <summary>
/// Stack-allocated wrapper over <see cref="SequenceReader{T}"/> that provides Aion read helpers.
/// Java: readC / readH / readD / readQ / readF / readS / readB.
/// </summary>
public ref struct PacketReader
{
    private SequenceReader<byte> _reader;

    public PacketReader(ReadOnlySequence<byte> sequence)
    {
        _reader = new SequenceReader<byte>(sequence);
    }

    public readonly long Remaining => _reader.Remaining;
    public readonly long Consumed => _reader.Consumed;

    /// <summary>Read one unsigned byte (Java readC).</summary>
    public byte ReadC()
    {
        if (!_reader.TryRead(out byte value))
            throw new PacketFormatException("underflow ReadC");
        return value;
    }

    /// <summary>Read int16 little-endian (Java readH).</summary>
    public short ReadH()
    {
        Span<byte> buf = stackalloc byte[2];
        if (!_reader.TryCopyTo(buf))
            throw new PacketFormatException("underflow ReadH");
        _reader.Advance(2);
        return BinaryPrimitives.ReadInt16LittleEndian(buf);
    }

    /// <summary>Read int32 little-endian (Java readD).</summary>
    public int ReadD()
    {
        Span<byte> buf = stackalloc byte[4];
        if (!_reader.TryCopyTo(buf))
            throw new PacketFormatException("underflow ReadD");
        _reader.Advance(4);
        return BinaryPrimitives.ReadInt32LittleEndian(buf);
    }

    /// <summary>Read int64 little-endian (Java readQ).</summary>
    public long ReadQ()
    {
        Span<byte> buf = stackalloc byte[8];
        if (!_reader.TryCopyTo(buf))
            throw new PacketFormatException("underflow ReadQ");
        _reader.Advance(8);
        return BinaryPrimitives.ReadInt64LittleEndian(buf);
    }

    /// <summary>Read float little-endian (Java readF).</summary>
    public float ReadF()
    {
        Span<byte> buf = stackalloc byte[4];
        if (!_reader.TryCopyTo(buf))
            throw new PacketFormatException("underflow ReadF");
        _reader.Advance(4);
        return BinaryPrimitives.ReadSingleLittleEndian(buf);
    }

    /// <summary>Read UTF-16LE null-terminated string (Java readS).</summary>
    public string ReadS()
    {
        var chars = new List<byte>(64);
        Span<byte> ch = stackalloc byte[2];

        while (true)
        {
            if (_reader.Remaining < 2)
                throw new PacketFormatException("underflow ReadS: missing null terminator");

            _reader.TryCopyTo(ch);
            _reader.Advance(2);

            if (ch[0] == 0 && ch[1] == 0)
                break;

            chars.Add(ch[0]);
            chars.Add(ch[1]);
        }

        if (chars.Count == 0)
            return string.Empty;

        return Encoding.Unicode.GetString(chars.ToArray());
    }

    /// <summary>Read N bytes into a new array (Java readB(len)).</summary>
    public byte[] ReadB(int length)
    {
        var result = new byte[length];
        ReadB(result);
        return result;
    }

    /// <summary>Read N bytes into destination span (Java readB with offset).</summary>
    public void ReadB(Span<byte> destination)
    {
        if (!_reader.TryCopyTo(destination))
            throw new PacketFormatException($"underflow ReadB({destination.Length})");
        _reader.Advance(destination.Length);
    }

    /// <summary>Skip N bytes.</summary>
    public void Skip(int count)
    {
        if (_reader.Remaining < count)
            throw new PacketFormatException($"underflow Skip({count})");
        _reader.Advance(count);
    }
}
