using AionLightning.Commons.Network;
using AionLightning.Game.Model.Siege;

namespace AionLightning.Game.Network.Aion.ServerPackets;

/// <summary>
/// Java network.aion.serverpackets.SM_SIEGE_LOCATION_STATE.
/// Opcode 0xD2 (4.5-era packet table). TODO: verify opcode against a live 4.6 client capture before enabling.
/// </summary>
public sealed class SM_SIEGE_LOCATION_STATE : AionServerPacket
{
    private readonly int _locationId;
    private readonly int _state;

    public SM_SIEGE_LOCATION_STATE(SiegeLocation location) : base(0xD2)
    {
        _locationId = location.LocationId;
        _state = location.IsVulnerable ? 1 : 0;
    }

    public SM_SIEGE_LOCATION_STATE(int locationId, int state) : base(0xD2)
    {
        _locationId = locationId;
        _state = state;
    }

    public override void Write(ref PacketWriter w)
    {
        w.WriteD(_locationId);
        w.WriteC((byte)_state);
    }
}
