using AionLightning.Commons.Network;

namespace AionLightning.Game.Network.Aion.ServerPackets;

/// <summary>
/// Java network.aion.serverpackets.SM_HOUSE_TELEPORT — confirms a CM_HOUSE_TELEPORT relationship-crystal
/// teleport, naming the house address and the target player teleported to.
/// Opcode 0xDD (4.5-era packet table). TODO: verify opcode vs live 4.6 client.
/// </summary>
public sealed class SM_HOUSE_TELEPORT : AionServerPacket
{
    private readonly int _address;
    private readonly int _playerId;

    public SM_HOUSE_TELEPORT(int houseAddress, int playerId) : base(0xDD)
    {
        _address = houseAddress;
        _playerId = playerId;
    }

    public override void Write(ref PacketWriter w)
    {
        w.WriteD(_address);
        w.WriteD(_playerId);
    }
}
