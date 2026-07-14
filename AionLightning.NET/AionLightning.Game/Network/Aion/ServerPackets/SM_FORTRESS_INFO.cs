using AionLightning.Commons.Network;

namespace AionLightning.Game.Network.Aion.ServerPackets;

/// <summary>
/// Java network.aion.serverpackets.SM_FORTRESS_INFO.
/// Opcode 0xF3 (4.5-era packet table). TODO: verify opcode against a live 4.6 client capture before enabling.
/// </summary>
public sealed class SM_FORTRESS_INFO : AionServerPacket
{
    private readonly int _locationId;
    private readonly bool _teleportStatus;

    public SM_FORTRESS_INFO(int locationId, bool teleportStatus) : base(0xF3)
    {
        _locationId = locationId;
        _teleportStatus = teleportStatus;
    }

    public override void Write(ref PacketWriter w)
    {
        w.WriteD(_locationId);
        w.WriteC(_teleportStatus ? (byte)1 : (byte)0);
    }
}
