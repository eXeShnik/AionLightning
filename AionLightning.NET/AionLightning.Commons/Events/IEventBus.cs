namespace AionLightning.Commons.Events;

/// <summary>
/// Bespoke event bus. DI-registered handlers are resolved at publish time.
/// Scripts use Subscribe/Unsubscribe for runtime-dynamic subscriptions.
/// </summary>
public interface IEventBus
{
    ValueTask PublishAsync<TEvent>(TEvent @event, CancellationToken ct) where TEvent : IGameEvent;

    /// <summary>Register a runtime handler (e.g. from a hot-reloaded script).</summary>
    void Subscribe<TEvent>(IEventHandler<TEvent> handler) where TEvent : IGameEvent;

    /// <summary>Unregister a runtime handler registered via Subscribe.</summary>
    void Unsubscribe<TEvent>(IEventHandler<TEvent> handler) where TEvent : IGameEvent;
}
