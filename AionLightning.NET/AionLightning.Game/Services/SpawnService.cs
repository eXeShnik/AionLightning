using AionLightning.Game.DataHolders;
using AionLightning.Game.Model;
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
        int spawned = 0;
        int skipped = 0;

        foreach (var (mapId, entry) in _dataManager.Spawns.All())
        {
            var template = _dataManager.Npcs.GetTemplate(entry.NpcId);
            if (template is null)
            {
                skipped++;
                continue;
            }

            foreach (var spot in entry.Spots)
            {
                SpawnNpc(template, new Position(spot.X, spot.Y, spot.Z, spot.Heading, mapId));
                spawned++;
            }
        }

        _log.LogInformation("SpawnService: spawned {Spawned} NPCs ({Skipped} entries skipped — no template)", spawned, skipped);
    }

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
