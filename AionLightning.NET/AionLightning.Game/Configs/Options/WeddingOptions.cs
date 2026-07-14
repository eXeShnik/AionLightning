namespace AionLightning.Game.Configs.Options;

/// <summary>Java configs.main.WeddingsConfig (weddings.properties) — marriage subsystem policy knobs
/// consumed by <see cref="Services.WeddingService"/>.</summary>
public sealed record WeddingOptions
{
    public bool Enable { get; init; } = true;

    /// <summary>Java WEDDINGS_SAME_SEX — when false, both partners must have different Gender.</summary>
    public bool AllowSameSex { get; init; } = true;

    /// <summary>Java WEDDINGS_DIFF_RACES — when false, both partners must share the same Race.</summary>
    public bool AllowDifferentRaces { get; init; } = false;

    /// <summary>Java WEDDINGS_KINAH — kinah cost deducted from both partners on a successful marriage.</summary>
    public long KinahCost { get; init; } = 0;

    /// <summary>Java WEDDINGS_ANNOUNCE — broadcasts "X and Y are now married." to every online player.</summary>
    public bool Announce { get; init; } = true;

    /// <summary>Seconds to wait for the target's yes/no answer before the proposal times out (Java used
    /// no explicit timeout for its chat-command accept flow; this port reuses the duel-request convention).</summary>
    public int ProposalTimeoutSeconds { get; init; } = 30;
}
