using System.Collections.Concurrent;
using AionLightning.Game.Configs.Options;
using AionLightning.Game.DataHolders;
using AionLightning.Game.Model;
using Microsoft.Extensions.Options;

namespace AionLightning.Game.Services;

/// <summary>Manages pending loot for killed NPCs until a player picks it up.</summary>
public sealed class LootService
{
    private const int KinahItemId       = 182400001;
    private const int MinorLifeElixirId = 162000052; // fallback when NPC has no drop table

    // Maps dead NPC objectId → pending loot items (index → item)
    private readonly ConcurrentDictionary<int, List<LootEntry>> _pending   = new();
    // Tracks where each NPC died so CM_START_LOOT can enforce proximity
    private readonly ConcurrentDictionary<int, Position>        _positions = new();
    private readonly IDataManager _dataManager;
    private readonly RateOptions  _rates;

    public record LootEntry(int ItemId, long Count, bool IsTradeable);

    public LootService(IDataManager dataManager, IOptions<RateOptions> rates)
    {
        _dataManager = dataManager;
        _rates       = rates.Value;
    }

    /// <summary>Generates drops for a killed NPC and stores them keyed by the NPC's (now-removed) objectId.</summary>
    public void GenerateDrops(Npc npc)
    {
        var drops = new List<LootEntry>();

        // Kinah always drops: level * 50 + random(0, level*20)
        long kinah = npc.Level * 50L + Random.Shared.Next(0, Math.Max(1, npc.Level * 20));
        drops.Add(new LootEntry(KinahItemId, kinah, IsTradeable: true));

        var groups = _dataManager.Drops.GetDropGroups(npc.Template.NpcId);
        if (groups is { Count: > 0 })
        {
            // Roll each item in each drop group independently
            foreach (var group in groups)
            {
                foreach (var entry in group.Entries)
                {
                    double effectiveChance = _rates.DropRate != 1.0f
                        ? Math.Min(100.0, entry.Chance * _rates.DropRate)
                        : entry.Chance;
                    double roll = Random.Shared.NextDouble() * 100.0;
                    if (roll < effectiveChance)
                    {
                        int count = entry.MinAmount == entry.MaxAmount
                            ? entry.MinAmount
                            : Random.Shared.Next(entry.MinAmount, entry.MaxAmount + 1);
                        drops.Add(new LootEntry(entry.ItemId, count, IsTradeable: true));
                    }
                }
            }
        }
        else
        {
            // Fallback when no data-driven table: 50% chance of a healing potion
            if (Random.Shared.NextDouble() < 0.5)
            {
                int potionCount = 1 + Random.Shared.Next(0, Math.Min(3, 1 + npc.Level / 5));
                drops.Add(new LootEntry(MinorLifeElixirId, potionCount, IsTradeable: true));
            }
        }

        _pending[npc.ObjectId]   = drops;
        _positions[npc.ObjectId] = npc.Position;
    }

    public Position? GetLootPosition(int npcObjectId)
        => _positions.TryGetValue(npcObjectId, out var pos) ? pos : null;

    public IReadOnlyList<LootEntry>? GetLoot(int npcObjectId)
        => _pending.TryGetValue(npcObjectId, out var list) ? list : null;

    /// <summary>Takes loot entry by index and removes it from pending. Returns null if not found.</summary>
    public LootEntry? TakeLootAt(int npcObjectId, int index)
    {
        if (!_pending.TryGetValue(npcObjectId, out var list)) return null;
        if (index < 0 || index >= list.Count) return null;

        var entry = list[index];
        list.RemoveAt(index);

        if (list.Count == 0)
            _pending.TryRemove(npcObjectId, out _);

        return entry;
    }

    /// <summary>
    /// Returns a previously taken loot entry back to the pending list (e.g. inventory full).
    /// Re-inserts at the original index so the loot window ordering is preserved.
    /// </summary>
    public void ReturnLoot(int npcObjectId, int index, LootEntry entry)
    {
        if (!_pending.TryGetValue(npcObjectId, out var list))
        {
            _pending[npcObjectId] = [entry];
            return;
        }
        list.Insert(Math.Min(index, list.Count), entry);
    }

    public void ClearLoot(int npcObjectId)
    {
        _pending.TryRemove(npcObjectId, out _);
        _positions.TryRemove(npcObjectId, out _);
    }
}
