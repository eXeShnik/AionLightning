namespace AionLightning.Game.Services;

/// <summary>
/// Computes soldier-tier abyss rank (1–9) from accumulated AP.
/// Officer ranks (10+) require a separate Glory Points system not yet implemented.
/// </summary>
public static class AbyssRankService
{
    // (requiredAp, rankId) — ordered ascending; rank 1 requires 0 AP
    private static readonly (long Ap, int Rank)[] Thresholds =
    [
        (0,      1),
        (1200,   2),
        (4220,   3),
        (10990,  4),
        (23500,  5),
        (42780,  6),
        (69700,  7),
        (105600, 8),
        (150800, 9),
    ];

    /// <summary>
    /// Abyss world ID (Reshanta). NPCs in this map reward AP on kill.
    /// </summary>
    public const int AbyssWorldId = 400010000;

    /// <summary>
    /// Calculates AP reward for killing an abyss NPC.
    /// Formula mirrors Java StatFunctions.calculatePvEApGained for normal-grade NPCs in abyss zone.
    /// </summary>
    public static int CalculateNpcApReward(int npcLevel) => Math.Max(1, npcLevel * 15);

    /// <summary>
    /// Adds AP to the player's total and recalculates the rank (rank only advances, never decreases from AP).
    /// Returns true when the rank changed (caller should persist and send SM_ABYSS_RANK).
    /// </summary>
    public static bool AddAp(Model.Player player, long amount)
    {
        if (amount <= 0) return false;
        player.AbyssPoints += amount;
        int newRank = GetRankForAp(player.AbyssPoints);
        if (newRank <= player.AbyssRank) return false;
        player.AbyssRank = newRank;
        return true;
    }

    public static int GetRankForAp(long ap)
    {
        int rank = 1;
        foreach (var (threshold, id) in Thresholds)
        {
            if (ap >= threshold) rank = id;
            else break;
        }
        return rank;
    }
}
