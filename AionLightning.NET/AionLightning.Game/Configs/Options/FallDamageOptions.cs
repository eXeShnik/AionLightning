namespace AionLightning.Game.Configs.Options;

/// <summary>
/// Java <c>configs.main.FallDamageConfig</c> (gameserver.falldamage.*) ported verbatim.
/// Controls HP loss taken from falling (see <see cref="Services.FallDamageService"/>).
/// </summary>
public sealed record FallDamageOptions
{
    /// <summary>Java <c>ACTIVE_FALL_DAMAGE</c> — master on/off switch for fall damage.</summary>
    public bool Enable { get; init; } = true;

    /// <summary>Java <c>FALL_DAMAGE_PERCENTAGE</c> — percentage of max HP lost per meter fallen.</summary>
    public float DamagePercentage { get; init; } = 1.0f;

    /// <summary>Java <c>MINIMUM_DISTANCE_DAMAGE</c> — minimum fall distance (meters) before any damage applies.</summary>
    public int MinimumDistance { get; init; } = 10;

    /// <summary>Java <c>MAXIMUM_DISTANCE_DAMAGE</c> — fall distance (meters) at or above which landing is lethal.</summary>
    public int MaximumDistance { get; init; } = 50;

    /// <summary>Java <c>MAXIMUM_DISTANCE_MIDAIR</c> — cumulative descent (meters) at or above which the player dies mid-air, before landing.</summary>
    public int MaximumDistanceMidair { get; init; } = 200;
}
