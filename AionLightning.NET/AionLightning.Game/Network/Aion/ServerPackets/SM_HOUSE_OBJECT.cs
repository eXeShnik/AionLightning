using AionLightning.Commons.Network;
using AionLightning.Game.Model.GameObjects;

namespace AionLightning.Game.Network.Aion.ServerPackets;

/// <summary>
/// Java network.aion.serverpackets.SM_HOUSE_OBJECT — introduces/updates a single placed house object for
/// the requesting player (CM_USE_HOUSE_OBJECT's reply, and CM_HOUSE_EDIT's SPAWN_OBJECT/MOVE_OBJECT use
/// SM_HOUSE_EDIT's own near-identical tail instead — see that class).
/// Opcode 0x10C (4.5-era packet table). TODO: verify opcode vs live 4.6 client.
/// </summary>
public sealed class SM_HOUSE_OBJECT : AionServerPacket
{
    private readonly HouseObject _obj;
    private readonly int _reuseDelaySeconds;

    public SM_HOUSE_OBJECT(HouseObject obj, int reuseDelaySeconds) : base(0x10C)
    {
        _obj = obj;
        _reuseDelaySeconds = reuseDelaySeconds;
    }

    public override void Write(ref PacketWriter w)
    {
        var house = _obj.OwnerHouse;

        w.WriteD(house.Address);
        w.WriteD(house.PlayerObjectId);
        w.WriteD(_obj.ObjectId); // <outlet[X]> data in house scripts
        w.WriteD(_obj.ObjectId); // <outDB[X]> data in house scripts

        w.WriteD(_obj.TemplateId);
        w.WriteF(_obj.X);
        w.WriteF(_obj.Y);
        w.WriteF(_obj.Z);
        w.WriteH(_obj.Rotation);

        w.WriteD(_reuseDelaySeconds);
        w.WriteD(_obj.UseSecondsLeft > 0 ? _obj.UseSecondsLeft : 0);

        HouseObjectPacketBody.WriteColor(ref w, _obj.Color);
        w.WriteD(0); // expiration as for armor

        w.WriteC(_obj.TypeId);
        switch (_obj.TypeId)
        {
            case 1: // Use item
                HouseObjectPacketBody.WriteUseItemUsageData(ref w, _obj);
                break;
            case 7: // Npc
                // note: Java writes the spawned house-NPC's world objectId here; this port doesn't spawn
                // a backing Npc for HousingObjectKind.Npc objects yet (see HouseObject's class doc).
                w.WriteD(0);
                break;
        }
    }
}
