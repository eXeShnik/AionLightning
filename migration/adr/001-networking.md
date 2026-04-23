# ADR-001: Networking stack

- **Status:** Accepted
- **Date:** 2026-04-23

## Context

The Aion server is a packet-heavy stateful TCP server with three listeners (Login client, Login↔GS, Game/Chat client) and long-lived connections (hundreds to thousands per process).

The Java original uses NIO (`Selector` + `SocketChannel` + `ByteBuffer`) with hand-rolled accept / read-write dispatcher threads.

The current .NET port in `Commons/Network/*` translates that model 1:1 using raw `System.Net.Sockets.Socket` + `new Thread().Start()`:

- `Dispatcher.Run`: `while (true) { Dispatch(); Thread.Sleep(1); } catch (Exception e) { log; }` — spin loop with swallowed exceptions (`Dispatcher.cs:34-48`).
- `Executor` implements its own queue on `Monitor.Wait/Pulse`.
- No `async`, no `CancellationToken`, no `Shutdown` (`Dispatcher.cs:31`: `// TODO: implement`).

This does not scale past a few thousand clients, wastes ~1 MB stack per thread, and breaks graceful shutdown.

## Decision

Use **`System.IO.Pipelines` on top of async `Socket` APIs** (`Socket.AcceptAsync`, `Socket.ReceiveAsync`, `Socket.SendAsync`).

- One `BackgroundService` per listener (client listener, GS listener).
- Accept loop: `await listenerSocket.AcceptAsync(ct)` → create an `AConnection` that owns a `PipeReader` / `PipeWriter`.
- Decoder: `SequenceReader<byte>` over `PipeReader.ReadAsync` results.
- Encoder: `IBufferWriter<byte>` implemented by the `PipeWriter`.
- Packet pipeline: read full frame → dispatch to handler → handler produces response → write to `PipeWriter` → flush.
- Graceful shutdown through the `CancellationToken` propagated from the host lifetime.

## Alternatives considered

### Alt-1: Kestrel `ConnectionHandler` (non-HTTP)

- Pros: battle-tested accept / pipeline transport, graceful shutdown, optional TLS.
- Cons: pulls ASP.NET Core runtime into console apps that otherwise need none. Configuration ergonomics do not match our "three independent processes" layout.
- Why rejected: extra runtime surface, minimal gain over raw Pipelines.

### Alt-2: SuperSocket 2.x

- Pros: framework designed for game / IoT / industrial TCP, session / package abstractions, uses Kestrel pipeline transport under the hood.
- Cons: opinionated session / package model fights custom framing + Aion's Blowfish-wrapped packets with variable-length fields.
- Why rejected: the abstraction layer forces us to either bypass it for framing or fight it — both defeat the purpose.

### Alt-3: DotNetty

- Pros: Netty-like mental model (ChannelPipeline, handlers) — structurally close to the Java NIO codebase we are porting.
- Cons: project activity is lower, does not use Pipelines, measurably slower.
- Why rejected: we gain the familiar mental model but lose the modern performance story.

### Alt-4: Keep raw `Socket` + `SocketAsyncEventArgs`

- Pros: maximum control over buffer pools and execution context.
- Cons: hand-rolled buffer management is exactly what Pipelines was built to replace; higher bug surface.
- Why rejected: Pipelines covers every real need we have.

### Alt-5: Keep the current port

- Pros: already written.
- Cons: spin loop, swallowed exceptions, no shutdown, no async, no cancellation. Every production symptom comes directly from these.
- Why rejected: see current-state diagnosis.

## Consequences

### Positive

- Idiomatic .NET, async end-to-end, cancellation-aware.
- `SequenceReader<byte>` is the right shape for Aion's little-endian framing.
- Graceful shutdown becomes trivial.
- Works well with DI, no custom thread scheduling.

### Negative / trade-offs

- `ByteBuffer` cursor semantics in Java packet classes do not port 1:1. An adapter layer is needed so each packet class looks similar to its Java counterpart (`readC`/`readH`/`readD`/`readQ`/`readS`). See [R-001](../risks.md).
- Existing .NET port of `NioServer`, `Dispatcher`, `AcceptDispatcherImpl`, `AcceptReadWriteDispatcherImpl`, `Executor` is discarded. `ServerCfg`, `IPRange`, `IConnectionFactory` are kept but reshaped to async.

## Implementation notes

- Libraries: none beyond the BCL. `System.IO.Pipelines` ships as part of .NET.
- Optional helper: `mgravell/Pipelines.Sockets.Unofficial` — provides a glue `SocketConnection` that combines `Socket` and `Pipe`. Add if it saves meaningful code in M1; otherwise hand-rolled.
- Key types: `Socket`, `PipeReader`, `PipeWriter`, `SequenceReader<byte>`, `IBufferWriter<byte>`, `ArrayBufferWriter<byte>`.

### Packet buffer adapter

Aion packets are little-endian; Java uses `readC` (byte), `readH` (short), `readD` (int), `readQ` (long), `readS` (UTF-16 null-terminated), `readB(int)` (byte[]). We provide matching extension methods on `ref SequenceReader<byte>` and on the writer, so a Java packet class ports mechanically.

### Connection lifecycle sketch

```csharp
public abstract class AConnection : IAsyncDisposable
{
    private readonly Socket _socket;
    private readonly PipeReader _reader;
    private readonly PipeWriter _writer;

    protected AConnection(Socket socket) { /* ctor sets up pipe */ }

    public async Task RunAsync(CancellationToken ct)
    {
        var readTask = ReadLoopAsync(ct);
        var writeTask = WriteLoopAsync(ct);
        await Task.WhenAny(readTask, writeTask);
        // propagate cancellation, dispose
    }

    protected abstract ValueTask OnPacketAsync(ReadOnlySequence<byte> frame, CancellationToken ct);
    public abstract ValueTask DisposeAsync();
}
```

## Affected milestones

- M1: introduces the base classes.
- M2: first listener built on this (Login client 2106).
- M3: second listener reuses the same code (Login↔GS 9014).
- M4, M5: reused for Chat and Game.

## Related

- ADR-003 (Hosting): each listener is exposed as a `BackgroundService`.
- Risks: [R-001](../risks.md), [R-007](../risks.md).
