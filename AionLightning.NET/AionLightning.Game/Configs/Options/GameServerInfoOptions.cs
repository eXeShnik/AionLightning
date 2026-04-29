namespace AionLightning.Game.Configs.Options;

public sealed record GameServerInfoOptions
{
    public byte Id { get; init; } = 1;
    public string Name { get; init; } = "Aion";
    public string Password { get; init; } = "aion";
    public int MaxPlayers { get; init; } = 1000;
}
