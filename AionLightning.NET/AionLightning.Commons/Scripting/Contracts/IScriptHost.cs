using AionLightning.Commons.Events;
using Microsoft.Extensions.Logging;

namespace AionLightning.Commons.Scripting.Contracts;

public interface IScriptHost
{
    IEventBus Events { get; }
    ILogger<IScript> Logger { get; }
}
