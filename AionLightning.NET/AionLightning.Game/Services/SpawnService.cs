using AionLightning.Game.DataHolders;
using AionLightning.Game.Model;
using AionLightning.Game.Model.Templates.Gatherable;
using AionLightning.Game.Network.Aion;
using AionLightning.Game.Network.Aion.ServerPackets;
using Microsoft.Extensions.Logging;
using GameWorld = AionLightning.Game.World.World;

namespace AionLightning.Game.Services;

public sealed class SpawnService
{
    private const int DefaultRespawnSeconds = 30;

    private readonly IDataManager _dataManager;
    private readonly GameWorld _world;
    private readonly PlayerConnectionRegistry _connRegistry;
    private readonly ILogger<SpawnService> _log;

    public SpawnService(IDataManager dataManager, GameWorld world,
        PlayerConnectionRegistry connRegistry, ILogger<SpawnService> log)
    {
        _dataManager  = dataManager;
        _world        = world;
        _connRegistry = connRegistry;
        _log          = log;
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
                SpawnNpc(template, new Position(spot.X, spot.Y, spot.Z, spot.Heading, mapId));
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
                SpawnGatherable(template, new Position(spot.X, spot.Y, spot.Z, spot.Heading, mapId));
                gSpawned++;
            }
        }

        _log.LogInformation("SpawnService: spawned {Spawned} gatherables ({Skipped} skipped)", gSpawned, gSkipped);
    }

    private Gatherable SpawnGatherable(GatherableTemplate template, Position position)
    {
        var g = new Gatherable(template)
        {
            ObjectId     = ObjectIdFactory.Next(),
            Position     = position,
            HomePosition = position,
        };
        _world.Add(g);
        return g;
    }

    public void ScheduleGatherableRespawn(Gatherable gatherable, int delaySeconds)
    {
        var template = gatherable.Template;
        var position = gatherable.HomePosition;

        _ = Task.Run(async () =>
        {
            await Task.Delay(TimeSpan.FromSeconds(delaySeconds));
            var respawned = SpawnGatherable(template, position);
            var infoPacket = new SM_GATHERABLE_INFO(respawned);
            foreach (var conn in _connRegistry.GetAll())
                if (conn.ActivePlayer?.Position.WorldId == position.WorldId)
                    try { await conn.SendAsync(infoPacket); } catch { }
        });
    }

    /// <summary>Spawns an NPC at an arbitrary position (e.g. from a GM command).</summary>
    public Npc SpawnNpcAt(Model.Templates.Npc.NpcTemplate template, Position position)
        => SpawnNpc(template, position);

    private Npc SpawnNpc(Model.Templates.Npc.NpcTemplate template, Position position)
    {
        var npc = new Npc(template)
        {
            ObjectId     = ObjectIdFactory.Next(),
            Name         = template.Name,
            Position     = position,
            HomePosition = position,
        };
        _world.Add(npc);
        return npc;
    }

    /// <summary>Schedules an NPC to respawn after the configured delay, broadcasting SM_NPC_INFO to all online players.</summary>
    public void ScheduleRespawn(Npc npc, int delaySeconds = DefaultRespawnSeconds)
    {
        var template = npc.Template;
        var position = npc.HomePosition; // always respawn at the original spawn spot, not where the NPC died

        _ = Task.Run(async () =>
        {
            await Task.Delay(TimeSpan.FromSeconds(delaySeconds));
            var respawned = SpawnNpc(template, position);
            var infoPacket = new SM_NPC_INFO(respawned);
            foreach (var conn in _connRegistry.GetAll())
                if (conn.ActivePlayer?.Position.WorldId == position.WorldId)
                    try { await conn.SendAsync(infoPacket); } catch { }
        });
    }
}
