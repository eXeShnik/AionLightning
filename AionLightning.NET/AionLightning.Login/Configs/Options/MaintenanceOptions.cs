namespace AionLightning.Login.Configs.Options;

public sealed record MaintenanceOptions
{
    public bool Enabled { get; init; }
    public int GmLevel { get; init; } = 3;
}
