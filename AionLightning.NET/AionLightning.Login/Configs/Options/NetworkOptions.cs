using System.ComponentModel.DataAnnotations;

namespace AionLightning.Login.Configs.Options;

public sealed record NetworkOptions
{
    [Required] public required string BindAddress { get; init; }
    [Range(1, 65535)] public int ClientPort { get; init; } = 2106;
    [Range(1, 65535)] public int GameServerPort { get; init; } = 9014;
    public int ReadThreads { get; init; }
    public int WriteThreads { get; init; }
}
