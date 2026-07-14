using AionLightning.Commons.Network;

namespace AionLightning.Game.Network.Aion.ServerPackets;

/// <summary>
/// Java network.aion.serverpackets.SM_DELETE_HOUSE_OBJECT — tells the client to despawn a placed house
/// object (not currently sent by this port's CM handlers — DESPAWN_OBJECT/DELETE_ITEM use SM_HOUSE_EDIT's
/// own despawn/remove acks instead, matching Java's CM_HOUSE_EDIT — kept for parity with the Java class list).
/// Opcode 0x10D (4.5-era packet table). TODO: verify opcode vs live 4.6 client.
/// </summary>
public sealed class SM_DELETE_HOUSE_OBJECT : AionServerPacket
{
    private readonly int _itemObjectId;

    public SM_DELETE_HOUSE_OBJECT(int itemObjectId) : base(0x10D)
    {
        _itemObjectId = itemObjectId;
    }

    public override void Write(ref PacketWriter w) => w.WriteD(_itemObjectId);
}
