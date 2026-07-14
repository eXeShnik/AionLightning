using AionLightning.Commons.Network;
using AionLightning.Game.Model.GameObjects;

namespace AionLightning.Game.Network.Aion.ServerPackets;

/// <summary>
/// Java network.aion.serverpackets.SM_HOUSE_OBJECTS — the list of a player's currently-placed (spawned)
/// house objects, sent on entering their house (Java CM_LEVEL_READY's houseRegistry != null branch).
/// Opcode 0x10E (4.5-era packet table). TODO: verify opcode vs live 4.6 client.
/// </summary>
public sealed class SM_HOUSE_OBJECTS : AionServerPacket
{
    private readonly IReadOnlyList<HouseObject> _objects;

    public SM_HOUSE_OBJECTS(IEnumerable<HouseObject> spawnedObjects) : base(0x10E)
    {
        _objects = spawnedObjects.ToList();
    }

    public override void Write(ref PacketWriter w)
    {
        w.WriteH(_objects.Count);
        foreach (var obj in _objects)
        {
            w.WriteD(obj.TemplateId);
            w.WriteF(obj.X);
            w.WriteF(obj.Y);
            w.WriteF(obj.Z);
        }
    }
}
