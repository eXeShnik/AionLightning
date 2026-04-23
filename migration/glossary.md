# Java → .NET glossary

Common Java concepts used in the Aion codebase and their .NET equivalents. Purpose: keep idiom choices consistent across ported classes.

Format: **Java** → **.NET** — a short note about the pitfall or context.

## Threads and synchronisation

- `Thread t = new Thread(r); t.start();` → **do not** default to `new Thread().Start()`; use `Task.Run` or `BackgroundService` with `CancellationToken`. `new Thread` is reserved for long-lived dedicated loops with an explicit reason.
- `synchronized (obj)` → `lock (obj)` — equivalent.
- `synchronized` method → rewrite: encapsulate state in a private class and `lock` on a private field; `lock(this)` is an anti-pattern.
- `ReentrantLock` → `lock` (C# locks are always reentrant) or `SemaphoreSlim` for async-friendly variants.
- `volatile int` → `int` with `Volatile.Read/Write`, or `Interlocked.*`. C# `volatile` does not imply a full fence the way Java `volatile` does.
- `AtomicInteger` → `int` + `Interlocked.Increment/Add/CompareExchange`.
- `AtomicReference<T>` → `T` + `Interlocked.Exchange/CompareExchange<T>` (reference types).
- `CountDownLatch` → `CountdownEvent` (sync) or a `TaskCompletionSource` with a counter (async).
- `CyclicBarrier` → no direct equivalent; for async, build one on `SemaphoreSlim` + `Channel<T>`.
- `ReadWriteLock` → `ReaderWriterLockSlim` (sync). Rarely needed.
- `ThreadLocal<T>` → `AsyncLocal<T>` (async flow) or `ThreadLocal<T>` (sync only).

## Concurrent collections

- `ConcurrentHashMap<K,V>` → `ConcurrentDictionary<K,V>`.
- `ConcurrentHashMap.computeIfAbsent(k, f)` → **pitfall**: `ConcurrentDictionary.GetOrAdd(k, f)` can invoke the factory multiple times concurrently. Use the `Lazy<V>` value-factory overload: `dict.GetOrAdd(k, _ => new Lazy<V>(() => factory(), LazyThreadSafetyMode.ExecutionAndPublication)).Value`.
- `ConcurrentLinkedQueue<T>` → `ConcurrentQueue<T>`.
- `CopyOnWriteArrayList<T>` → no direct equivalent; `ImmutableList<T>.Builder` or manual `Interlocked.Exchange`.
- `BlockingQueue<T>` / `LinkedBlockingQueue<T>` → `Channel<T>` (modern async) or `BlockingCollection<T>` (sync).

## Scheduling and time

- `Thread.sleep(n)` → `await Task.Delay(n, ct)` (in async). Never `Thread.Sleep` in new code.
- `ScheduledExecutorService.scheduleAtFixedRate` → `PeriodicTimer` + `await foreach` in a `BackgroundService`.
- `Timer.schedule(task, delay)` → `await Task.Delay(delay, ct); task();` or `PeriodicTimer`.
- `System.currentTimeMillis()` → `DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()`.
- `System.nanoTime()` → `Stopwatch.GetTimestamp()` / `Stopwatch.StartNew()` for measurements.
- **Timing:** never `DateTime.Now - start`; always `Stopwatch`.

## I/O and networking

- `ByteBuffer.put/get/flip/compact/position/limit` → `Span<byte>` / `Memory<byte>` / `SequenceReader<byte>` / `ArrayBufferWriter<byte>`. Cursor semantics do not port 1:1; use slice-based equivalents. See [ADR-001](adr/001-networking.md) and [R-001](risks.md).
- `SocketChannel.read(buffer)` → `await socket.ReceiveAsync(memory, ct)` or `PipeReader.ReadAsync(ct)`.
- `ServerSocketChannel.accept()` → `await Socket.AcceptAsync(ct)`.
- `SelectionKey.OP_READ/OP_WRITE` — **does not port**; use Pipelines, which hides the selector.
- `ByteOrder.LITTLE_ENDIAN` → the Aion protocol is little-endian by default; read via `BinaryPrimitives.Read{UInt16,UInt32,UInt64}LittleEndian`.
- `InetAddress` → `System.Net.IPAddress`.
- `InetSocketAddress` → `System.Net.IPEndPoint`.

## Error handling

- `throws IOException` (checked) → disappears. Every ported DAO / network class must be code-reviewed. See [R-004](risks.md).
- `RuntimeException` → `System.Exception` or a specific exception.
- `IllegalArgumentException` → `ArgumentException` / `ArgumentOutOfRangeException`.
- `IllegalStateException` → `InvalidOperationException`.
- `NullPointerException` → `NullReferenceException` (rarely thrown manually; nullable annotations catch most).
- `NumberFormatException` → `FormatException`.

## Collections

- `List<T>` (Java `ArrayList`) → `List<T>`.
- `LinkedList<T>` → `LinkedList<T>` (rarely needed) or `List<T>`.
- `Map<K,V>` → `Dictionary<K,V>` or `IReadOnlyDictionary<K,V>` on public APIs.
- `HashMap` → `Dictionary`.
- `TreeMap` → `SortedDictionary<K,V>`.
- `Set<T>` → `HashSet<T>`.
- `TreeSet<T>` → `SortedSet<T>`.
- `Iterable<T>` → `IEnumerable<T>`.
- `Iterator<T>` → `IEnumerator<T>` (rarely used directly — prefer `foreach` + LINQ).
- `Collections.unmodifiableList(x)` → `x.AsReadOnly()` or expose as `IReadOnlyList<T>`.
- `Collections.emptyList()` → `[]` (C# 12+ collection expression) or `Array.Empty<T>()`.

## OO constructs

- `final class` → `sealed class`.
- `final` field → `readonly` field or get-only property (`public int X { get; }`).
- `abstract class` → `abstract class` (identical).
- `interface` → `interface` (C# 8+ supports default methods).
- `@Override` → `override` (keyword, not an attribute).
- `instanceof` → `is` (with pattern matching: `if (obj is Foo f) { ... }`).
- `T.class` → `typeof(T)`.
- `obj.getClass()` → `obj.GetType()`.
- `Object.equals` → `Equals(object?)`. For records — generated automatically.
- `hashCode()` → `GetHashCode()`. Always paired with `Equals`.
- `toString()` → `ToString()`.
- `clone()` → `with { }` for records, or an explicit copy ctor.
- Java enum with methods → C# `enum` + extension methods, **or** records with factory-static fields when per-value behaviour is required.

## Java-specific things that do not port

- **Java agent (ASM bytecode rewriting)** → the concept is dropped; see [ADR-005](adr/005-callbacks.md).
- **`@ObjectCallback`, `@GlobalCallback` attributes** → `await bus.Publish(new XEvent(...))` at the same site.
- **`ClassLoader` hierarchy** → `AssemblyLoadContext`. Cross-ALC types are not equivalent; see [R-005](risks.md).
- **Checked exceptions** → see above.
- **`Runnable`, `Callable`** → `Action`, `Func<T>`, `Task`, `Func<Task>`.
- **`Comparable<T>.compareTo`** → `IComparable<T>.CompareTo`.
- **`Comparator<T>`** → `IComparer<T>` or a `Comparison<T>` lambda.
- **`Supplier<T>`** → `Func<T>`.
- **`Consumer<T>`** → `Action<T>`.
- **`Predicate<T>`** → `Predicate<T>` or `Func<T, bool>`.
- **`Optional<T>`** → not used; use nullable (`T?`) with nullable annotations.

## Services / injection

- `ServiceLoader` → `IServiceProvider` / the DI container.
- `@Inject` / Spring `@Autowired` → constructor injection through `Microsoft.Extensions.DependencyInjection`.
- Singleton via `getInstance()` → DI `AddSingleton<T>()`; `getInstance` methods are removed.
- `InitializingBean.afterPropertiesSet` → `IHostedService.StartAsync`.
- `@PostConstruct` → `IHostedService.StartAsync` or `IAsyncDisposable` / `IDisposable`.

## Logging

- `org.slf4j.Logger log = LoggerFactory.getLogger(X.class);` → `ILogger<X>` via constructor injection.
- `log.info("{}", arg)` → `_log.LogInformation("{Arg}", arg)` (named placeholders, not positional).
- **Pitfall:** Java SLF4J placeholders `{}` are positional. `ILogger` expects named. Port explicitly: `log.info("user {} logged in", id)` → `_log.LogInformation("user {UserId} logged in", id)`.

## Config

- `.properties` file → `appsettings.json` + `record` DTO via `BindConfiguration`. See [ADR-002](adr/002-configuration.md).
- `@Value("${x}")` → `IOptions<T>` constructor injection.
- `System.getProperty(x)` → `IConfiguration["x"]` or an environment variable.

## Tests

- JUnit `@Test` → xUnit `[Fact]` / `[Theory]` + `[InlineData]`.
- `@Before` → constructor of the test class.
- `@After` → `Dispose` / `IAsyncDisposable`.
- Mockito `mock()` → NSubstitute `Substitute.For<T>()` or Moq. Not mandatory now, deferred to M7.
