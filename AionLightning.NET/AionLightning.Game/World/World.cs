using System.Collections.Concurrent;
using AionLightning.Game.Model;

namespace AionLightning.Game.World;

public sealed class World
{
    private readonly ConcurrentDictionary<int, Player> _players     = new();
    private readonly ConcurrentDictionary<string, Player> _byName   = new(StringComparer.OrdinalIgnoreCase);
    private readonly ConcurrentDictionary<int, Npc> _npcs           = new();

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
}
