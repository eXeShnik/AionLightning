using System.ComponentModel.DataAnnotations;

namespace AionLightning.Chat.Configs.Options;

public sealed record ChatNetworkOptions
{
    [Required] public string BindAddress { get; init; } = "0.0.0.0";
    [Range(1, 65535)] public int ClientPort { get; init; } = 10241;
    [Range(1, 65535)] public int GameServerPort { get; init; } = 9021;
}
