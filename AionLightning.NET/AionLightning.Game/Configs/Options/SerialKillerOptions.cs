namespace AionLightning.Game.Configs.Options;

/// <summary>
/// Serial Killer subsystem (Java CustomConfig gameserver.serialkiller.*). Tracks players who repeatedly
/// kill enemy-race players while invading that race's home territory — Java's "enemy world": a world
/// registered in <see cref="HandledWorlds"/> to the *opposing* race, so entering it flags you as an
/// invader there. Escalating victim counts raise a rank (1-2) that applies a stat-penalty debuff and is
/// broadcast to the affected player; killing an active serial killer rewards nearby same-race allies of
/// the killer. See SerialKillerService's class doc for the full behavior and Java-fidelity notes.
/// </summary>
public sealed record SerialKillerOptions
{
    public bool Enabled { get; init; } = true;

    /// <summary>
    /// Comma-separated world ids that participate in the serial-killer system (Java
    /// gameserver.serialkiller.handledworlds, default empty = feature has no effect anywhere even when
    /// <see cref="Enabled"/> is true). Each world id's 2nd character encodes its home race — ported
    /// verbatim from Java's <c>world.charAt(1)</c> parsing: '1' = ELYOS home turf, '2'+ = ASMODIANS home
    /// turf, '0' = USEALL (neither race's home turf, e.g. Abyss/Balaurea zones — always "enemy"
    /// territory for both races there).
    /// </summary>
    public string HandledWorlds { get; init; } = "";

    /// <summary>Java gameserver.serialkiller.kills.refresh — minutes between decay sweeps.</summary>
    public int RefreshMinutes { get; init; } = 5;

    /// <summary>Java gameserver.serialkiller.kills.decrease — victim count subtracted per decay sweep.</summary>
    public int DecayPerTick { get; init; } = 1;

    /// <summary>
    /// Java gameserver.serialkiller.level.diff — the killer must be at least this many levels above the
    /// victim for the kill to count toward serial-killer rank (guards against low-level defenders
    /// inflating a high-level invader's rank disproportionately... actually the reverse: this gates
    /// which kills count at all, so only high-level-vs-low-level kills accrue rank — a griefer/ganker
    /// penalty, not a duel-fairness one).
    /// </summary>
    public int LevelDiff { get; init; } = 10;

    /// <summary>Java gameserver.serialkiller.1st.rank.kills — victim count strictly above this reaches rank 1.</summary>
    public int Rank1Kills { get; init; } = 25;

    /// <summary>Java gameserver.serialkiller.2nd.rank.kills — victim count strictly above this reaches rank 2.</summary>
    public int Rank2Kills { get; init; } = 50;
}
