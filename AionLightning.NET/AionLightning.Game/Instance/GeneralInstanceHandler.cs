using AionLightning.Game.DataHolders;
using AionLightning.Game.Model;
using AionLightning.Game.Services;
using GameWorld = AionLightning.Game.World.World;
using WorldMapInstanceType = AionLightning.Game.World.WorldMapInstance;

namespace AionLightning.Game.Instance;

/// <summary>
/// No-op base for every instance handler script (Java <c>GeneralInstanceHandler</c>). Provides the
/// protected helpers scripts use (spawn / npc lookup), backed by the game services via one-time
/// static injection — the same pattern <c>QuestHandlerBase</c> uses, because compiled script
/// classes are constructed by reflection with a parameterless constructor and cannot inject DI
/// services themselves.
/// </summary>
public abstract class GeneralInstanceHandler : IInstanceHandler
{
    private static SpawnService? _spawnService;
    private static GameWorld? _world;
    private static IDataManager? _dataManager;

    /// <summary>Wires the shared services once at boot (called from the instance-engine host).</summary>
    public static void InitServices(SpawnService spawnService, GameWorld world, IDataManager dataManager)
    {
        _spawnService = spawnService;
        _world        = world;
        _dataManager  = dataManager;
    }

    /// <summary>The channel this handler drives; set by the engine before <see cref="OnInstanceCreate"/>.</summary>
    protected WorldMapInstanceType Instance { get; private set; } = null!;

    protected int WorldId => Instance.WorldId;
    protected int InstanceId => Instance.InstanceId;

    internal void Bind(WorldMapInstanceType instance) => Instance = instance;

    /// <summary>Spawns an NPC into THIS channel (Java <c>spawn(npcId,x,y,z,h)</c> → SpawnEngine + spawnObject(instanceId)).</summary>
    protected Npc? Spawn(int npcId, float x, float y, float z, byte heading = 0)
    {
        var template = _dataManager?.Npcs.GetTemplate(npcId);
        if (template is null || _spawnService is null) return null;
        return _spawnService.SpawnNpcAt(template, new Position(x, y, z, heading, Instance.WorldId, Instance.InstanceId));
    }

    /// <summary>First NPC of the given id inside this channel, or null.</summary>
    protected Npc? GetNpc(int npcId)
    {
        if (_world is null) return null;
        var scope = new Position(0, 0, 0, 0, Instance.WorldId, Instance.InstanceId);
        return _world.GetNpcsInScope(scope).FirstOrDefault(n => n.Template.NpcId == npcId);
    }

    // --- No-op default hooks (scripts override what they need) ---
    public virtual void OnInstanceCreate(WorldMapInstanceType instance) { }
    public virtual void OnInstanceDestroy() { }
    public virtual void OnPlayerLogin(Player player) { }
    public virtual void OnPlayerLogOut(Player player) { }
    public virtual void OnEnterInstance(Player player) { }
    public virtual void OnLeaveInstance(Player player) { }
    public virtual void OnExitInstance(Player player) { }
    public virtual void OnEnterZone(Player player, string zoneName) { }
    public virtual void OnLeaveZone(Player player, string zoneName) { }
    public virtual void OnPlayMovieEnd(Player player, int movieId) { }
    public virtual bool OnReviveEvent(Player player) => false;
    public virtual bool OnDie(Player player, Creature? lastAttacker) => false;
    public virtual void OnDie(Npc npc) { }
    public virtual void OnGather(Player player, Gatherable gatherable) { }
    public virtual bool OnPassFlyingRing(Player player, string flyingRing) => false;
    public virtual void HandleUseItemFinish(Player player, Npc npc) { }
}
