using System.ComponentModel.DataAnnotations;

namespace AionLightning.Game.Configs.Options;

public sealed record LsConnectionOptions
{
    [Required] public required string Host { get; init; }
    [Range(1, 65535)] public int Port { get; init; } = 9014;
    public int ReconnectDelayMs { get; init; } = 15000;
}
