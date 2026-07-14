using AionLightning.Commons.Network;
using AionLightning.Game.Model.Siege;
using AionLightning.Game.Services;

namespace AionLightning.Game.Network.Aion.ServerPackets;

/// <summary>
/// Java network.aion.serverpackets.SM_SHIELD_EFFECT.
/// Opcode 0xDA (4.5-era packet table). TODO: verify opcode against a live 4.6 client capture before enabling.
/// </summary>
public sealed class SM_SHIELD_EFFECT : AionServerPacket
{
    private readonly IReadOnlyCollection<SiegeLocation> _locations;

    public SM_SHIELD_EFFECT(IReadOnlyCollection<SiegeLocation> locations) : base(0xDA)
    {
        _locations = locations;
    }

    public SM_SHIELD_EFFECT(int locationId, SiegeService siegeService) : base(0xDA)
    {
        var location = siegeService.GetSiegeLocation(locationId);
        _locations = location is null ? [] : [location];
    }

    public override void Write(ref PacketWriter w)
    {
        w.WriteH((short)_locations.Count);
        foreach (var loc in _locations)
        {
            w.WriteD(loc.LocationId);
            w.WriteC(loc.IsUnderShield ? (byte)1 : (byte)0);
        }
    }
}
