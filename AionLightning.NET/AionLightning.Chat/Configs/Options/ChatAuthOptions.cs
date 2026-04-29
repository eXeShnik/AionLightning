namespace AionLightning.Chat.Configs.Options;

public sealed record ChatAuthOptions
{
    public string Password { get; init; } = "*";
    public int MessageDelaySeconds { get; init; } = 30;
}
