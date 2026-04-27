namespace AionLightning.Login.Configs.Options;

public sealed record SecurityOptions
{
    public bool EnableFloodProtection { get; init; } = true;
    public bool EnableBruteforceProtection { get; init; } = true;
    public int LoginTriesBeforeBan { get; init; } = 5;
    public int BanTimeForBruteforcing { get; init; } = 15;
    public string ExcludedIps { get; init; } = string.Empty;
}
