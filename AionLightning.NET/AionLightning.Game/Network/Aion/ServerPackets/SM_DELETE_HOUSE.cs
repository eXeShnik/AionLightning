using AionLightning.Commons.Network;

namespace AionLightning.Game.Network.Aion.ServerPackets;

/// <summary>
/// Java network.aion.serverpackets.SM_DELETE_HOUSE — despawns an open-world house client-side when it
/// leaves a player's visibility range (Java HouseController.notSee, non-instanced branch).
/// Opcode 0x110 (4.5-era packet table). TODO: verify opcode vs live 4.6 client.
/// </summary>
public sealed class SM_DELETE_HOUSE : AionServerPacket
{
    private readonly int _address;

    public SM_DELETE_HOUSE(int address) : base(0x110)
    {
        _address = address;
    }

    public override void Write(ref PacketWriter w)
    {
        w.WriteD(_address);
    }
}
