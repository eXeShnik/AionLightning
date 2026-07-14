using System.Collections.Concurrent;
using AionLightning.Game.Model;
using AionLightning.Game.Model.Legion;
using AionLightning.Game.Model.Siege;

namespace AionLightning.Game.Services.Siege;

/// <summary>
/// Java services.siegeservice.SiegeRaceCounter — per-race boss-damage/AP/GP/legion tallies for a single
/// active siege. Java backed each counter with a shared/synchronized FastMap of AtomicLong; this port
/// uses ConcurrentDictionary&lt;int, long&gt; with AddOrUpdate (the update factory may run more than
/// once under contention, but plain addition is commutative so that's harmless).
/// </summary>
public sealed class SiegeRaceCounter : IComparable<SiegeRaceCounter>
{
    private long _totalDamage;
    private readonly ConcurrentDictionary<int, long> _playerDamage = new();
    private readonly ConcurrentDictionary<int, long> _playerAp = new();
    private readonly ConcurrentDictionary<int, long> _playerGp = new();
    private readonly ConcurrentDictionary<int, long> _legionDamage = new();

    public SiegeRace SiegeRace { get; }

    public SiegeRaceCounter(SiegeRace siegeRace) => SiegeRace = siegeRace;

    public void AddPoints(Creature creature, int damage)
    {
        AddTotalDamage(damage);
        if (creature is Player player)
            AddPlayerDamage(player, damage);
    }

    public void AddTotalDamage(int damage) => Interlocked.Add(ref _totalDamage, damage);

    public void AddPlayerDamage(Player player, int damage)
    {
        if (player.Legion is { } legion)
            AddLegionDamage(legion, damage);
        AddToCounter(player.ObjectId, damage, _playerDamage);
    }

    public void AddLegionDamage(Legion legion, int damage) => AddToCounter(legion.LegionId, damage, _legionDamage);

    public void AddAbyssPoints(Player player, int abyssPoints) => AddToCounter(player.ObjectId, abyssPoints, _playerAp);

    public void AddGloryPoints(Player player, int gloryPoints) => AddToCounter(player.ObjectId, gloryPoints, _playerGp);

    private static void AddToCounter(int key, int value, ConcurrentDictionary<int, long> counter) =>
        counter.AddOrUpdate(key, value, (_, existing) => existing + value);

    public long TotalDamage => Interlocked.Read(ref _totalDamage);

    public long NonLegionDamage => TotalDamage - TotalLegionDamage;

    public long TotalLegionDamage => _legionDamage.Values.Sum();

    /// <summary>Java getLegionDamageCounter() — legionId → damage, descending, zero-damage entries dropped.</summary>
    public IReadOnlyDictionary<int, long> LegionDamageCounter => OrderDescending(_legionDamage);

    /// <summary>Java getPlayerDamageCounter().</summary>
    public IReadOnlyDictionary<int, long> PlayerDamageCounter => OrderDescending(_playerDamage);

    /// <summary>Java getPlayerAbyssPoints().</summary>
    public IReadOnlyDictionary<int, long> PlayerAbyssPoints => OrderDescending(_playerAp);

    /// <summary>Java getPlayerGloryPoints().</summary>
    public IReadOnlyDictionary<int, long> PlayerGloryPoints => OrderDescending(_playerGp);

    private static IReadOnlyDictionary<int, long> OrderDescending(ConcurrentDictionary<int, long> map) =>
        map.Where(kv => kv.Value > 0)
           .OrderByDescending(kv => kv.Value)
           .ToDictionary(kv => kv.Key, kv => kv.Value);

    /// <summary>Java compareTo(SiegeRaceCounter) — deliberately inverted so that sorting ascending
    /// (the default for both Java's Collections.sort and C#'s List.Sort/OrderBy) yields descending
    /// total-damage order.</summary>
    public int CompareTo(SiegeRaceCounter? other) => (other?.TotalDamage ?? 0).CompareTo(TotalDamage);

    /// <summary>
    /// Java getWinnerLegionId() — the legion whose members did the most damage, but only when that
    /// legion's damage exceeds all non-legion (unaffiliated/other-legion) damage combined; otherwise no
    /// single legion "captured" the siege and null is returned.
    /// </summary>
    public int? WinnerLegionId
    {
        get
        {
            var legionDamage = LegionDamageCounter;
            if (legionDamage.Count == 0) return null;

            var top = legionDamage.First();
            return top.Value > NonLegionDamage ? top.Key : null;
        }
    }
}
