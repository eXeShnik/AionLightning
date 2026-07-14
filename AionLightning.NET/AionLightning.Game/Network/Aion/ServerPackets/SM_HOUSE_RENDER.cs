using AionLightning.Commons.Network;
using AionLightning.Game.Model.House;
using AionLightning.Game.Model.Legion;
using AionLightning.Game.Model.Templates.Housing;

namespace AionLightning.Game.Network.Aion.ServerPackets;

/// <summary>
/// Java network.aion.serverpackets.SM_HOUSE_RENDER — full house appearance, sent when an open-world house
/// first enters a player's visibility range (Java HouseController.see, non-instanced branch).
/// Opcode 0x10F (4.5-era packet table). TODO: verify opcode vs live 4.6 client.
/// Caller must call <see cref="House.FixBuildingStates"/> and <see cref="House.NormalizePermissions"/>
/// beforehand — see <see cref="HouseRenderPacketBody"/>.
/// </summary>
public sealed class SM_HOUSE_RENDER : AionServerPacket
{
    private readonly House _house;
    private readonly Building _building;
    private readonly Legion? _legion;

    public SM_HOUSE_RENDER(House house, Building building, Legion? legion) : base(0x10F)
    {
        _house = house;
        _building = building;
        _legion = legion;
    }

    public override void Write(ref PacketWriter w)
    {
        w.WriteD(0);
        w.WriteD(_house.Address);
        w.WriteD(_house.PlayerObjectId);
        // note: Java hardcodes BuildingType.PERSONAL_FIELD's packet id here regardless of the house's
        // actual building type (SM_HOUSE_UPDATE below writes the real type) — reproduced verbatim.
        w.WriteD(HouseRenderPacketBody.BuildingTypeId(BuildingType.PERSONAL_FIELD));
        w.WriteC(1); // unk

        HouseRenderPacketBody.Write(ref w, _house, _building, _legion);
    }
}
