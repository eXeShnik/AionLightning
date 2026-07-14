namespace AionLightning.Game.Services;

/// <summary>
/// Computes abyss rank from accumulated AP (soldier ranks 1–9) and Glory Points (officer/general
/// ranks 10–18). Mirrors Java model.gameobjects.player.AbyssRank / utils.stats.AbyssRankEnum, but
/// keeps AP and GP on <see cref="Model.Player"/> directly instead of a nested AbyssRank object.
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

    // Officer/general ranks (10-18): required Glory Points (Java AbyssRankEnum.required, reused for
    // the GP band), player quota (Java's weekly top-N ladder cap — a real recompute needs a
    // cross-player ranking pass; see AbyssRankUpdateService note on AddGloryPoints below, deferred),
    // and daily GP decay for inactive officers (Java AbyssRankEnum.dailyReduceGp — not yet applied,
    // see AbyssResetService).
    private static readonly (long Gp, int Rank, int Quota, int DailyReduceGp)[] GpThresholds =
    [
        (1244,  10, 1000, 14),
        (1368,  11, 700,  27),
        (1915,  12, 500,  55),
        (3064,  13, 300,  98),
        (5210,  14, 100,  213),
        (8335,  15, 30,   237),
        (10002, 16, 10,   244),
        (11503, 17, 3,    254),
        (12437, 18, 1,    294),
    ];

    // AP gained by the killer per rank of the killed player (ranks 1–18).
    // From Java AbyssRankEnum.pointsGained for GRADE9_SOLDIER … SUPREME_COMMANDER.
    private static readonly int[] PvPApGained =
        [300, 414, 475, 546, 627, 721, 865, 1038, 1245, 1868, 2241, 2577, 2964, 4446, 4890, 5378, 5916, 7099];

    // AP lost by the killed player per their own rank (ranks 1–18).
    // From Java AbyssRankEnum.pointsLost.
    private static readonly int[] PvPApLost =
        [90, 103, 118, 136, 156, 180, 216, 259, 311, 467, 560, 644, 741, 1511, 1662, 1828, 2011, 2413];

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
    /// Mirrors Java StatFunctions.calculatePvpApGained, including the winnerRank&lt;=7 abyss-rank penalty.
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
        else if (diff == -3) points *= 1.20f;
        else if (diff <  -3) points *= 1.30f;
        int pointsGained = (int)MathF.Round(points);

        // Java: winners at/under rank 7 who outrank the defeated player lose 5% of the gain per rank
        // of difference (discourages high-rank players farming low-rank ones).
        int rankDiff = winner.AbyssRank - defeated.AbyssRank;
        if (winner.AbyssRank <= 7 && rankDiff > 0)
            pointsGained -= (int)MathF.Round(pointsGained * (rankDiff * 0.05f));

        return Math.Max(1, pointsGained);
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

    /// <summary>
    /// Adds Glory Points and promotes the player into the officer/general band (ranks 10-18) when a
    /// threshold is crossed. Mirrors Java AbyssRank.addGp / AbyssPointsService.setGp: GP-driven
    /// promotion only ever applies within the officer band (never demotes, never re-enters the
    /// AP-driven soldier band 1-9) — parallel to how AddAp above never promotes past rank 9.
    /// Returns true when the rank changed (caller should persist and send SM_ABYSS_RANK/_UPDATE).
    /// </summary>
    /// <remarks>
    /// note: Java's AbyssRankUpdateService recomputes the officer/general ladder on a scheduled
    /// cross-player pass (top-N by GP per race, via CronService + RankingConfig.TOP_RANKING_UPDATE_RULE),
    /// so <see cref="Model.Player.AbyssTopRanking"/>/quota there reflects a competitive weekly ranking,
    /// not just a threshold. That ladder recompute is not ported here — this is a simplified
    /// GP-threshold-only rank-up (a player promotes purely by crossing their own GP threshold,
    /// regardless of how many other players also qualify). Revisit if/when a CronService-driven ladder
    /// job is added.
    /// </remarks>
    public static bool AddGloryPoints(Model.Player player, long amount)
    {
        if (amount <= 0) return false;
        player.AbyssGp       += amount;
        player.AbyssDailyGp  += amount;
        player.AbyssWeeklyGp += amount;

        int newRank = GetRankForGp(player.AbyssGp);
        if (newRank == 0 || newRank <= player.AbyssRank) return false;

        player.AbyssRank       = newRank;
        player.AbyssTopRanking = GetQuotaForRank(newRank);
        if (player.AbyssRank > player.AbyssMaxRank)
            player.AbyssMaxRank = player.AbyssRank;
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

    /// <summary>
    /// Returns the officer/general rank (10-18) that <paramref name="gp"/> qualifies for, or 0 if it
    /// doesn't reach the first officer threshold (STAR1_OFFICER). Mirrors Java
    /// AbyssRankEnum.getRankForGp restricted to the officer band.
    /// </summary>
    public static int GetRankForGp(long gp)
    {
        int rank = 0;
        foreach (var (threshold, id, _, _) in GpThresholds)
        {
            if (gp >= threshold) rank = id;
            else break;
        }
        return rank;
    }

    private static int GetQuotaForRank(int rank)
    {
        foreach (var (_, id, quota, _) in GpThresholds)
            if (id == rank) return quota;
        return 0;
    }

    /// <summary>
    /// Returns the AP/GP threshold for the next rank above <paramref name="rank"/> — AP for soldier
    /// ranks (1-8→2-9), GP for the rank-9→10 transition and the officer band (10-17→11-18).
    /// Returns the max threshold once at the highest rank (18), to avoid division by zero in progress
    /// calculation.
    /// </summary>
    public static long GetNextRankThreshold(int rank)
    {
        if (rank < 9)
        {
            foreach (var (threshold, id) in Thresholds)
                if (id == rank + 1) return Math.Max(1, threshold);
            return Math.Max(1, Thresholds[^1].Ap);
        }

        foreach (var (threshold, id, _, _) in GpThresholds)
            if (id == rank + 1) return Math.Max(1, threshold);
        return Math.Max(1, GpThresholds[^1].Gp);
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
