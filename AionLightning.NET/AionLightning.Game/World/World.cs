using System.Collections.Concurrent;
using AionLightning.Game.Model;

namespace AionLightning.Game.World;

public sealed class World
{
    private readonly ConcurrentDictionary<int, Player>      _players    = new();
    private readonly ConcurrentDictionary<string, Player>   _byName     = new(StringComparer.OrdinalIgnoreCase);
    private readonly ConcurrentDictionary<int, Npc>         _npcs       = new();
    private readonly ConcurrentDictionary<int, Gatherable>  _gatherables = new();
    private readonly ConcurrentDictionary<int, Summon>      _summons    = new();

    // --- Players ---

    public bool Add(Player player)
    {
        if (!_players.TryAdd(player.ObjectId, player)) return false;
        _byName[player.Name] = player;
        return true;
    }

    public bool Remove(Player player)
    {
        _byName.TryRemove(player.Name, out _);
        return _players.TryRemove(player.ObjectId, out _);
    }

    public Player? GetPlayerByObjectId(int objectId) => _players.GetValueOrDefault(objectId);
    public Player? GetByName(string name)            => _byName.GetValueOrDefault(name);
    public IEnumerable<Player> GetAll()              => _players.Values;
    public int OnlineCount                           => _players.Count;

    // --- NPCs ---

    public bool Add(Npc npc)    => _npcs.TryAdd(npc.ObjectId, npc);
    public bool Remove(Npc npc) => _npcs.TryRemove(npc.ObjectId, out _);
    public Npc? GetNpcByObjectId(int objectId) => _npcs.GetValueOrDefault(objectId);
    public IEnumerable<Npc> GetAllNpcs()       => _npcs.Values;
    public int NpcCount                        => _npcs.Count;

    // --- Summons (M381) ---

    public bool Add(Summon summon)    => _summons.TryAdd(summon.ObjectId, summon);
    public bool Remove(Summon summon) => _summons.TryRemove(summon.ObjectId, out _);
    public Summon? GetSummonByObjectId(int objectId) => _summons.GetValueOrDefault(objectId);
    public IEnumerable<Summon> GetAllSummons() => _summons.Values;

    // --- Gatherables ---

    public bool Add(Gatherable g)    => _gatherables.TryAdd(g.ObjectId, g);
    public bool Remove(Gatherable g) => _gatherables.TryRemove(g.ObjectId, out _);
    public Gatherable? GetGatherable(int objectId) => _gatherables.GetValueOrDefault(objectId);
    public IEnumerable<Gatherable> GetAllGatherables() => _gatherables.Values;
    public int GatherableCount => _gatherables.Count;
}
