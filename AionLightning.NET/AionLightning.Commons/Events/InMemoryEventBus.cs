using System.Collections.Concurrent;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace AionLightning.Commons.Events;

public sealed class InMemoryEventBus(IServiceProvider sp, ILogger<InMemoryEventBus> log) : IEventBus
{
    // Runtime subscriptions keyed by event type. Values are ConcurrentBag<object>.
    private readonly ConcurrentDictionary<Type, object> _runtimeHandlers = new();

    public async ValueTask PublishAsync<TEvent>(TEvent @event, CancellationToken ct) where TEvent : IGameEvent
    {
        // DI-registered handlers
        foreach (var handler in sp.GetServices<IEventHandler<TEvent>>())
            await InvokeAsync(handler, @event, ct);

        // Runtime-registered handlers (scripts)
        if (_runtimeHandlers.TryGetValue(typeof(TEvent), out var bag))
        {
            foreach (var h in (ConcurrentBag<IEventHandler<TEvent>>)bag)
                await InvokeAsync(h, @event, ct);
        }
    }

    public void Subscribe<TEvent>(IEventHandler<TEvent> handler) where TEvent : IGameEvent
    {
        var bag = (ConcurrentBag<IEventHandler<TEvent>>)_runtimeHandlers
            .GetOrAdd(typeof(TEvent), _ => new ConcurrentBag<IEventHandler<TEvent>>());
        bag.Add(handler);
    }

    public void Unsubscribe<TEvent>(IEventHandler<TEvent> handler) where TEvent : IGameEvent
    {
        // ConcurrentBag doesn't support removal; replace with a new bag excluding the handler.
        _runtimeHandlers.AddOrUpdate(
            typeof(TEvent),
            _ => new ConcurrentBag<IEventHandler<TEvent>>(),
            (_, existing) =>
            {
                var old = (ConcurrentBag<IEventHandler<TEvent>>)existing;
                var newBag = new ConcurrentBag<IEventHandler<TEvent>>(
                    old.Where(h => !ReferenceEquals(h, handler)));
                return newBag;
            });
    }

    private async ValueTask InvokeAsync<TEvent>(IEventHandler<TEvent> handler, TEvent @event, CancellationToken ct)
        where TEvent : IGameEvent
    {
        try
        {
            await handler.HandleAsync(@event, ct);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            log.LogError(ex, "Handler {Handler} failed for {Event}", handler.GetType().Name, typeof(TEvent).Name);
        }
    }
}
