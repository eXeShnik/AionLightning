namespace AionLightning.Commons.Scripting.Contracts;

public interface IScript
{
    ValueTask InitializeAsync(IScriptHost host, CancellationToken ct);
}
