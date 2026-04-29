namespace AionLightning.Game.Configs.Options;

public sealed record WorldOptions
{
    public int RegionSize { get; init; } = 128;
    public bool ActiveTrace { get; init; } = true;
    public bool EmulateFasttrack { get; init; } = true;
}
