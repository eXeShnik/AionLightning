namespace AionLightning.Commons.Events;

public interface IEventHandler<in TEvent> where TEvent : IGameEvent
{
    ValueTask HandleAsync(TEvent @event, CancellationToken ct);
}
