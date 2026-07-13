using AionLightning.Game.DataHolders;
using AionLightning.Game.Instance;
using AionLightning.Game.Model;
using AionLightning.Game.Model.Group;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using GameWorld = AionLightning.Game.World.World;
using WorldMapInstanceType = AionLightning.Game.World.WorldMapInstance;

namespace AionLightning.Game.Services;

/// <summary>
/// Creates, tracks and tears down instance channels, and registers players/teams into them
/// (Java <c>services/instance/InstanceService</c>). Objects live in the flat <see cref="GameWorld"/>
/// and are scoped by <c>(WorldId, InstanceId)</c>; this service owns the channel lifecycle metadata
/// via <see cref="World.InstanceRegistry"/>.
/// </summary>
public sealed class InstanceService
{
    /// <summary>A solo channel is destroyed this long after it is first observed empty (Java 10 min).</summary>
    public const long SoloDestroyDelayMs = 10 * 60 * 1000;

    private readonly World.InstanceRegistry _registry;
    private readonly IDataManager _dataManager;
    private readonly SpawnService _spawnService;
    private readonly InstanceEngine _engine;
    private readonly GameWorld _world;
    private readonly IServiceProvider _sp; // lazy TeleportService (breaks the ctor cycle)
    private readonly ILogger<InstanceService> _log;

    public InstanceService(World.InstanceRegistry registry, IDataManager dataManager,
        SpawnService spawnService, InstanceEngine engine, GameWorld world,
        IServiceProvider sp, ILogger<InstanceService> log)
    {
        _registry     = registry;
        _dataManager  = dataManager;
        _spawnService = spawnService;
        _engine       = engine;
        _world        = world;
        _sp           = sp;
        _log          = log;
    }

    /// <summary>
    /// Allocates a new channel for <paramref name="worldId"/> (Java <c>getNextAvailableInstance</c>):
    /// creates the metadata, attaches a fresh handler, loads the instanced spawn set, and fires the
    /// handler's create hook. Throws if the world is not an instance map.
    /// </summary>
    public WorldMapInstanceType GetNextAvailableInstance(int worldId, int ownerId = 0)
    {
        if (!_dataManager.WorldMaps.IsInstance(worldId))
            throw new InvalidOperationException($"World {worldId} is not an instance map.");

        int instanceId = _registry.NextInstanceId(worldId);
        var instance = new WorldMapInstanceType(worldId, instanceId, ownerId)
        {
            Handler = _engine.GetNewInstanceHandler(worldId),
        };
        _registry.Add(instance);

        _spawnService.SpawnInstance(worldId, instanceId, ownerId);
        _engine.OnInstanceCreate(instance);

        _log.LogInformation("InstanceService: created channel {World}:{Instance} (owner {Owner})", worldId, instanceId, ownerId);
        return instance;
    }

    public void RegisterPlayerWithInstance(WorldMapInstanceType instance, Player player)
    {
        instance.Register(player.ObjectId);
        instance.SoloPlayerObjId = player.ObjectId;
    }

    public void RegisterGroupWithInstance(WorldMapInstanceType instance, PlayerGroup group)
    {
        instance.RegisteredGroupId = group.GroupId;
        foreach (var member in group.Members)
            instance.Register(member.ObjectId);
    }

    /// <summary>The channel of <paramref name="worldId"/> that <paramref name="objectId"/> is registered to, or null.</summary>
    public WorldMapInstanceType? GetRegisteredInstance(int worldId, int objectId)
        => _registry.ByWorld(worldId).FirstOrDefault(i => i.IsRegistered(objectId));

    public WorldMapInstanceType? GetPersonalInstance(int worldId, int ownerId)
        => ownerId == 0 ? null : _registry.ByWorld(worldId).FirstOrDefault(i => i.IsPersonal && i.OwnerId == ownerId);

    public bool IsInstanceExist(int worldId, int instanceId) => _registry.Exists(worldId, instanceId);

    /// <summary>Tears down a channel: evicts players to the exit, deletes its NPCs/gatherables, fires the destroy hook.</summary>
    public void DestroyInstance(WorldMapInstanceType instance)
    {
        var scope = ScopeOf(instance);

        var teleport = _sp.GetRequiredService<TeleportService>();
        foreach (var player in _world.GetPlayersInScope(scope).ToList())
            _ = teleport.MoveToInstanceExitAsync(player, CancellationToken.None);

        foreach (var npc in _world.GetNpcsInScope(scope).ToList())
            _world.Remove(npc);
        foreach (var g in _world.GetGatherablesInScope(scope).ToList())
            _world.Remove(g);

        try { instance.Handler.OnInstanceDestroy(); } catch (Exception ex) { _log.LogError(ex, "OnInstanceDestroy hook failed for {World}:{Instance}", instance.WorldId, instance.InstanceId); }
        _registry.Remove(instance);
        _log.LogInformation("InstanceService: destroyed channel {World}:{Instance}", instance.WorldId, instance.InstanceId);
    }

    public void OnEnterInstance(Player player)
    {
        var inst = _registry.Get(player.Position.WorldId, player.Position.InstanceId);
        if (inst is null) return;
        try { inst.Handler.OnEnterInstance(player); } catch (Exception ex) { _log.LogError(ex, "OnEnterInstance hook failed"); }
    }

    public void OnLeaveInstance(Player player, Position oldScope)
    {
        var inst = _registry.Get(oldScope.WorldId, oldScope.InstanceId);
        if (inst is null) return;
        try { inst.Handler.OnLeaveInstance(player); } catch (Exception ex) { _log.LogError(ex, "OnLeaveInstance hook failed"); }
    }

    /// <summary>Fires the instance handler's NPC-death hook when a mob dies inside a channel (Java <c>onDie(Npc)</c>).</summary>
    public void OnNpcDeath(Npc npc)
    {
        if (npc.Position.InstanceId == 0) return;
        var inst = _registry.Get(npc.Position.WorldId, npc.Position.InstanceId);
        if (inst is null) return;
        try { inst.Handler.OnDie(npc); } catch (Exception ex) { _log.LogError(ex, "OnDie(Npc) hook failed"); }
    }

    /// <summary>Fires the instance handler's player-death hook (Java <c>onDie(Player, lastAttacker)</c>); returns true if handled.</summary>
    public bool OnPlayerDeath(Player player, Creature? killer)
    {
        if (player.Position.InstanceId == 0) return false;
        var inst = _registry.Get(player.Position.WorldId, player.Position.InstanceId);
        if (inst is null) return false;
        try { return inst.Handler.OnDie(player, killer); } catch (Exception ex) { _log.LogError(ex, "OnDie(Player) hook failed"); return false; }
    }

    /// <summary>Count of online players currently inside a channel (derived by scoping the world).</summary>
    public int PlayersInside(WorldMapInstanceType instance) => _world.GetPlayersInScope(ScopeOf(instance)).Count();

    internal static Position ScopeOf(WorldMapInstanceType instance)
        => new(0, 0, 0, 0, instance.WorldId, instance.InstanceId);
}
