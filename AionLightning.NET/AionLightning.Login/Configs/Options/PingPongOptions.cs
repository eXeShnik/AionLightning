namespace AionLightning.Login.Configs.Options;

public sealed record PingPongOptions
{
    public bool Enabled { get; init; } = true;
    public int DelayMs { get; init; } = 3000;
}
