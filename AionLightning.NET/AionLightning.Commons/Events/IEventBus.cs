namespace AionLightning.Commons.Events;

/// <summary>
/// Bespoke Channel&lt;T&gt;-backed event bus. Implementation lands in M5.
/// Only the interface contract is defined here so early consumers can declare dependencies.
/// </summary>
public interface IEventBus
{
    ValueTask PublishAsync<TEvent>(TEvent @event, CancellationToken ct) where TEvent : IGameEvent;
}
