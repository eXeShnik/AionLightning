namespace AionLightning.Game.Configs.Options;

/// <summary>
/// Controls whether the real geo engine (<c>RealGeoService</c>) loads and serves geodata, or
/// whether the server falls back to <c>DummyGeoService</c> (Java parity for
/// <c>GEO_ENABLE=false</c>). Defaults to disabled since no <c>.geo</c> dataset exists in this
/// repository yet — see migration_plan.md's C4 survey.
/// </summary>
public sealed record GeoDataOptions
{
    public bool Enable { get; init; }

    public string DataPath { get; init; } = "data/geo";
}
