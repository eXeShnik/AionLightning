using System.Buffers;
using System.Buffers.Binary;
using System.IO.Pipelines;
using System.Net;
using System.Net.Sockets;

namespace AionLightning.Commons.Network;

/// <summary>
/// Async Pipelines-based connection base. Reads Aion-framed packets (2-byte LE length prefix)
/// and dispatches them to <see cref="OnPacketAsync"/>. Graceful shutdown via CancellationToken.
/// </summary>
public abstract class AConnection : IAsyncDisposable
{
    private readonly Socket _socket;
    private readonly NetworkStream _stream;
    protected readonly PipeReader Reader;
    protected readonly PipeWriter Writer;
    private int _disposed;

    protected AConnection(Socket socket)
    {
        _socket = socket;
        _stream = new NetworkStream(socket, ownsSocket: true);
        Reader = PipeReader.Create(_stream);
        Writer = PipeWriter.Create(_stream);
    }

    public string IP => (_socket.RemoteEndPoint as IPEndPoint)?.Address.ToString() ?? "?";

    public async Task RunAsync(CancellationToken ct)
    {
        try
        {
            await OnConnectedAsync(ct);
            await ReadLoopAsync(ct);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested) { }
        catch (IOException) { /* client disconnected */ }
        finally
        {
            await DisposeAsync();
        }
    }

    protected virtual ValueTask OnConnectedAsync(CancellationToken ct) => ValueTask.CompletedTask;

    private async Task ReadLoopAsync(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested && _disposed == 0)
        {
            ReadResult result;
            try { result = await Reader.ReadAsync(ct); }
            catch (InvalidOperationException) { return; } // DisposeAsync called before ReadAsync

            var buffer = result.Buffer;

            if (result.IsCompleted && buffer.IsEmpty)
                break;

            var consumed = buffer.Start;
            var examined = buffer.End;

            try
            {
                while (TryReadFrame(ref buffer, out var frame, out var frameEnd))
                {
                    await OnPacketAsync(frame, ct);
                    consumed = frameEnd;
                    if (_disposed != 0) return; // DisposeAsync called from packet handler
                }
            }
            finally
            {
                if (_disposed == 0)
                    Reader.AdvanceTo(consumed, examined);
            }

            if (result.IsCompleted)
                break;
        }
    }

    private static bool TryReadFrame(
        ref ReadOnlySequence<byte> buffer,
        out ReadOnlySequence<byte> frame,
        out SequencePosition frameEnd)
    {
        if (buffer.Length < 2)
        {
            frame = default;
            frameEnd = default;
            return false;
        }

        Span<byte> lenSpan = stackalloc byte[2];
        buffer.Slice(0, 2).CopyTo(lenSpan);
        int packetLength = BinaryPrimitives.ReadInt16LittleEndian(lenSpan);

        if (packetLength < 2)
            throw new PacketFormatException($"Invalid packet length: {packetLength}");

        if (buffer.Length < packetLength)
        {
            frame = default;
            frameEnd = default;
            return false;
        }

        frame = buffer.Slice(2, packetLength - 2);
        frameEnd = buffer.GetPosition(packetLength);
        buffer = buffer.Slice(packetLength);
        return true;
    }

    /// <summary>
    /// Write a length-prefixed, opcode-prefixed, encrypted packet to the wire.
    /// The raw bytes (post-encryption, with length prefix) are passed directly.
    /// </summary>
    protected async ValueTask WriteRawAsync(ReadOnlyMemory<byte> data, CancellationToken ct)
    {
        Writer.Write(data.Span);
        await Writer.FlushAsync(ct);
    }

    /// <summary>Called for every complete decrypted frame. Subclass decrypts and dispatches.</summary>
    protected abstract ValueTask OnPacketAsync(ReadOnlySequence<byte> frame, CancellationToken ct);

    public virtual async ValueTask DisposeAsync()
    {
        if (Interlocked.Exchange(ref _disposed, 1) != 0)
            return;

        await Reader.CompleteAsync();
        await Writer.CompleteAsync();
        await _stream.DisposeAsync();
    }
}
