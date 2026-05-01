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

    // AP gained by the killer per rank of the killed player (ranks 1–9).
    // From Java AbyssRankEnum.pointsGained for GRADE9_SOLDIER … GRADE1_SOLDIER.
    private static readonly int[] PvPApGained = [300, 414, 475, 546, 627, 721, 865, 1038, 1245];

    // AP lost by the killed player per their own rank (ranks 1–9).
    // From Java AbyssRankEnum.pointsLost.
    private static readonly int[] PvPApLost = [90, 103, 118, 136, 156, 180, 216, 259, 311];

    // World IDs where PvP AP is awarded (Abyss + Balaurea).
    private static readonly HashSet<int> PvPWorldIds =
    [
        210050000, 220070000, 400010000,
        600010000, 600020000, 600030000, 600040000, 600050000, 600060000, 600070000,
    ];

    /// <summary>
    /// Abyss world ID (Reshanta). NPCs in this map reward AP on kill.
    /// </summary>
    public const int AbyssWorldId = 400010000;

    /// <summary>
    /// Returns true when the world ID is a PvP zone where AP is exchanged on player kills.
    /// </summary>
    public static bool IsPvPMap(int worldId) => PvPWorldIds.Contains(worldId);

    /// <summary>
    /// Calculates AP reward for killing an abyss NPC.
    /// Formula mirrors Java StatFunctions.calculatePvEApGained for normal-grade NPCs in abyss zone.
    /// </summary>
    public static int CalculateNpcApReward(int npcLevel) => Math.Max(1, npcLevel * 15);

    /// <summary>
    /// AP the winner gains for killing a player; based on the defeated player's rank and level difference.
    /// Mirrors Java StatFunctions.calculatePvpApGained.
    /// </summary>
    public static int CalculatePvPApGained(Model.Player winner, Model.Player defeated)
    {
        int rankIdx = Math.Clamp(defeated.AbyssRank - 1, 0, PvPApGained.Length - 1);
        float points = PvPApGained[rankIdx];
        int diff = winner.Level - defeated.Level;
        if      (diff >  4) points *= 0.10f;
        else if (diff == 4) points *= 0.65f;
        else if (diff == 3) points *= 0.85f;
        else if (diff == -2) points *= 1.10f;
        else if (diff <= -3) points *= 1.20f;
        return Math.Max(1, (int)MathF.Round(points));
    }

    /// <summary>
    /// AP the defeated player loses; based on their own rank and the level difference.
    /// Mirrors Java StatFunctions.calculatePvPApLost.
    /// </summary>
    public static int CalculatePvPApLost(Model.Player winner, Model.Player defeated)
    {
        int rankIdx = Math.Clamp(defeated.AbyssRank - 1, 0, PvPApLost.Length - 1);
        float points = PvPApLost[rankIdx];
        int diff = winner.Level - defeated.Level;
        if      (diff >  4) points *= 0.10f;
        else if (diff == 4) points *= 0.65f;
        else if (diff == 3) points *= 0.85f;
        return Math.Max(0, (int)MathF.Round(points));
    }

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

    /// <summary>
    /// Subtracts AP from the player. AP cannot go below 0.
    /// Returns true if the rank changed (though rank never decreases from AP loss in this implementation).
    /// </summary>
    public static void LoseAp(Model.Player player, long amount)
    {
        if (amount <= 0) return;
        player.AbyssPoints = Math.Max(0, player.AbyssPoints - amount);
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

    /// <summary>
    /// Returns the AP threshold for the next rank above <paramref name="rank"/>.
    /// Returns 1 if already at max rank (to avoid division by zero in progress calculation).
    /// </summary>
    public static long GetNextRankThreshold(int rank)
    {
        foreach (var (threshold, id) in Thresholds)
            if (id == rank + 1) return Math.Max(1, threshold);
        return Math.Max(1, Thresholds[^1].Ap);
    }

    /// <summary>
    /// Records a PvP kill against the player's kill counters and daily/weekly AP.
    /// Call this after AddAp for a PvP kill.
    /// </summary>
    public static void TrackPvPKill(Model.Player killer, int apGain)
    {
        killer.AbyssAllKill++;
        killer.AbyssDailyKill++;
        killer.AbyssWeeklyKill++;
        killer.AbyssDailyAp  += apGain;
        killer.AbyssWeeklyAp += apGain;
        if (killer.AbyssRank > killer.AbyssMaxRank)
            killer.AbyssMaxRank = killer.AbyssRank;
    }
}
