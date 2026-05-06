// Hot-reloadable sample script. Edit text and save — the server reloads automatically.
using System.Threading;
using System.Threading.Tasks;
using AionLightning.Commons.Events;
using AionLightning.Commons.Scripting.Contracts;
using AionLightning.Game.Events;
using Microsoft.Extensions.Logging;

public sealed class Greeter : IScript, IEventHandler<PlayerEnteredWorldEvent>
{
    private IScriptHost _host = null!;

    public ValueTask InitializeAsync(IScriptHost host, CancellationToken ct)
    {
        _host = host;
        _host.Events.Subscribe<PlayerEnteredWorldEvent>(this);
        return ValueTask.CompletedTask;
    }

    public ValueTask HandleAsync(PlayerEnteredWorldEvent e, CancellationToken ct)
    {
        _host.Logger.LogInformation("Welcome to Aion, {Name}!", e.Player.Name);
        return ValueTask.CompletedTask;
    }
}
