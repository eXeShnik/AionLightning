namespace AionLightning.Game.Configs.Options;

public sealed record GsOptions
{
    public int CountryCode { get; init; } = 1;
    public string ServerName { get; init; } = "Aion Lightning";
    public string ServerVersion { get; init; } = "4.6";
    public int PlayerMaxLevel { get; init; } = 65;
    public string Lang { get; init; } = "en";
    public bool EnableChatServer { get; init; } = true;
    public int StartingLevel { get; init; } = 1;
    public int CharacterLimitCount { get; init; } = 8;
    public int CharacterCreationMode { get; init; } = 0;
}
