using System.Collections.Concurrent;
using AionLightning.Game.Ai;
using AionLightning.Game.Configs.Options;
using AionLightning.Game.DataHolders;
using AionLightning.Game.Model;
using AionLightning.Game.Model.GameObjects;
using AionLightning.Game.Model.GameObjects.Base;
using AionLightning.Game.Model.GameObjects.Siege;
using AionLightning.Game.Model.Templates.Event;
using AionLightning.Game.Model.Templates.Gatherable;
using AionLightning.Game.Model.Templates.Spawns;
using StaticDoorTemplates = AionLightning.Game.Model.Templates.StaticDoor;
using AionLightning.Game.Network.Aion;
using AionLightning.Game.Network.Aion.ServerPackets;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using GameWorld = AionLightning.Game.World.World;

namespace AionLightning.Game.Services;

public sealed class SpawnService
{
    private const int DefaultRespawnSeconds = 30;

    private readonly IDataManager _dataManager;
    private readonly GameWorld _world;
    private readonly PlayerConnectionRegistry _connRegistry;
    private readonly ILogger<SpawnService> _log;
    private readonly RateOptions _rates;
    private readonly AiEngine _aiEngine;

    /// <summary>Event-name-tagged NPC registry backing <see cref="SpawnEvent"/>/<see cref="DespawnEvent"/>
    /// — the seasonal-event counterpart to SiegeService's/BaseService's own siege-/base-id registries,
    /// except owned here (rather than by a dedicated EventNpc wrapper + external registry) since a
    /// seasonal event has no per-NPC state beyond "which event spawned it".</summary>
    private readonly ConcurrentDictionary<string, List<Npc>> _eventNpcsByName = new();

    public SpawnService(IDataManager dataManager, GameWorld world,
        PlayerConnectionRegistry connRegistry, ILogger<SpawnService> log,
        IOptions<RateOptions> rates, AiEngine aiEngine)
    {
        _dataManager  = dataManager;
        _world        = world;
        _connRegistry = connRegistry;
        _log          = log;
        _rates        = rates.Value;
        _aiEngine     = aiEngine;
    }

    public void SpawnAll()
    {
        int npcSpawned = 0, npcSkipped = 0;

        foreach (var (mapId, entry) in _dataManager.Spawns.All())
        {
            var template = _dataManager.Npcs.GetTemplate(entry.NpcId);
            if (template is null) { npcSkipped++; continue; }

            foreach (var spot in entry.Spots)
            {
                SpawnNpc(template, new Position(spot.X, spot.Y, spot.Z, spot.Heading, mapId), entry.RespawnTime, spot.WalkerId);
                npcSpawned++;
            }
        }

        _log.LogInformation("SpawnService: spawned {Spawned} NPCs ({Skipped} skipped)", npcSpawned, npcSkipped);

        int gSpawned = 0, gSkipped = 0;

        foreach (var (mapId, entry) in _dataManager.Spawns.AllGather())
        {
            var template = _dataManager.Gatherables.GetTemplate(entry.NpcId);
            if (template is null) { gSkipped++; continue; }

            foreach (var spot in entry.Spots)
            {
                SpawnGatherable(template, new Position(spot.X, spot.Y, spot.Z, spot.Heading, mapId), entry.RespawnTime);
                gSpawned++;
            }
        }

        _log.LogInformation("SpawnService: spawned {Spawned} gatherables ({Skipped} skipped)", gSpawned, gSkipped);

        int doorsSpawned = 0;
        foreach (int worldId in _dataManager.StaticDoors.WorldIds)
            doorsSpawned += SpawnDoors(worldId, instanceId: 0);

        _log.LogInformation("SpawnService: spawned {Spawned} static doors", doorsSpawned);
    }

    /// <summary>
    /// Populates a world/instance channel with its static doors (Java StaticDoorSpawnManager.spawnTemplate).
    /// Java skips ABYSS/HOUSE door types here ("assign house doors to houses ... abyss doors need
    /// owners" — TODO in the original), a decision this port keeps as-is; only plain DOOR templates spawn.
    /// </summary>
    public int SpawnDoors(int worldId, int instanceId)
    {
        var templates = _dataManager.StaticDoors.GetWorldDoors(worldId);
        if (templates.Count == 0) return 0;

        int spawned = 0;
        foreach (var template in templates)
        {
            if (template.Type != StaticDoorTemplates.DoorType.DOOR) continue;

            var door = new StaticDoor(template)
            {
                ObjectId = ObjectIdFactory.Next(),
                Position = new Position(template.X, template.Y, template.Z, 0, worldId, instanceId),
            };
            _world.Add(door);
            spawned++;
        }

        if (spawned > 0)
            _log.LogInformation("SpawnService: spawned static doors: {World} [{Instance}] : {Count}", worldId, instanceId, spawned);
        return spawned;
    }

    /// <summary>
    /// Populates a freshly-created instance channel with its own copy of the map's static NPC and
    /// gatherable set, tagged with <paramref name="instanceId"/> so they are visible only inside that
    /// channel (Java <c>SpawnEngine.spawnInstance</c>). Open-world <see cref="SpawnAll"/> is unaffected.
    /// </summary>
    public int SpawnInstance(int worldId, int instanceId, int ownerId = 0)
    {
        int npcSpawned = 0;
        foreach (var entry in _dataManager.Spawns.GetByMap(worldId))
        {
            var template = _dataManager.Npcs.GetTemplate(entry.NpcId);
            if (template is null) continue;
            foreach (var spot in entry.Spots)
            {
                SpawnNpc(template, new Position(spot.X, spot.Y, spot.Z, spot.Heading, worldId, instanceId), entry.RespawnTime, spot.WalkerId);
                npcSpawned++;
            }
        }

        int gSpawned = 0;
        foreach (var (mapId, entry) in _dataManager.Spawns.AllGather())
        {
            if (mapId != worldId) continue;
            var template = _dataManager.Gatherables.GetTemplate(entry.NpcId);
            if (template is null) continue;
            foreach (var spot in entry.Spots)
            {
                SpawnGatherable(template, new Position(spot.X, spot.Y, spot.Z, spot.Heading, worldId, instanceId), entry.RespawnTime);
                gSpawned++;
            }
        }

        int doorSpawned = SpawnDoors(worldId, instanceId);

        _log.LogInformation("SpawnService: instance {World}:{Instance} spawned {Npc} NPCs, {Gather} gatherables, {Doors} doors",
            worldId, instanceId, npcSpawned, gSpawned, doorSpawned);
        return npcSpawned + gSpawned + doorSpawned;
    }

    private Gatherable SpawnGatherable(GatherableTemplate template, Position position, int respawnTime = 0)
    {
        var g = new Gatherable(template)
        {
            ObjectId     = ObjectIdFactory.Next(),
            Position     = position,
            HomePosition = position,
            RespawnTime  = respawnTime,
        };
        _world.Add(g);
        return g;
    }

    public void ScheduleGatherableRespawn(Gatherable gatherable)
    {
        var template    = gatherable.Template;
        var position    = gatherable.HomePosition;
        var respawnTime = gatherable.RespawnTime;
        int delay       = respawnTime > 0 ? respawnTime : DefaultRespawnSeconds;

        _ = Task.Run(async () =>
        {
            await Task.Delay(TimeSpan.FromSeconds(delay));
            var respawned = SpawnGatherable(template, position, respawnTime);
            var infoPacket = new SM_GATHERABLE_INFO(respawned);
            foreach (var conn in _connRegistry.GetAll())
                if (conn.ActivePlayer is { } cp && cp.Position.SameScope(position))
                    try { await conn.SendAsync(infoPacket); } catch { }
        });
    }

    /// <summary>Spawns an NPC at an arbitrary position (e.g. from a GM command, or a rift/siege
    /// portal/guard spawn point). <paramref name="respawnTime"/> defaults to 0 (generic default-delay
    /// respawn on death); pass a data-driven value for NPCs that carry their own respawn_time.</summary>
    public Npc SpawnNpcAt(Model.Templates.Npc.NpcTemplate template, Position position, int respawnTime = 0)
        => SpawnNpc(template, position, respawnTime);

    /// <summary>
    /// Java SpawnEngine/VisibleObjectSpawner.spawnSiegeNpc — spawns a single siege-tagged NPC from a
    /// flattened <see cref="SiegeSpawnTemplate"/> (see DataHolders.SiegeSpawnData), reusing the same
    /// creation/AI-binding path as regular NPCs (<see cref="SpawnNpc"/>) and wrapping the result as a
    /// <see cref="SiegeNpc"/> for the caller (SiegeService) to register and broadcast. Returns null when
    /// the referenced NPC id has no known template.
    /// note: Java re-invoked SpawnEngine.spawnObject with the same SiegeSpawnTemplate on death-respawn,
    /// so a respawned guard stayed siege-tagged. This port's generic death/respawn pipeline
    /// (NpcAiService -> ScheduleRespawn) only knows about plain <see cref="Npc"/>/NpcTemplate, so an
    /// individual siege NPC killed mid-siege respawns as a plain (untagged, unregistered) NPC instead of
    /// a re-registered SiegeNpc. Full-location DeSpawnNpcs/SpawnNpcs at siege start/stop is unaffected —
    /// see SiegeService.SpawnNpcs/DeSpawnNpcs.
    /// </summary>
    public SiegeNpc? SpawnSiegeNpc(SiegeSpawnTemplate siegeTemplate)
    {
        var template = _dataManager.Npcs.GetTemplate(siegeTemplate.NpcId);
        if (template is null) return null;

        var position = new Position(siegeTemplate.X, siegeTemplate.Y, siegeTemplate.Z, siegeTemplate.Heading, siegeTemplate.WorldId);
        var npc = SpawnNpc(template, position, siegeTemplate.RespawnTime);
        bool isShieldGenerator = string.Equals(template.Ai, ShieldService.ShieldGeneratorAiName, StringComparison.OrdinalIgnoreCase);
        return new SiegeNpc(npc, siegeTemplate.SiegeId, siegeTemplate.SiegeRace, isShieldGenerator: isShieldGenerator);
    }

    /// <summary>
    /// Java SpawnEngine's base-spawn counterpart to <see cref="SpawnSiegeNpc"/> (services.base.Base.spawn()
    /// /spawnBoss() iterate the base's spawn templates and call SpawnEngine.spawnObject directly) — spawns
    /// a single base-tagged NPC from a flattened <see cref="BaseSpawnTemplate"/> (see
    /// DataHolders.BaseSpawnData), reusing the same creation/AI-binding path as regular NPCs
    /// (<see cref="SpawnNpc"/>) and wrapping the result as a <see cref="BaseNpc"/> for the caller
    /// (BaseService) to register and broadcast. Returns null when the referenced NPC id has no known
    /// template.
    /// note: like <see cref="SpawnSiegeNpc"/>, an individual base NPC killed outright (not via a
    /// location-wide DespawnBase/SpawnDefenders cycle) respawns as a plain, untagged Npc through the
    /// generic death/respawn pipeline (NpcAiService/CM_ATTACK/CM_CASTSPELL -> ScheduleRespawn) — see
    /// SpawnSiegeNpc's own note for the same caveat.
    /// </summary>
    public BaseNpc? SpawnBaseNpc(BaseSpawnTemplate baseTemplate)
    {
        var template = _dataManager.Npcs.GetTemplate(baseTemplate.NpcId);
        if (template is null) return null;

        var position = new Position(baseTemplate.X, baseTemplate.Y, baseTemplate.Z, baseTemplate.Heading, baseTemplate.WorldId);
        var npc = SpawnNpc(template, position, baseTemplate.RespawnTime);
        return new BaseNpc(npc, baseTemplate.BaseId, baseTemplate.BaseRace, baseTemplate.HandlerType == BaseSpawnHandlerType.Boss);
    }

    /// <summary>
    /// Java EventTemplate.Start()'s spawn side — spawns every flattened spot of a seasonal event's NPC
    /// set into the open world (mirrors <see cref="SpawnInstance"/>'s flatten-and-spawn shape, minus the
    /// per-instance channel id), tagging the result under <paramref name="eventName"/> so
    /// <see cref="DespawnEvent"/> can remove the whole group cleanly. Re-spawning under the same name
    /// replaces the previous group's registry entry — callers (EventService) despawn first so no NPC
    /// leaks between the two calls.
    /// </summary>
    public IReadOnlyList<Npc> SpawnEvent(string eventName, IEnumerable<EventSpawnPoint> spawnPoints)
    {
        var spawned = new List<Npc>();
        foreach (var point in spawnPoints)
        {
            var template = _dataManager.Npcs.GetTemplate(point.NpcId);
            if (template is null) continue;

            var position = new Position(point.X, point.Y, point.Z, point.Heading, point.MapId);
            spawned.Add(SpawnNpc(template, position, point.RespawnTime));
        }

        _eventNpcsByName[eventName] = spawned;
        _log.LogInformation("SpawnService: event '{Event}' spawned {Count} NPCs", eventName, spawned.Count);

        if (spawned.Count > 0)
            _ = BroadcastEventSpawnAsync(spawned);

        return spawned;
    }

    /// <summary>Java EventTemplate.Stop()'s despawn side — removes every NPC currently tagged under
    /// <paramref name="eventName"/> by <see cref="SpawnEvent"/>. Returns the number of NPCs removed (0 if
    /// the event has no tracked spawn group, e.g. it was never started or has already been stopped).</summary>
    public int DespawnEvent(string eventName)
    {
        if (!_eventNpcsByName.TryRemove(eventName, out var npcs) || npcs.Count == 0) return 0;

        foreach (var npc in npcs)
            _world.Remove(npc);

        _log.LogInformation("SpawnService: despawned {Count} NPCs for event '{Event}'", npcs.Count, eventName);
        _ = BroadcastEventDespawnAsync(npcs);
        return npcs.Count;
    }

    private async Task BroadcastEventSpawnAsync(List<Npc> spawned)
    {
        foreach (var npc in spawned)
        {
            var infoPacket = new SM_NPC_INFO(npc);
            foreach (var conn in _connRegistry.GetAll())
                if (conn.ActivePlayer is { } p && p.Position.SameScope(npc.Position))
                    try { await conn.SendAsync(infoPacket); } catch { }
        }
    }

    private async Task BroadcastEventDespawnAsync(List<Npc> removed)
    {
        foreach (var npc in removed)
        {
            var deletePacket = new SM_DELETE(npc.ObjectId);
            foreach (var conn in _connRegistry.GetAll())
                if (conn.ActivePlayer is { } p && p.Position.SameScope(npc.Position))
                    try { await conn.SendAsync(deletePacket); } catch { }
        }
    }

    private Npc SpawnNpc(Model.Templates.Npc.NpcTemplate template, Position position, int respawnTime = 0, string walkerId = "")
    {
        var npc = new Npc(template)
        {
            ObjectId     = ObjectIdFactory.Next(),
            Name         = template.Name,
            Position     = position,
            HomePosition = position,
            RespawnTime  = respawnTime,
            WalkerId     = walkerId,
        };

        // Scale HP by NormalMobsRateHp (elite mobs use the same scale for now)
        if (_rates.NormalMobsRateHp != 1.0)
        {
            npc.MaxHp     = Math.Max(1, (int)(npc.MaxHp * _rates.NormalMobsRateHp));
            npc.CurrentHp = npc.MaxHp;
        }

        _world.Add(npc);

        // Bind + fire the script-driven AI (if one is registered for this template's ai-name).
        // NpcAiService remains the archetype-driven combat/wander driver regardless — this only
        // gives scripted NPCs a live NpcAi2 instance to react through (see AiEngineHostedService).
        if (_aiEngine.HasAi(template.Ai))
        {
            npc.ScriptedAi = _aiEngine.Create(template.Ai, npc);
            npc.ScriptedAi?.OnSpawned();
        }

        return npc;
    }

    /// <summary>Schedules an NPC to respawn after its data-driven delay (or the default if unset), broadcasting SM_NPC_INFO to all online players.</summary>
    public void ScheduleRespawn(Npc npc)
    {
        var template    = npc.Template;
        var position    = npc.HomePosition; // always respawn at the original spawn spot, not where the NPC died
        var respawnTime = npc.RespawnTime;
        int delay       = respawnTime > 0 ? respawnTime : DefaultRespawnSeconds;

        _ = Task.Run(async () =>
        {
            await Task.Delay(TimeSpan.FromSeconds(delay));
            var respawned = SpawnNpc(template, position, respawnTime);
            var infoPacket = new SM_NPC_INFO(respawned);
            foreach (var conn in _connRegistry.GetAll())
                if (conn.ActivePlayer is { } cp && cp.Position.SameScope(position))
                    try { await conn.SendAsync(infoPacket); } catch { }
        });
    }
}
