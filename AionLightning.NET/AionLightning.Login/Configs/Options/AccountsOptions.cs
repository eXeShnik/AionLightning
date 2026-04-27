namespace AionLightning.Login.Configs.Options;

public sealed record AccountsOptions
{
    public string Charset { get; init; } = "ISO8859_2";
    public bool AutoCreate { get; init; } = true;
    public int FastReconnectionTime { get; init; } = 10;
}
