namespace AionLightning.Game.Configs.Options;

public sealed record CsConnectionOptions
{
    public bool Enabled { get; init; } = false;
    public string Host { get; init; } = "127.0.0.1";
    public int Port { get; init; } = 9021;
    public int ReconnectDelayMs { get; init; } = 10000;
    public string Password { get; init; } = "*";
}
