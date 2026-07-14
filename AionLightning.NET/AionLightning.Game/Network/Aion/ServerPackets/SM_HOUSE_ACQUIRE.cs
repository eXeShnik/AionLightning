using AionLightning.Commons.Network;

namespace AionLightning.Game.Network.Aion.ServerPackets;

/// <summary>
/// Java network.aion.serverpackets.SM_HOUSE_ACQUIRE — confirms a house/studio acquisition to the client.
/// Opcode 0x113 (4.5-era packet table). TODO: verify opcode against a live 4.6 client capture before enabling.
/// </summary>
public sealed class SM_HOUSE_ACQUIRE : AionServerPacket
{
    private readonly int _playerId;
    private readonly int _address;
    private readonly bool _acquire;

    public SM_HOUSE_ACQUIRE(int playerId, int address, bool acquire) : base(0x113)
    {
        _playerId = playerId;
        _address = address;
        _acquire = acquire;
    }

    public override void Write(ref PacketWriter w)
    {
        w.WriteD(_playerId);
        w.WriteD(_address);
        w.WriteD(_acquire ? 1 : 0); // now it has value 2 sometimes, maybe initial door state ? (Java comment)
    }
}
