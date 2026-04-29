using System.ComponentModel.DataAnnotations;

namespace AionLightning.Game.Configs.Options;

public sealed record NetworkOptions
{
    [Required] public required string BindAddress { get; init; }
    [Range(1, 65535)] public int GamePort { get; init; } = 7777;
}
