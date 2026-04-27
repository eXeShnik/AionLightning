using System.Buffers;
using System.Buffers.Binary;
using System.Text;

namespace AionLightning.Commons.Network;

/// <summary>
/// Struct wrapper over <see cref="IBufferWriter{T}"/> that provides Aion write helpers.
/// Java: writeC / writeH / writeD / writeQ / writeF / writeS / writeB.
/// </summary>
public struct PacketWriter
{
    private readonly IBufferWriter<byte> _writer;

    public PacketWriter(IBufferWriter<byte> writer)
    {
        _writer = writer;
    }

    /// <summary>Write one byte (Java writeC).</summary>
    public void WriteC(byte value)
    {
        var span = _writer.GetSpan(1);
        span[0] = value;
        _writer.Advance(1);
    }

    /// <summary>Write int16 little-endian (Java writeH).</summary>
    public void WriteH(short value)
    {
        var span = _writer.GetSpan(2);
        BinaryPrimitives.WriteInt16LittleEndian(span, value);
        _writer.Advance(2);
    }

    /// <summary>Convenience overload — caller often has int.</summary>
    public void WriteH(int value) => WriteH((short)value);

    /// <summary>Write int32 little-endian (Java writeD).</summary>
    public void WriteD(int value)
    {
        var span = _writer.GetSpan(4);
        BinaryPrimitives.WriteInt32LittleEndian(span, value);
        _writer.Advance(4);
    }

    /// <summary>Write int64 little-endian (Java writeQ).</summary>
    public void WriteQ(long value)
    {
        var span = _writer.GetSpan(8);
        BinaryPrimitives.WriteInt64LittleEndian(span, value);
        _writer.Advance(8);
    }

    /// <summary>Write float little-endian (Java writeF).</summary>
    public void WriteF(float value)
    {
        var span = _writer.GetSpan(4);
        BinaryPrimitives.WriteSingleLittleEndian(span, value);
        _writer.Advance(4);
    }

    /// <summary>Write UTF-16LE null-terminated string (Java writeS).</summary>
    public void WriteS(string? value)
    {
        if (!string.IsNullOrEmpty(value))
        {
            int byteCount = Encoding.Unicode.GetByteCount(value);
            var span = _writer.GetSpan(byteCount + 2);
            Encoding.Unicode.GetBytes(value, span);
            span[byteCount] = 0;
            span[byteCount + 1] = 0;
            _writer.Advance(byteCount + 2);
        }
        else
        {
            var span = _writer.GetSpan(2);
            span[0] = 0;
            span[1] = 0;
            _writer.Advance(2);
        }
    }

    /// <summary>Write raw bytes (Java writeB).</summary>
    public void WriteB(ReadOnlySpan<byte> bytes)
    {
        var span = _writer.GetSpan(bytes.Length);
        bytes.CopyTo(span);
        _writer.Advance(bytes.Length);
    }

    /// <summary>Write N zero bytes.</summary>
    public void WriteZero(int count)
    {
        var span = _writer.GetSpan(count);
        span[..count].Clear();
        _writer.Advance(count);
    }
}
