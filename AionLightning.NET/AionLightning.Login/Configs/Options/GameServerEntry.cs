namespace AionLightning.Login.Configs.Options;

public sealed record GameServerEntry
{
    public byte Id { get; init; } = 1;
    public string Name { get; init; } = string.Empty;
    public string Address { get; init; } = "127.0.0.1";
    public int Port { get; init; } = 7777;
    public int MaxPlayers { get; init; } = 1000;
    public string Password { get; init; } = "aion";
}
