using AionLightning.Game.DataHolders;
using AionLightning.Game.Model;
using Microsoft.Extensions.Logging;
using GameWorld = AionLightning.Game.World.World;

namespace AionLightning.Game.Services;

public sealed class SpawnService
{
    private readonly IDataManager _dataManager;
    private readonly GameWorld    _world;
    private readonly ILogger<SpawnService> _log;

    public SpawnService(IDataManager dataManager, GameWorld world, ILogger<SpawnService> log)
    {
        _dataManager = dataManager;
        _world       = world;
        _log         = log;
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
                var npc = new Npc(template)
                {
                    ObjectId = ObjectIdFactory.Next(),
                    Name     = template.Name,
                    Position = new Position(spot.X, spot.Y, spot.Z, spot.Heading, mapId)
                };
                _world.Add(npc);
                spawned++;
            }
        }

        _log.LogInformation("SpawnService: spawned {Spawned} NPCs ({Skipped} entries skipped — no template)", spawned, skipped);
    }
}
