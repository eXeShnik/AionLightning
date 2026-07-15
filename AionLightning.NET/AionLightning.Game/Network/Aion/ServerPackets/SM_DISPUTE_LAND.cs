using AionLightning.Commons.Network;

namespace AionLightning.Game.Network.Aion.ServerPackets;

/// <summary>
/// Java network.aion.serverpackets.SM_DISPUTE_LAND — dispute-status broadcast for the rotating
/// PvP-enabled world set (see Services.DisputeLandService). Opcode 0x11B (4.5-era packet table).
/// TODO: verify opcode against a live 4.6 client capture before enabling.
/// </summary>
public sealed class SM_DISPUTE_LAND : AionServerPacket
{
    private readonly IReadOnlyList<int> _worlds;
    private readonly bool _active;

    public SM_DISPUTE_LAND(IReadOnlyList<int> worlds, bool active) : base(0x11B)
    {
        _worlds = worlds;
        _active = active;
    }

    public override void Write(ref PacketWriter w)
    {
        w.WriteH(_worlds.Count);
        foreach (var world in _worlds)
        {
            w.WriteD(_active ? 0x02 : 0x01);
            w.WriteD(world);
            w.WriteQ(0x00);
            w.WriteQ(0x00);
            w.WriteQ(0x00);
            w.WriteQ(0x00);
        }
    }
}
