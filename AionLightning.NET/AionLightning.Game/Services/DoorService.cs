using AionLightning.Game.Configs.Options;
using AionLightning.Game.Model;
using AionLightning.Game.Model.GameObjects;
using AionLightning.Game.Network.Aion.ServerPackets;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using GameWorld = AionLightning.Game.World.World;

namespace AionLightning.Game.Services;

/// <summary>
/// Java services.StaticDoorService — open/close state for spawned static doors, plus the client-facing
/// broadcast. Java's StaticDoor.setOpen self-broadcasts SM_EMOTION on every state flip; this port
/// centralizes that here so the door-state packet can be config-gated in one place (see
/// <see cref="DoorOptions"/>) instead of scattering the gate check into the model.
/// </summary>
public sealed class DoorService
{
    private readonly GameWorld _world;
    private readonly PlayerConnectionRegistry _connRegistry;
    private readonly DoorOptions _options;
    private readonly ILogger<DoorService> _log;

    public DoorService(GameWorld world, PlayerConnectionRegistry connRegistry, IOptions<DoorOptions> options, ILogger<DoorService> log)
    {
        _world        = world;
        _connRegistry = connRegistry;
        _options      = options.Value;
        _log          = log;
    }

    /// <summary>Java StaticDoorService.openStaticDoor — client-driven open request (CM_OPEN_STATICDOOR,
    /// opcode 0xF5). Looks the door up by its map-design id in the requesting player's own world/instance
    /// scope, consumes a key item when the door template requires one, then opens it.</summary>
    public bool TryOpenDoor(Player player, int staticId)
    {
        var door = _world.GetDoor(player.Position, staticId);
        if (door is null)
        {
            _log.LogWarning("DoorService: not spawned door worldId={World} instanceId={Instance} staticId={StaticId}",
                player.Position.WorldId, player.Position.InstanceId, staticId);
            return false;
        }

        if (!ConsumeKeyIfNeeded(player, door.Template.KeyId))
            return false;

        SetOpenAndBroadcast(door, open: true);
        return true;
    }

    /// <summary>Java StaticDoor.setOpen — script-driven state change with no key check, used by the
    /// GeneralInstanceHandler/NpcAi2 SetDoorState helpers so ported instance/AI scripts' door stubs have
    /// a real API to call. Returns false when no door with <paramref name="staticId"/> is spawned in
    /// <paramref name="scope"/>.</summary>
    public bool SetDoorState(Position scope, int staticId, bool open)
    {
        var door = _world.GetDoor(scope, staticId);
        if (door is null) return false;

        SetOpenAndBroadcast(door, open);
        return true;
    }

    private void SetOpenAndBroadcast(StaticDoor door, bool open)
    {
        door.SetOpen(open);
        if (!_options.Enable) return;

        var emotion = open ? EmotionType.OPEN_DOOR : EmotionType.CLOSE_DOOR;
        int packetState = open ? 0x9 : 0xA; // Java comment: "not important IMO, similar to internal state"
        // Java broadcasts SM_EMOTION keyed by the door's static/map id, not its AION object id — doors
        // are not spawned visible objects to the client (no knownlist add packet ever goes out for one),
        // so the client must be matching this id against its own static map geometry instead.
        var packet = new SM_EMOTION(door.StaticId, emotion, packetState);
        var scope  = door.Position;

        _ = Task.Run(async () =>
        {
            foreach (var conn in _connRegistry.GetAll())
                if (conn.ActivePlayer is { } p && p.Position.SameScope(scope))
                    try { await conn.SendAsync(packet); } catch { }
        });
    }

    /// <summary>Java StaticDoorService.checkStaticDoorKey, minus the GM access-level bypass — no
    /// admin/GM-level concept is ported yet (see migration_plan.md). keyId==0 always allows opening;
    /// keyId==1 always denies (a Java-side quirk kept as-is); any other id requires the player to hold
    /// one of that item, which is consumed in memory only — unlike the DB-backed item removal used
    /// elsewhere (e.g. QuestHandlerBase), the item's DB row and SM_DELETE_ITEM are not touched by this
    /// path, since key items are rare enough that this is not worth the extra DAO wiring here.</summary>
    private static bool ConsumeKeyIfNeeded(Player player, int keyId)
    {
        if (keyId == 0) return true;
        if (keyId == 1) return false;

        var item = player.Inventory.FindByItemId(keyId);
        if (item is null) return false;

        item.Count -= 1;
        if (item.Count <= 0)
            player.Inventory.Remove(item.UniqueId);
        return true;
    }
}
