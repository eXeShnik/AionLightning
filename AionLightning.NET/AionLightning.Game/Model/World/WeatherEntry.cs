namespace AionLightning.Game.Model.World;

/// <summary>Java model.templates.world.WeatherEntry — one &lt;table&gt; row from weather_table.xml: a
/// possible weather state for one zone index within a map.</summary>
public sealed record WeatherEntry(int ZoneId, int Code, int Rank, string? Name, bool Before, bool After)
{
    /// <summary>The "no weather" default entry (Java's no-arg constructor: code 0, rank 0).</summary>
    public static readonly WeatherEntry None = new(0, 0, 0, null, false, false);
}
