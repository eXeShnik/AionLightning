using AionLightning.Commons.Network;
using AionLightning.Game.DataHolders;
using AionLightning.Game.Model;
using AionLightning.Game.Services;
using QuestEngineType = AionLightning.Game.QuestEngine.QuestEngine;
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
    private static DoorService? _doorService;
    private static QuestEngineType? _questEngine;
    private static PlayerConnectionRegistry? _connRegistry;
    private static TeleportService? _teleportService;

    /// <summary>Wires the shared services once at boot (called from the instance-engine host).</summary>
    public static void InitServices(SpawnService spawnService, GameWorld world, IDataManager dataManager, DoorService doorService)
    {
        _spawnService = spawnService;
        _world        = world;
        _dataManager  = dataManager;
        _doorService  = doorService;
    }

    /// <summary>
    /// Wires the quest-hook dispatch path once at boot (called from the instance-engine host,
    /// separate from <see cref="InitServices"/> so a script only needs it — like Dredgion — pulls
    /// in the quest engine and per-player connection lookup it needs to fire
    /// <c>QuestEngine.OnDredgionRewardAsync</c> and evict players on completion.
    /// </summary>
    public static void InitQuestEngine(QuestEngineType questEngine, PlayerConnectionRegistry connRegistry, TeleportService teleportService)
    {
        _questEngine     = questEngine;
        _connRegistry    = connRegistry;
        _teleportService = teleportService;
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

    /// <summary>Opens/closes the door with the given map-design id inside this channel (Java
    /// <c>StaticDoor.setOpen</c>, invoked from instance handlers via e.g. <c>getDoors().get(id).setOpen(...)</c>).
    /// Returns false when no door with that id is spawned in this instance.</summary>
    protected bool SetDoorState(int staticId, bool open)
    {
        if (_doorService is null) return false;
        var scope = new Position(0, 0, 0, 0, Instance.WorldId, Instance.InstanceId);
        return _doorService.SetDoorState(scope, staticId, open);
    }

    /// <summary>Every online player currently inside this channel (Java <c>instance.getPlayersInside()</c>).</summary>
    protected IEnumerable<Player> PlayersInside
        => _world?.GetPlayersInScope(new Position(0, 0, 0, 0, Instance.WorldId, Instance.InstanceId)) ?? [];

    /// <summary>Every NPC currently alive inside this channel (Java <c>instance.getNpcs()</c>).</summary>
    protected IEnumerable<Npc> NpcsInScope
        => _world?.GetNpcsInScope(new Position(0, 0, 0, 0, Instance.WorldId, Instance.InstanceId)) ?? [];

    /// <summary>Sends a packet to every player currently inside this channel (Java
    /// <c>instance.doOnAllPlayers(new Visitor...)</c> + <c>PacketSendUtility.sendPacket</c>).</summary>
    protected void BroadcastToInstance(AionServerPacket packet)
    {
        if (_connRegistry is null) return;
        var scope = new Position(0, 0, 0, 0, Instance.WorldId, Instance.InstanceId);
        foreach (var conn in _connRegistry.GetAll())
            if (conn.ActivePlayer is { } p && p.Position.SameScope(scope))
                _ = conn.SendAsync(packet);
    }

    /// <summary>Evicts a player to their race's instance-exit location (Java
    /// <c>TeleportService2.moveToInstanceExit</c>). No-op if the teleport service isn't wired.</summary>
    protected ValueTask MoveToInstanceExitAsync(Player player, CancellationToken ct)
        => _teleportService?.MoveToInstanceExitAsync(player, ct) ?? ValueTask.CompletedTask;

    /// <summary>
    /// Fires the dredgion-reward quest hook for one player (Java
    /// <c>QuestEngine.getInstance().onDredgionReward(env)</c>). No-op if the quest engine isn't
    /// wired or the player has no live connection.
    /// </summary>
    protected async ValueTask FireOnDredgionRewardAsync(Player player, int rank, CancellationToken ct)
    {
        if (_questEngine is null || _connRegistry is null) return;
        var conn = _connRegistry.Get(player.ObjectId);
        if (conn is null) return;
        await _questEngine.OnDredgionRewardAsync(player, rank, conn, ct);
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
