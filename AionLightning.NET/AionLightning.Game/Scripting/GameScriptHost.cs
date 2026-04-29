using AionLightning.Commons.Events;
using AionLightning.Commons.Scripting.Contracts;
using Microsoft.Extensions.Logging;

namespace AionLightning.Game.Scripting;

public sealed class GameScriptHost(IEventBus events, ILogger<IScript> logger) : IScriptHost
{
    public IEventBus Events  => events;
    public ILogger<IScript> Logger => logger;
}
