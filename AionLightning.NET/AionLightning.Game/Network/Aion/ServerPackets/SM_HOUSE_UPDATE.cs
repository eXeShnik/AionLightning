using AionLightning.Commons.Network;
using AionLightning.Game.Model.House;
using AionLightning.Game.Model.Legion;
using AionLightning.Game.Model.Templates.Housing;

namespace AionLightning.Game.Network.Aion.ServerPackets;

/// <summary>
/// Java network.aion.serverpackets.SM_HOUSE_UPDATE — full house appearance for the instanced-house branch
/// (Java HouseController.see, when the house's world is an instance) — same payload as SM_HOUSE_RENDER
/// plus a 3-int16 header and the house's real building-type id.
/// Opcode 0x3D (4.5-era packet table). TODO: verify opcode vs live 4.6 client.
/// Caller must call <see cref="House.FixBuildingStates"/> and <see cref="House.NormalizePermissions"/>
/// beforehand — see <see cref="HouseRenderPacketBody"/>.
/// </summary>
public sealed class SM_HOUSE_UPDATE : AionServerPacket
{
    private readonly House _house;
    private readonly Building _building;
    private readonly Legion? _legion;

    public SM_HOUSE_UPDATE(House house, Building building, Legion? legion) : base(0x3D)
    {
        _house = house;
        _building = building;
        _legion = legion;
    }

    public override void Write(ref PacketWriter w)
    {
        w.WriteH(1); // unk
        w.WriteH(0);
        w.WriteH(1); // unk

        w.WriteD(0);
        w.WriteD(_house.Address);
        w.WriteD(_house.PlayerObjectId);
        w.WriteD(HouseRenderPacketBody.BuildingTypeId(_building.Type));
        w.WriteC(1); // unk

        HouseRenderPacketBody.Write(ref w, _house, _building, _legion);
    }
}
