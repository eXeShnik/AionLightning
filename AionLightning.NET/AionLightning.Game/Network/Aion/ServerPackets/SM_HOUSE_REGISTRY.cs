using AionLightning.Commons.Network;
using AionLightning.Game.Model.House;

namespace AionLightning.Game.Network.Aion.ServerPackets;

/// <summary>
/// Java network.aion.serverpackets.SM_HOUSE_REGISTRY — sent twice on entering decoration mode
/// (CM_HOUSE_EDIT's ENTER_DECORATION): action=1 lists the player's not-yet-placed
/// <see cref="Model.GameObjects.HouseObject"/> pool, action=2 lists every decoration part (default +
/// not-currently-displayed custom) available to swap into a slot via CM_HOUSE_DECORATE.
/// Opcode 0x74 (4.5-era packet table). TODO: verify opcode vs live 4.6 client.
/// </summary>
public sealed class SM_HOUSE_REGISTRY : AionServerPacket
{
    private readonly int _action;
    private readonly HouseRegistry? _registry;
    private readonly Func<int, int> _reuseDelayFor;

    /// <param name="reuseDelayFor">Resolves an object's remaining cooldown in seconds for the requesting
    /// player (Java Player.getHouseObjectCooldownList().getReuseDelay(objId)) — only consulted for action=1.</param>
    public SM_HOUSE_REGISTRY(int action, HouseRegistry? registry, Func<int, int>? reuseDelayFor = null) : base(0x74)
    {
        _action = action;
        _registry = registry;
        _reuseDelayFor = reuseDelayFor ?? (_ => 0);
    }

    public override void Write(ref PacketWriter w)
    {
        w.WriteC((byte)_action);

        if (_action == 1)
        {
            if (_registry is null) { w.WriteH(0); return; }

            var notSpawned = _registry.NotSpawnedObjects.ToList();
            w.WriteH(notSpawned.Count);
            foreach (var obj in notSpawned)
            {
                w.WriteD(obj.ObjectId);
                w.WriteD(obj.TemplateId);
                w.WriteD(_reuseDelayFor(obj.ObjectId));
                w.WriteD(obj.UseSecondsLeft > 0 ? obj.UseSecondsLeft : 0);
                HouseObjectPacketBody.WriteColor(ref w, obj.Color);
                w.WriteD(0); // expiration as for armor
                w.WriteC(obj.TypeId);
                if (obj.TypeId == 1)
                    HouseObjectPacketBody.WriteUseItemUsageData(ref w, obj);
            }
        }
        else if (_action == 2)
        {
            if (_registry is null) { w.WriteH(0); return; }

            var defaults = _registry.DefaultParts;
            var customs = _registry.CustomParts;
            w.WriteH(defaults.Count + customs.Count);
            foreach (var deco in defaults)
            {
                w.WriteD(0);
                w.WriteD(deco.PartId);
            }
            foreach (var deco in customs)
            {
                w.WriteD(deco.ObjectId);
                w.WriteD(deco.PartId);
            }
        }
    }
}
